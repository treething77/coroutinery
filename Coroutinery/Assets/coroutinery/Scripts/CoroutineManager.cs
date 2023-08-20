using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;
using Object = UnityEngine.Object;

namespace aeric.coroutinery
{
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
        public static WaitForEndOfFrame _WaitForEndOfFrame = new();
        public static WaitForFixedUpdate _WaitForFixedUpdate = new();
        public static WaitForLateUpdate _WaitForLateUpdate = new();


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

        private CoroutineUpdater _updater;

        //This is used to emulate an oddity of the Unity behavior where a coroutine that yields in the fixed update does not
        //run again on the same frame even though it seems like it should since Update runs after FixedUpdate
        //so we introduce an artificial delay of one frame for coroutines that yield in the fixed update
        private List<CoroutineHandle> _pendingList = new();


        private List<CoroutineHandle> _activeList = new();
        private List<CoroutineHandle> _pausedList = new();
        private List<CoroutineHandle> _killList = new();//temp list of coroutines to kill

        private Dictionary<CoroutineHandle, IEnumerator> _enumeratorLookup = new();
        private Dictionary<IEnumerator, CoroutineHandle> _handleLookup = new();

        private Dictionary<string, List<CoroutineHandle>> _tags = new();
        private Dictionary<int, List<CoroutineHandle>> _layers = new();
        private Dictionary<Object, List<CoroutineHandle>> _context = new();

        private ulong _nextId = 0;
                
        //wait lists - coroutines that yielded on things we are waiting to complete
        private List<CoroutineHandle> _endOfFrameRunners = new();
        private List<CoroutineHandle> _fixedUpdateRunners = new();
        private List<CoroutineHandle> _lateUpdateRunners = new();

        private FieldInfo waitForSecondValue;

        struct WaitTimer
        {
            public float endTime;
            public CoroutineHandle handle;
        }

        private List<WaitTimer> _waitTimers = new();

        struct WaitCoroutine
        {
            public CoroutineHandle subcoroutine;
            public CoroutineHandle handle;
        }

        private List<WaitCoroutine> _waitCoroutines = new();

        private List<CoroutineHandle> _customYieldList = new();


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

                    WaitTimer wt = new();
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

                        WaitCoroutine wc = new();
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

        private void removeFromRunnerLists(CoroutineHandle handle)
        {
            if (_pendingList.Contains(handle)) _pendingList.Remove(handle);
            if (_activeList.Contains(handle)) _activeList.Remove(handle);
            if (_endOfFrameRunners.Contains(handle)) _endOfFrameRunners.Remove(handle);
            if (_fixedUpdateRunners.Contains(handle)) _fixedUpdateRunners.Remove(handle);
            if (_lateUpdateRunners.Contains(handle)) _lateUpdateRunners.Remove(handle);
        }

        private void CoroutineFinished(IEnumerator co, CoroutineHandle handle)
        {
            //Add to the kill list to be processed at the end of the frame
            //do we need this since we are iterating a copy of the runner list?
            _killList.Add(handle);

            removeFromRunnerLists(handle);

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
            }

            _manager.Initialize();

            //TODO: push coroutine handle onto list rather than enumerator
            return _manager.Start(coroutine);

            //TODO: did it immediately end?

        }

        //////////////////////////////////////////////////////////////////////////////
        //PUBLIC API
        public CoroutineHandle Start(IEnumerator coroutine, [CanBeNull] Object context = null, [CanBeNull] string tag = null, int layer = 0)
        {
            _nextId++;
            CoroutineHandle coroutineHandle = new CoroutineHandle(_nextId);

            _activeList.Add(coroutineHandle);
            _enumeratorLookup.Add(coroutineHandle, coroutine);
            _handleLookup.Add(coroutine, coroutineHandle);

            RunCoroutineStep(coroutineHandle, RunPhase.Update);

            return coroutineHandle;
        }

        public void StopCoroutine(CoroutineHandle handle)
        {
            //remove from _activeList, lookups, kill list etc
            removeFromRunnerLists(handle);

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
        }

        public void StopCoroutinesByTag(string tag)
        {
            StopCoroutines(GetCoroutinesByTag(tag));
        }

        public void StopCoroutinesByLayer(int layer)
        {
            StopCoroutines(GetCoroutinesByLayer(layer));
        }

        public void StopCoroutinesByContext(Object context)
        {
            StopCoroutines(GetCoroutinesByContext(context));
        }

        public List<CoroutineHandle> GetCoroutinesByTag(string tag)
        {
            return new List<CoroutineHandle>();
        }

        public List<CoroutineHandle> GetCoroutinesByLayer(int layer)
        {
            return new List<CoroutineHandle>();
        }

        public List<CoroutineHandle> GetCoroutinesByContext(Object context)
        {
            return new List<CoroutineHandle>();
        }

        public void PauseCoroutine(CoroutineHandle handle)
        {
        }

        public void PauseCoroutinesByTag(string tag)
        {
            PauseCoroutines(GetCoroutinesByTag(tag));
        }

        public void PauseCoroutines(List<CoroutineHandle> coroutines)
        {
            foreach (var c in coroutines) PauseCoroutine(c);
        }

        public void PauseCoroutinesByLayer(int layer)
        {
            PauseCoroutines(GetCoroutinesByLayer(layer));
        }

        public void PauseCoroutinesByContext(Object context)
        {
            PauseCoroutines(GetCoroutinesByContext(context));
        }

        public void ResumeCoroutine(CoroutineHandle handle) { }

        public void ResumeCoroutines(List<CoroutineHandle> coroutines)
        {
            foreach (var c in coroutines) ResumeCoroutine(c);
        }

        public void ResumeCoroutinesByTag(string tag)
        {
            ResumeCoroutines(GetCoroutinesByTag(tag));
        }

        public void ResumeCoroutinesByLayer(int layer)
        {
            ResumeCoroutines(GetCoroutinesByLayer(layer));
        }

        public void ResumeCoroutinesByContext(Object context)
        {
            ResumeCoroutines(GetCoroutinesByContext(context));
        }

        //Set the context of a coroutine
        public void SetCoroutineContext(CoroutineHandle handle, Object context) { }

        //Get the context of a coroutine
        public Object GetCoroutineContext(CoroutineHandle handle) { return null; }

        //Set the tag of a coroutine
        public void SetCoroutineTag(CoroutineHandle handle, string tag) { }

        //Get the tag of a coroutine
        public string GetCoroutineTag(CoroutineHandle handle) { return null; }

        //Set the layer of a coroutine
        public void SetCoroutineLayer(CoroutineHandle handle, int layer) { }

        //Get the layer of a coroutine
        public int GetCoroutineLayer(CoroutineHandle handle) { return 0; }

        //Get the paused state of a coroutine
        public bool GetCoroutinePaused(CoroutineHandle handle) { return false; }

        //Get the active state of a coroutine
        public bool GetCoroutineActive(CoroutineHandle handle) { return false; }

        //Set the execution order of a coroutine
        public void SetCoroutineExecutionOrder(CoroutineHandle handle, int order) { }

        //Get the execution order of a coroutine
        public int GetCoroutineExecutionOrder(CoroutineHandle handle) { return 0; }

        //Set the callback for when a coroutine is stopped
        public void SetCoroutineOnStop(CoroutineHandle handle, Action callback) { }

        //Set the callback for when a coroutine is finished
        public void SetCoroutineOnFinished(CoroutineHandle handle, Action callback) { }

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

    }
}