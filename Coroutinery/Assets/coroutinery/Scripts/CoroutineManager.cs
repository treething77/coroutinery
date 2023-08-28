using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace aeric.coroutinery
{
    //TODO: dont have this as an extension it pollutes the namespace
    public static class ReflectionExtensions
    {
        public static T GetFieldValue<T>(this object obj, string name)
        {
            // Set the flags so that private and public fields from instances will be found
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var field = obj.GetType().GetField(name, bindingFlags);
            return (T)field?.GetValue(obj);
        }

        public static FieldInfo GetFieldInfo<T>(Type type, string name)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var field = type.GetField(name, bindingFlags);
            return field;
        }

        public static T GetFieldValue<T>(this object obj, FieldInfo fieldInfo)
        {
            return (T)fieldInfo?.GetValue(obj);
        }

    }

    // This struct is used to keep track of coroutines. It is used to identify coroutines but does not store any state.
    public struct CoroutineHandle
    {
        //uuid - 64 bit
        public readonly ulong _id;

        public CoroutineHandle(ulong id)
        {
            _id = id;
        }
    }

    public class WaitForFrames : CustomYieldInstruction
    {
        private int _endFrame;

        public WaitForFrames(int frames)
        {
            _endFrame = Time.frameCount + frames;
        }

        public override bool keepWaiting
        {
            get
            {
                return Time.frameCount < _endFrame;
            }
        }
    }

    public class WaitForLateUpdate : YieldInstruction
    {
    }

    public static class YieldStatics
    {
        public static WaitForEndOfFrame _WaitForEndOfFrame = new WaitForEndOfFrame();
        public static WaitForFixedUpdate _WaitForFixedUpdate = new WaitForFixedUpdate();
        public static WaitForLateUpdate _WaitForLateUpdate = new WaitForLateUpdate();


    }

    public class CoroutineUpdater : MonoBehaviour
    {
        private CoroutineManager _manager;

        public void Initialize(CoroutineManager coroutineManager)
        {
            _manager = coroutineManager;

            //We use Unity coroutines in order to run our logic at the same points in the frame as Unity
            //coroutines.
            StartCoroutine(EndOfFrameRunner());
        }

        private IEnumerator EndOfFrameRunner()
        {
            while (true)
            {
                yield return YieldStatics._WaitForEndOfFrame;
                _manager.RunEndOfFrame();
            }
        }

        private void Update()
        {
            CoroutineManager.RunCoroutines();
        }

        private void FixedUpdate()
        {
            _manager.RunFixedUpdate();
        }

        private void LateUpdate()
        {
            _manager.RunLateUpdate();
        }
    }

    public class CoroutineManager
    {
        public enum RunPhase
        {
            Update,
            FixedUpdate,
            LateUpdate,
            EndOfFrame
        }

        private static CoroutineManager _manager;

        public static CoroutineManager Instance => _manager;

        private CoroutineUpdater _updater;

        //This is used to emulate an oddity of the Unity behavior where a coroutine that yields in the fixed update does not
        //run again on the same frame even though it seems like it should since Update runs after FixedUpdate
        //so we introduce an artificial delay of one frame for coroutines that yield in the fixed update
        private List<CoroutineHandle> _pendingList = new List<CoroutineHandle>();


        private List<CoroutineHandle> _activeList = new List<CoroutineHandle>();
        private List<CoroutineHandle> _pausedList = new List<CoroutineHandle>();
        private List<CoroutineHandle> _killList = new List<CoroutineHandle>();//temp list of coroutines to kill

        private Dictionary<CoroutineHandle, IEnumerator> _enumeratorLookup = new Dictionary<CoroutineHandle, IEnumerator>();
        private Dictionary<IEnumerator, CoroutineHandle> _handleLookup = new Dictionary<IEnumerator, CoroutineHandle>();

        private Dictionary<string, List<CoroutineHandle>> _tags = new Dictionary<string, List<CoroutineHandle>>();
        private Dictionary<int, List<CoroutineHandle>> _layers = new Dictionary<int, List<CoroutineHandle>>();
        private Dictionary<Object, List<CoroutineHandle>> _context = new Dictionary<Object, List<CoroutineHandle>>();

        private ulong _nextId = 0;
                
        //wait lists - coroutines that yielded on things we are waiting to complete
        private List<CoroutineHandle> _endOfFrameRunners = new List<CoroutineHandle>();
        private List<CoroutineHandle> _fixedUpdateRunners = new List<CoroutineHandle>();
        private List<CoroutineHandle> _lateUpdateRunners = new List<CoroutineHandle>();

        private FieldInfo waitForSecondValue;

        struct WaitTimer
        {
            public float endTime;
            public CoroutineHandle handle;
        }

        private List<WaitTimer> _waitTimers = new List<WaitTimer>();
        private List<WaitTimer> _pausedWaitTimers = new List<WaitTimer>();

        struct WaitCoroutine
        {
            public CoroutineHandle subcoroutine;
            public CoroutineHandle handle;
        }

        private List<WaitCoroutine> _waitCoroutines = new List<WaitCoroutine>();

        private List<CoroutineHandle> _customYieldList = new List<CoroutineHandle>();
        private List<CoroutineHandle> _pausedCustomYieldList = new List<CoroutineHandle>();
        private Dictionary<CoroutineHandle,Action> _stopCallbacks = new Dictionary<CoroutineHandle, Action>();
        private Dictionary<CoroutineHandle, Action> _finishedCallbacks = new Dictionary<CoroutineHandle, Action>();
        private Dictionary<CoroutineHandle, string> _stackTrace = new Dictionary<CoroutineHandle, string>();

        public static void CreateManager()
        {
            //TODO: error + early out if not null
            _manager = new CoroutineManager();
        }


        public CoroutineManager()
        {
            waitForSecondValue = ReflectionExtensions.GetFieldInfo<float>(typeof(WaitForSeconds), "m_Seconds");
        }

        //TODO: move static methods into own static class, purely for API?
        public static void RunCoroutines()
        {
            _manager.Run(RunPhase.Update);

            //handle wait timers
            _manager.RunWaitTimers();

            //handle frame timers
            _manager.RunCustomYields();

            _manager.RunPending();

        }

        private void RunPending()
        {
            foreach(var c in _pendingList)
            {
                addCoroutineToRunnerList(c, _activeList);
            }
            _pendingList.Clear();
        }

        private void RunCustomYields()
        {
            for (int i = _customYieldList.Count - 1; i > -1; i--)
            {
                var handle = _customYieldList[i];
                var enumerator = _enumeratorLookup[handle];
                if (!(enumerator.Current as CustomYieldInstruction).keepWaiting)
                {
                    addCoroutineToRunnerList(handle, _activeList);
                    _customYieldList.RemoveAt(i);

                    //immediately run the coroutine that was waiting
                    RunCoroutineStep(handle, RunPhase.Update);
                }
            }

            if (_waitCoroutines.Count > 0)
            {
                var waitListcopy = new List<WaitCoroutine>(_waitCoroutines);

                foreach (var w in waitListcopy)
                {
                    //If we are waiting on a coroutine that isn't in our system then go back to active
                    if (!_enumeratorLookup.ContainsKey(w.subcoroutine))
                    {
                        addCoroutineToRunnerList(w.handle, _activeList);
                        _waitCoroutines.Remove(w);
                    }
                }
            }
        }

        private void RunWaitTimers()
        {
            for (int i = _waitTimers.Count - 1; i > -1; i--)
            {
                var timer = _waitTimers[i];
                if (Time.time >= timer.endTime)
                {
                    addCoroutineToRunnerList(timer.handle, _activeList);
                    _waitTimers.RemoveAt(i);

                    //immediately run the coroutine that was waiting
                    RunCoroutineStep(timer.handle, RunPhase.Update);
                }
            }

            //For any paused coroutines waiting on a timer we will push the end time back
            //by the amount of time we were paused
            for (int i = _pausedWaitTimers.Count - 1; i > -1; i--)
            {
                var timer = _pausedWaitTimers[i];
                timer.endTime += Time.deltaTime;
                _pausedWaitTimers[i] = timer;
            }
        }

        private void Run(RunPhase phase)
        {
            //get the correct coroutine list
            List<CoroutineHandle> srcList = GetPhaseCoroutines(phase);
            List<CoroutineHandle> listCopy = new List<CoroutineHandle>(srcList);

            RunCoroutinesInPhase(listCopy, phase);
        }

        private void RunCoroutinesInPhase(List<CoroutineHandle> coroutines, RunPhase phase)
        {
            foreach (var co in coroutines)
            {
                RunCoroutineStep(co, phase);
            }

            KillCoroutines();
        }

        private List<CoroutineHandle> GetPhaseCoroutines(RunPhase phase)
        {
            switch (phase)
            {
                case RunPhase.Update:
                    return _activeList;
                case RunPhase.EndOfFrame:
                    return _endOfFrameRunners;
                case RunPhase.FixedUpdate:
                    return _fixedUpdateRunners;
                case RunPhase.LateUpdate:
                    return _lateUpdateRunners;
            }
            //TODO: error
            return null;
        }

        private void KillCoroutines()
        {
            //remove from any runner or wait lists it was in
            foreach (var kill in _killList)
                StopCoroutine(kill);
            _killList.Clear();
        }

        private void RunCoroutineStep(CoroutineHandle handle, RunPhase phase)
        {
            IEnumerator co;
            if (!_enumeratorLookup.TryGetValue(handle, out co))
            {
                //TODO: error if not found
                return;
            }

            //do the MoveNext first and then handle the results
            if (!co.MoveNext())
            {
                CoroutineFinished(co, handle);
            }
            else
            {
                //what did we yield on?
                if (co.Current == null)
                {
                    //if we yielded on null, then add back to the active list regardless of which phase we are currently running
                    addCoroutineToRunnerList(handle, _pendingList);
                }
                //if we are waiting on a timer, start it now?
                if (co.Current is WaitForSeconds)
                {
                    //remove from whichever runner list and add to a wait list
                    removeFromRunnerLists(handle);

                    WaitTimer wt = new WaitTimer();
                    wt.handle = handle;
                    wt.endTime = Time.time + (waitForSecondValue.GetValue(co.Current) as float? ?? 0.0f);

                    _waitTimers.Add(wt);
                }
                else if (co.Current is WaitForEndOfFrame)
                {
                    //remove from whichever runner list and add to the end of frame list
                    addCoroutineToRunnerList(handle, _endOfFrameRunners);
                }
                else if (co.Current is WaitForFixedUpdate)
                {
                    //remove from whichever runner list and add to the fixed update list
                    addCoroutineToRunnerList(handle, _fixedUpdateRunners);
                }
                else if (co.Current is WaitForLateUpdate)
                {
                    //remove from whichever runner list and add to the fixed update list
                    addCoroutineToRunnerList(handle, _lateUpdateRunners);
                }
                else if (co.Current is CustomYieldInstruction)
                {
                    //remove from whichever runner list and add to a wait list
                    //remove from runner lists, add to wait list
                    removeFromRunnerLists(handle);
                    _customYieldList.Add(handle);
                }
                else if (co.Current is IEnumerator && _handleLookup.ContainsKey(co.Current as IEnumerator))
                {
                    //TODO: if we are waiting on an enumator that is not already in our system then we need to add it
                    // this can happen when someone starts a coroutine using
                    // e.g. "yield return Moveit()" vs
                    //      "yield return StartCoroutine(Moveit())"

                    //Check if this coroutine is on the kill list
                    var subcoroutineHandle = _handleLookup[co.Current as IEnumerator];
                //    if (!_killList.Contains(subcoroutineHandle))
                    {
                        //if we are waiting on another coroutine then remove from runner lists and put on a wait list
                        removeFromRunnerLists(handle);

                        WaitCoroutine wc = new WaitCoroutine();
                        wc.handle = handle;
                        wc.subcoroutine = subcoroutineHandle;
                        _waitCoroutines.Add(wc);
                    }
                }
            }
        }

        private void addCoroutineToRunnerList(CoroutineHandle handle, List<CoroutineHandle> runnerList)
        {
            removeFromRunnerLists(handle);
            runnerList.Add(handle);
        }

        private bool removeFromRunnerLists(CoroutineHandle handle)
        {
            //TODO: should only be in one list at a time
            bool found = false;

            if (_activeList.Contains(handle)) {_activeList.Remove(handle); found = true;}
            if (_endOfFrameRunners.Contains(handle)) { _endOfFrameRunners.Remove(handle); found = true; }
            if (_fixedUpdateRunners.Contains(handle)) { _fixedUpdateRunners.Remove(handle); found = true;}
            if (_lateUpdateRunners.Contains(handle)) { _lateUpdateRunners.Remove(handle); found = true;}

            return found;
        }

        private void CoroutineFinished(IEnumerator co, CoroutineHandle handle)
        {
            //Add to the kill list to be processed at the end of the frame
            //do we need this since we are iterating a copy of the runner list?
            _killList.Add(handle);

            removeFromRunnerLists(handle);

            if (_finishedCallbacks.TryGetValue(handle, out var callback))
            {
                callback?.Invoke();
                _finishedCallbacks.Remove(handle);
            }

            //check for any coroutines waiting on this one. Remove them from the wait list and
            //immediately trigger them (ie call RunCoroutineStep with them)
            foreach (var wc in _waitCoroutines)
            {
                if (wc.subcoroutine._id == handle._id)
                {
                    _waitCoroutines.Remove(wc);
                    RunCoroutineStep(wc.handle, RunPhase.Update);
                    break;
                }
            }
        }

        public void RunEndOfFrame()
        {
            Run(RunPhase.EndOfFrame);
        }

        public void RunFixedUpdate()
        {
            Run(RunPhase.FixedUpdate);
        }

        public void RunLateUpdate()
        {
            Run(RunPhase.LateUpdate);
        }

        // Keep the same interface as the Unity version
        public static CoroutineHandle StartCoroutine(IEnumerator coroutine)
        {
            if (coroutine == null)
            {
                //TODO: error
                return default(CoroutineHandle);
            }

            if (_manager == null)
            {
                CreateManager();
                _manager.Initialize();
            }

            //TODO: push coroutine handle onto list rather than enumerator
            return _manager.Start(coroutine);

            //TODO: did it immediately end?

        }

        public string GetStackTrace(CoroutineHandle handle)
        {
            if (_stackTrace.TryGetValue(handle, out var stackTrace))
            {
                return stackTrace;
            }

            return string.Empty;
        }

        //////////////////////////////////////////////////////////////////////////////
        //PUBLIC API
        public CoroutineHandle Start(IEnumerator coroutine, Object context = null, string tag = null, int layer = 0)
        {
            _nextId++;
            CoroutineHandle coroutineHandle = new CoroutineHandle(_nextId);

            //TODO: store the stack trace for debugging
            if (Application.isEditor)
            {
                var stackTrace = Environment.StackTrace;
                _stackTrace[coroutineHandle] = stackTrace;
            }

            _activeList.Add(coroutineHandle);
            _enumeratorLookup.Add(coroutineHandle, coroutine);
            _handleLookup.Add(coroutine, coroutineHandle);

            RunCoroutineStep(coroutineHandle, RunPhase.Update);

            return coroutineHandle;
        }

        public void StopCoroutine(CoroutineHandle handle)
        {
            if (_stopCallbacks.TryGetValue(handle, out var callback))
            {
                callback?.Invoke();
                _stopCallbacks.Remove(handle);
            }

            //remove from _activeList, lookups, kill list etc
            removeFromRunnerLists(handle);

            //TODO: ssdfremove from kill list
            //TODO: remove from wait lists
            
            //Remove from context lists
            foreach (var contextList in _context)
            {
                if (contextList.Value.Contains(handle)) contextList.Value.Remove(handle);
            }

            //Remove from tag lists
            foreach (var tagList in _tags)
            {
                if (tagList.Value.Contains(handle)) tagList.Value.Remove(handle);
            }

            //Remove from layer lists
            foreach (var layerList in _layers)
            {
                if (layerList.Value.Contains(handle)) layerList.Value.Remove(handle);
            }

            //TODO: remove from pending list
            //TODO: remove from paused lists

            IEnumerator co = _enumeratorLookup[handle];
            _enumeratorLookup.Remove(handle);
            _handleLookup.Remove(co);
        }

        public void StopCoroutines(List<CoroutineHandle> coroutines)
        {
            foreach (var c in coroutines) StopCoroutine(c);
        }

        public void StopAllCoroutines()
        {
            StopCoroutines(_activeList);
        }

        public void StopCoroutinesByTag(string tag)
        {
            StopCoroutines(GetCoroutinesByTagNoCopy(tag));
        }

        public void StopCoroutinesByLayer(int layer)
        {
            StopCoroutines(GetCoroutinesByLayerNoCopy(layer));
        }

        public void StopCoroutinesByContext(Object context)
        {
            StopCoroutines(GetCoroutinesByContextNoCopy(context));
        }

        //Returns a copy of the list
        public List<CoroutineHandle> GetCoroutinesByTag(string tag)
        {
            List<CoroutineHandle> internalList = GetCoroutinesByTagNoCopy(tag);
            return new List<CoroutineHandle>(internalList);
        }

        public List<CoroutineHandle> GetCoroutinesByTagNoCopy(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return new List<CoroutineHandle>(); 

            if (!_tags.ContainsKey(tag)) {
                List<CoroutineHandle> tagHandleList = new List<CoroutineHandle>();
                _tags.Add(tag, tagHandleList);
            }
            return _tags[tag];
        }

        //Returns a copy of the list   ffd    
        public List<CoroutineHandle> GetCoroutinesByLayer(int layer)
        {
            List<CoroutineHandle> internalList = GetCoroutinesByLayerNoCopy(layer);
            return new List<CoroutineHandle>(internalList);
        }
        public List<CoroutineHandle> GetCoroutinesByLayerNoCopy(int layer)
        {
            if (!_layers.ContainsKey(layer)) {
                List<CoroutineHandle> layerHandleList = new List<CoroutineHandle>();
                _layers.Add(layer, layerHandleList);
            }
            return _layers[layer];
        }

        //Returns a copy of the list
        public List<CoroutineHandle> GetCoroutinesByContext(Object context) {
            List<CoroutineHandle> internalList = GetCoroutinesByContextNoCopy(context);
            return new List<CoroutineHandle>(internalList);
        }

        public List<CoroutineHandle> GetCoroutinesByContextNoCopy(Object context) {
            if (!_context.ContainsKey(context)) {
                List<CoroutineHandle> contextHandleList = new List<CoroutineHandle>();
                _context.Add(context, contextHandleList);
            }
            return _context[context];
        }

        public void PauseCoroutine(CoroutineHandle handle)
        {
            if (_killList.Contains(handle)) return; //TODO: error?

            //if this coroutine is waiting on another coroutine then pause that one too
            foreach (var wc in _waitCoroutines) {
                if (wc.handle._id == handle._id) {
                    PauseCoroutine(wc.subcoroutine);
                }
            }

            //If the coroutine is in an active runner list then add it to our paused list
            if (removeFromRunnerLists(handle))
            {
                _pausedList.Add(handle);
            }
            else
            {
                //if the coroutine is waiting on a timer then it is sufficient to just not run the timer
                // when pausing remove from waitTimers add to pausedWaitTimers
                foreach (var wt in _waitTimers)
                {
                    if (wt.handle._id == handle._id)
                    {
                        _pausedWaitTimers.Add(wt);
                        _waitTimers.Remove(wt);
                        break;
                    }
                }

                //if the coroutine is waiting on a custom yield instruction then it is sufficient to just not check the end condition
                // when pausing remove from customYieldList add to pausedCustomYieldList
                foreach (var ct in _customYieldList)
                {
                    if (ct._id == handle._id)
                    {
                        _pausedCustomYieldList.Add(ct);
                        _customYieldList.Remove(ct);
                        break;
                    }
                }
            }
        }

        public void PauseCoroutinesByTag(string tag)
        {
            PauseCoroutines(GetCoroutinesByTagNoCopy(tag));
        }

        public void PauseCoroutines(List<CoroutineHandle> coroutines)
        {
            foreach (var c in coroutines) PauseCoroutine(c);
        }

        public void PauseCoroutinesByLayer(int layer)
        {
            PauseCoroutines(GetCoroutinesByLayerNoCopy(layer));
        }

        public void PauseCoroutinesByContext(Object context)
        {
            PauseCoroutines(GetCoroutinesByContextNoCopy(context));
        }

        public void ResumeCoroutine(CoroutineHandle handle) 
        {
            //TODO: if this coroutine is in the kill list then dont do anything
            addCoroutineToRunnerList(handle, _activeList);    
            _pausedList.Remove(handle);
        }

        public void ResumeCoroutines(List<CoroutineHandle> coroutines)
        {
            foreach (var c in coroutines) ResumeCoroutine(c);
        }

        public void ResumeCoroutinesByTag(string tag)
        {
            ResumeCoroutines(GetCoroutinesByTagNoCopy(tag));
        }

        public void ResumeCoroutinesByLayer(int layer)
        {
            ResumeCoroutines(GetCoroutinesByLayerNoCopy(layer));
        }

        public void ResumeCoroutinesByContext(Object context)
        {
            ResumeCoroutines(GetCoroutinesByContextNoCopy(context));
        }

        //Set the context of a coroutine
        public void SetCoroutineContext(CoroutineHandle handle, Object context) 
        {
            //If this coroutine already has a tag, remove it from the tag list
            if (_context.ContainsKey(context))
            {
                _context[context].Remove(handle);
            }
            var contextgHandleList = GetCoroutinesByContextNoCopy(context);
            contextgHandleList.Add(handle);
        }

        //Get the context of a coroutine
        public Object GetCoroutineContext(CoroutineHandle handle) 
        {
            //Look at context lists and return context if handle exists in any of them
            foreach (var contextList in _context)
            {
                if (contextList.Value.Contains(handle)) return contextList.Key;
            }
            return null; 
        }

        //Set the tag of a coroutine
        public void SetCoroutineTag(CoroutineHandle handle, string tag) 
        {
            //If this coroutine already has a tag, remove it from the tag list
            if (_tags.ContainsKey(tag))
            {
                _tags[tag].Remove(handle);
            }
            var tagHandleList = GetCoroutinesByTagNoCopy(tag);
            tagHandleList.Add(handle);
        }

        //Get the tag of a coroutine
        public string GetCoroutineTag(CoroutineHandle handle) 
        {
            //Look at tag lists and return tag if handle exists in any of them
            foreach (var tagList in _tags)
            {
                if (tagList.Value.Contains(handle)) return tagList.Key;
            }
            return string.Empty;
        }

        //Set the layer of a coroutine
        public void SetCoroutineLayer(CoroutineHandle handle, int layer) {
            var layerHandleList = GetCoroutinesByLayerNoCopy(layer);
            layerHandleList.Add(handle);
        }

        //Get the layer of a coroutine
        public int GetCoroutineLayer(CoroutineHandle handle) 
        {
            //Look at layer lists and return layer if handle exists in any of them
            foreach (var layerList in _layers)
            {
                if (layerList.Value.Contains(handle)) return layerList.Key;
            }
            return 0;
        }

        //Get the paused state of a coroutine
        public bool GetCoroutinePaused(CoroutineHandle handle) 
        { 
            return _pausedList.Contains(handle);
        }

        //Get the active state of a coroutine
        public bool GetCoroutineActive(CoroutineHandle handle) { return false; }

        //Set the callback for whendd a coroutine is stopped
        public void SetCoroutineOnStop(CoroutineHandle handle, Action callback) {
            _stopCallbacks[handle] = callback;
        }

        //Set the callback for when a coroutine is finished  
        public void SetCoroutineOnFinished(CoroutineHandle handle, Action callback) {
            _finishedCallbacks[handle] = callback;
        }

        //////////////////////////////////////////////////////////////////////////////

        private void Initialize()
        {
            if (_updater == null)
            {
                //TODO: yuck
                GameObject instanceHome = GameObject.Find("CoroutineUpdater");

                if (instanceHome == null)
                {
                    instanceHome = new GameObject { name = "CoroutineUpdater" };
                    Object.DontDestroyOnLoad(instanceHome);
                }

                _updater = instanceHome.GetComponent<CoroutineUpdater>() ??
                           instanceHome.AddComponent<CoroutineUpdater>();
                _updater.Initialize(this);
            }
        }

        public SourceInfo GetCoroutineDebugInfo(CoroutineHandle coroutineHandle, CoroutineDebugInfo d)
        {
            string debugInfo = string.Empty;

            IEnumerator c = _enumeratorLookup[coroutineHandle];
            Type typ = c.GetType();
            FieldInfo type = typ.GetField("<>1__state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int stateValue = (int)type.GetValue(c);

            debugInfo += "State: " + stateValue + "\n";


            return d.GetSourceInfo(c, stateValue);
        }

        public IEnumerator GetCoroutineEnumerator(CoroutineHandle coroutineHandle)
        {
            return _enumeratorLookup[coroutineHandle];
        }

        public CoroutineHandle GetCoroutineHandle(IEnumerator enumerator)
        {
            return _handleLookup[enumerator];
        }

        public string GetCoroutinePrettyName(CoroutineHandle coroutine, CoroutineDebugInfo debugInfo)
        {
            var context = GetCoroutineContext(coroutine);
            string contextStr = string.Empty;
            if (context != null)
            {
                contextStr = context.name;
                if (contextStr == "")
                {
                    contextStr = context.GetType().Name;
                }
            }

            SourceInfo coroutineDebug = GetCoroutineDebugInfo(coroutine, debugInfo);

            //extract the method name from the enumerator type name
            string methodName = string.Empty;
            if (debugInfo != null)
            {
                methodName = coroutineDebug.enumeratorTypeName;
                int index1 = methodName.IndexOf('<');
                int index2 = methodName.IndexOf('>');
                if (index1 != -1 && index2 != -1)
                {
                    methodName = methodName.Substring(index1 + 1, index2 - index1 - 1);
                }
            }

            return string.Format("{0}.{1}", contextStr, methodName);
        }

        public float GetWaitTimeRemaining(CoroutineHandle coroutineHandle)
        {
            //check the waitTimers list for this coroutine
            foreach(var wt in _waitTimers)
            {
                if (wt.handle._id == coroutineHandle._id)
                {
                    float timeLeft = wt.endTime - Time.time;
                    return timeLeft;
                }
            }

            //TODO: error
            return 0.0f;
        }
    }
}