using DTT.Utils.CoroutineManagement;
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
        public static readonly CoroutineHandle InvalidHandle = new CoroutineHandle(0);

        //uuid - 64 bit
        public readonly ulong _id;
        public bool IsValid => _id != InvalidHandle._id;

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
            EndOfFrame,
        //    UpdatePending
        }

        private static CoroutineManager _manager;

        public static CoroutineManager Instance
        {
            get
            {
                if (_manager == null)
                {
                    CreateManager();
                    _manager.Initialize();
                }
                return _manager;
            }
        }

        public bool BreakOnFinished { get; set; }
        public bool LogSteps { get; set; }

        private CoroutineUpdater _updater;

        enum CoroutineState // 16 bits?
        {
            Running,
            Finished,
            Pending,
            Waiting_CustomYield,
            Waiting_Coroutine,
            Waiting_Time,
            Waiting_Realtime,
            Waiting_Frame,

        }

        class CoroutineData
        {
            public CoroutineHandle _handle;
            public IEnumerator _enumerator;
            public CoroutineState _state;
            public bool _paused;
            public RunPhase _runPhase;
            public float _waitEndTime;
            public ulong _waitCoroutineId;
            public Action _stopAction;
            public Action _finishedAction;

            public CoroutineData(CoroutineHandle coroutineHandle, IEnumerator coroutine)
            {
                this._handle = coroutineHandle;
                this._enumerator = coroutine;
            }
        }

        private List<CoroutineData> _coroutines = new List<CoroutineData>();

        //This is used to emulate an oddity of the Unity behavior where a coroutine that yields in the fixed update does not
        //run again on the same frame even though it seems like it should since Update runs after FixedUpdate
        //so we introduce an artificial delay of one frame for coroutines that yield in the fixed update 

        private List<CoroutineData> _killList = new List<CoroutineData>();//temp list of coroutines to kill
        private List<CoroutineData> _startList = new List<CoroutineData>();//temp list of coroutines to start


        private Dictionary<string, List<CoroutineHandle>> _tags = new Dictionary<string, List<CoroutineHandle>>();
        private Dictionary<int, List<CoroutineHandle>> _layers = new Dictionary<int, List<CoroutineHandle>>();
        private Dictionary<Object, List<CoroutineHandle>> _context = new Dictionary<Object, List<CoroutineHandle>>();

        private ulong _nextId = 0;
                
        private FieldInfo waitForSecondValue;

        struct WaitTimer
        {
            public float endTime;
            public CoroutineHandle handle;
        }

        struct WaitCoroutine
        {
            public CoroutineHandle subcoroutine;//coroutine that we are waiting on
            public CoroutineHandle handle;//coroutine that is waiting
        }

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
            Instance.Run(RunPhase.Update);

            //handle wait timers
            Instance.RunTimersAndCustomYields();
        }

        private void RunTimersAndCustomYields()
        {


            foreach (var c in _coroutines)
            {
                //if a coroutine is paused then exit early
                if (c._paused)
                {
                    if (c._state == CoroutineState.Waiting_Time || c._state == CoroutineState.Waiting_Realtime)
                    { 
                        //For any paused coroutines waiting on a timer we will push the end time back
                        //by the amount of time we were paused
                        c._waitEndTime += Time.deltaTime;
                    }   
                    continue;
                }

                if (c._state == CoroutineState.Waiting_CustomYield)
                {
                    var handle = c._handle;
                    var enumerator = c._enumerator;
                    if (!(enumerator.Current as CustomYieldInstruction).keepWaiting)
                    {
                        c._state = CoroutineState.Running;

                        //immediately run the coroutine that was waiting
                        RunCoroutineStep(c, RunPhase.Update);
                    }
                }
                else if (c._state == CoroutineState.Waiting_Coroutine)
                {
                    //If we are waiting on a coroutine that isn't in our system then go back to active
                    //Technically this should never be required, because when a coroutine ends we check if any other coroutines are waiting on it
                    if (GetCoroutineEnumerator(new CoroutineHandle(c._waitCoroutineId)) == null)
                    {
                        c._state = CoroutineState.Running;
                    }
                }
                else if (c._state == CoroutineState.Waiting_Time)
                {                    
                    if (Time.time >= c._waitEndTime)
                    {
                        c._state = CoroutineState.Running;

                        //immediately run the coroutine that was waiting
                        RunCoroutineStep(c, RunPhase.Update);
                    }
                }
                else if (c._state == CoroutineState.Waiting_Realtime)
                {
                    if (Time.realtimeSinceStartup >= c._waitEndTime)
                    {
                        c._state = CoroutineState.Running;

                        //immediately run the coroutine that was waiting
                        RunCoroutineStep(c, RunPhase.Update);
                    }
                }
            }

            foreach (var c in _coroutines)
            {
                if (c._state == CoroutineState.Pending)
                {
                    c._runPhase = RunPhase.Update;
                    c._state = CoroutineState.Running;
                }
            }
        }

        private void Run(RunPhase phase)
        {
            foreach (var co in _coroutines)
            {
                if (co._runPhase == phase)
                {
                    RunCoroutineStep(co, phase);
                }
            }

            foreach(var c in _startList)
            {
                _coroutines.Add(c);
            }
            _startList.Clear();

            KillCoroutines();
        }

        private void KillCoroutines()
        {
            //remove from any runner or wait lists it was in
            foreach (var kill in _killList)
                CoroutineCleanup(kill);
            _killList.Clear();
        }

        private void RunCoroutineStep(CoroutineData coroutine, RunPhase phase)
        {
            IEnumerator co = coroutine._enumerator;

            //Only call MoveNext is the coroutine is in the active state
            if (coroutine._state != CoroutineState.Running)
            {
                return;
            }

            //do the MoveNext first and then handle the results
            if (!co.MoveNext())
            {
                CoroutineFinished( coroutine );
            }
            else
            {
                //what did we yield on?
                if (co.Current == null)
                {
                    //if we yielded on null, then add back to the active list regardless of which phase we are currently running
                    coroutine._state = CoroutineState.Pending;
                }
                //if we are waiting on a timer, start it now?
                else if (co.Current is WaitForSeconds)
                {
                    //remove from whichever runner list and add to a wait list
                    coroutine._state = CoroutineState.Waiting_Time;
                    coroutine._waitEndTime = Time.time + (waitForSecondValue.GetValue(co.Current) as float? ?? 0.0f);
                }
                else if (co.Current is WaitForSecondsRealtime)
                {
                    coroutine._state = CoroutineState.Waiting_Realtime;
                    var wr = co.Current as WaitForSecondsRealtime;
                    coroutine._waitEndTime = Time.realtimeSinceStartup + wr.waitTime;
                }
                else if (co.Current is WaitForEndOfFrame)
                {
                    //remove from whichever runner list and add to the end of frame list
                    coroutine._runPhase = RunPhase.EndOfFrame;
                }
                else if (co.Current is WaitForFixedUpdate)
                {
                    coroutine._runPhase = RunPhase.FixedUpdate;
                }
                else if (co.Current is WaitForLateUpdate)
                {
                    coroutine._runPhase = RunPhase.LateUpdate;
                }
                else if (co.Current is CustomYieldInstruction)
                {
                    coroutine._state = CoroutineState.Waiting_CustomYield;
                }
                else if (co.Current is IEnumerator)
                {
                    //TODO: if we are waiting on an enumator that is not already in our system then we need to add it
                    // this can happen when someone starts a coroutine using
                    // e.g. "yield return Moveit()" vs
                    //      "yield return StartCoroutine(Moveit())"

                    coroutine._state = CoroutineState.Waiting_Coroutine;
                    coroutine._waitCoroutineId = GetCoroutineIdFromEnumerator(co.Current as IEnumerator);
                }
            }
        }

        private ulong GetCoroutineIdFromEnumerator(IEnumerator enumerator)
        {
            foreach(var c in _coroutines)
            {
                if (c._enumerator == enumerator)
                    return c._handle._id;
            }
            foreach (var c in _startList)
            {
                if (c._enumerator == enumerator)
                    return c._handle._id;
            }
            return CoroutineHandle.InvalidHandle._id;
        }

 
        private void CoroutineFinished(CoroutineData coroutine)
        {
            //Add to the kill list to be processed at the end of the frame
            //do we need this since we are iterating a copy of the runner list?
            _killList.Add(coroutine);

            coroutine._state = CoroutineState.Finished;
            if (coroutine._finishedAction != null)
            {
                coroutine._finishedAction();
            }

            if (BreakOnFinished)
            {
                Debug.Log("Coroutine Finished: " + coroutine.ToString());
                Debug.Break();
            }

            //check for any coroutines waiting on this one. Remove them from the wait list and
            //immediately trigger them (ie call RunCoroutineStep with them)
            foreach(var co in _coroutines)
            {
                if (co._state == CoroutineState.Waiting_Coroutine && co._waitCoroutineId == coroutine._handle._id)
                {
                    co._state = CoroutineState.Running;
                    RunCoroutineStep(co, RunPhase.Update);
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
                return CoroutineHandle.InvalidHandle;
            }

            return Instance.Start(coroutine);

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

            //TODO: context, tag, layer

            CoroutineData coroutineData = new CoroutineData(coroutineHandle, coroutine);
            coroutineData._state = CoroutineState.Running;
            coroutineData._enumerator = coroutine;
            coroutineData._runPhase = RunPhase.Update;

            _startList.Add(coroutineData);

            RunCoroutineStep(coroutineData, RunPhase.Update);

            return coroutineHandle;
        }

        public void StopCoroutine(CoroutineHandle handle)
        {
            foreach (var c in _coroutines)
            {
                if (c._handle._id == handle._id)
                {
                    StopCoroutineInternal(c);
                    break;
                }
            }
        }

        private void StopCoroutineInternal(CoroutineData coroutine)
        {
            if (coroutine._stopAction != null)
            {
                coroutine._stopAction();
            }

            CoroutineCleanup(coroutine);
        }

        private void CoroutineCleanup(CoroutineData coroutine)
        { 
            //Remove from context lists
            foreach (var contextList in _context)
            {
                if (contextList.Value.Contains(coroutine._handle)) contextList.Value.Remove(coroutine._handle);
            }

            //Remove from tag lists
            foreach (var tagList in _tags)
            {
                if (tagList.Value.Contains(coroutine._handle)) tagList.Value.Remove(coroutine._handle);
            }

            //Remove from layer lists
            foreach (var layerList in _layers)
            {
                if (layerList.Value.Contains(coroutine._handle)) layerList.Value.Remove(coroutine._handle);
            }

            _coroutines.Remove(coroutine);
        }

        public void StopCoroutines(List<CoroutineHandle> coroutines)
        {
            foreach (var c in coroutines) StopCoroutine(c);
        }

        public void StopAllCoroutines()
        {
            foreach (var c in _coroutines)
            {
                StopCoroutineInternal(c);
            }
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

        private bool CoroutineIsOnKillList(CoroutineHandle handle)
        {
            foreach(var c in _killList)
            {
                if (c._handle._id == handle._id) return true;
            }
            return false;
        }

        public void PauseCoroutine(CoroutineHandle handle)
        {
            if (CoroutineIsOnKillList(handle)) return;

            var c = GetCoroutineByHandle(handle);

            //if this coroutine is waiting on another coroutine then pause that one too
            if (c._state == CoroutineState.Waiting_Coroutine)
            {
                PauseCoroutine(new CoroutineHandle(c._waitCoroutineId));
            }
            c._paused = true;
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
            CoroutineData c = GetCoroutineByHandle(handle);
            if (c == null) return;//TODO: error

            //c._state = CoroutineState.Running;
            c._paused = false;

            //if this coroutine is waiting on another coroutine then resume that one too
            if (c._state == CoroutineState.Waiting_Coroutine)
            {
                ResumeCoroutine(new CoroutineHandle(c._waitCoroutineId));
            }
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
            CoroutineData c = GetCoroutineByHandle(handle);
            if (c == null) return false;//TODO: error

            return c._paused;
        }

        //Get the active state of a coroutine
        public bool GetCoroutineActive(CoroutineHandle handle) { return false; }

        //Set the callback for whendd a coroutine is stopped
        public void SetCoroutineOnStop(CoroutineHandle handle, Action callback) {
            CoroutineData c = GetCoroutineByHandle(handle);
            if (c == null) return;//TODO: error
            c._stopAction = callback;
        }

        private CoroutineData GetCoroutineByHandle(CoroutineHandle handle)
        {
            foreach(var c in _coroutines)
            {
                if (c._handle._id == handle._id) return c;
            }
            return null;
        }

        //Set the callback for when a coroutine is finished  
        public void SetCoroutineOnFinished(CoroutineHandle handle, Action callback) {
            CoroutineData c = GetCoroutineByHandle(handle);
            if (c == null) return;//TODO: error
            c._finishedAction = callback;
        }

        //////////////////////////////////////////////////////////////////////////////

        private void Initialize()
        {
            if (_updater == null)
            {
                //TODO: only in play mode?
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
          //  string debugInfo = string.Empty;
            CoroutineData coroutine = GetCoroutineByHandle(coroutineHandle);
            if (coroutine == null) return null;

            IEnumerator c = coroutine._enumerator;
            Type typ = c.GetType();
            FieldInfo type = typ.GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance);
            int stateValue = (int)type.GetValue(c);

         //   debugInfo += "State: " + stateValue + "\n";


            return d.GetSourceInfo(c, stateValue);
        }

        public IEnumerator GetCoroutineEnumerator(CoroutineHandle coroutineHandle)
        {
            CoroutineData coroutine = GetCoroutineByHandle(coroutineHandle);
            if (coroutine == null) return null;//TODO: error
            return coroutine._enumerator;
        }

        public CoroutineHandle GetCoroutineHandle(IEnumerator enumerator)
        {
            foreach(var c in _coroutines)
            {
                if (c._enumerator == enumerator) return c._handle;
            }
            return CoroutineHandle.InvalidHandle;
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
            if (coroutineDebug != null)
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
            CoroutineData coroutine = GetCoroutineByHandle(coroutineHandle);
            if (coroutine == null) return 0.0f;//TODO: error
            float waitTimeRemaining = 0.0f;
            if (coroutine._state == CoroutineState.Waiting_Time)
            {
                waitTimeRemaining = coroutine._waitEndTime - Time.time;
            }
            else if (coroutine._state == CoroutineState.Waiting_Realtime)
            {
                waitTimeRemaining = coroutine._waitEndTime - Time.time;
            }
            return waitTimeRemaining;
        }

        public List<CoroutineHandle> GetCoroutineStack(CoroutineHandle coroutineHandle)
        {
            List<CoroutineHandle > stack = new List<CoroutineHandle>();

            //first get to the root coroutine and then walk down the stack
            CoroutineHandle rootCoroutine = coroutineHandle;
            while (true)
            {
                CoroutineHandle parentHandle = GetCoroutineParent(rootCoroutine);
                if (parentHandle._id == 0)
                {
                    break;
                }
                rootCoroutine = parentHandle;
            }

            //Then starting at the root coroutine walk down the stack checking if there is
            //a coroutine waiting on it
            CoroutineHandle currentHandle = rootCoroutine;

            while (currentHandle._id != 0)
            {
                stack.Add(currentHandle);
                currentHandle = GetCoroutineChild(currentHandle);
            }

            return stack;
        }

        private CoroutineHandle GetCoroutineChild(CoroutineHandle currentHandle)
        {
            CoroutineData c = GetCoroutineByHandle(currentHandle);
            if (c == null) return CoroutineHandle.InvalidHandle;
            if (c._state == CoroutineState.Waiting_Coroutine)
            {
                return new CoroutineHandle(c._waitCoroutineId);
            }
            return CoroutineHandle.InvalidHandle;
        }

        private CoroutineHandle GetCoroutineParent(CoroutineHandle currentHandle)
        {
            foreach(var c in _coroutines)
            {
                if (c._state == CoroutineState.Waiting_Coroutine && c._waitCoroutineId == currentHandle._id)
                {
                    return c._handle;
                }
            }

            return CoroutineHandle.InvalidHandle;
        }
    }
}