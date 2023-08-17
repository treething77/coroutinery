using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace aeric.coroutinery {
    public static class ReflectionExtensions {
        public static T GetFieldValue<T>(this object obj, string name) {
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
    
    public struct CoroutineHandle
    {
        public readonly IEnumerator _enumerator;
        public CoroutineHandle(IEnumerator coroutine)
        {
            _enumerator = coroutine;
        }
    }

    public static class WaitStatics
    {
        public static WaitForEndOfFrame _WaitForEndOfFrame = new WaitForEndOfFrame();
    }

    public class CoroutineUpdater : MonoBehaviour {
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
                yield return WaitStatics._WaitForEndOfFrame;
                _manager.RunEndOfFrame();
            }
        }
        
        private void Update() {
            CoroutineManager.RunCoroutines();
        }
    }
    
    public class CoroutineManager
    {
        private static CoroutineManager _manager;

        private CoroutineUpdater _updater;

        private List<IEnumerator> _coroutines = new List<IEnumerator>();
        private List<IEnumerator> _killList = new List<IEnumerator>();

        public static void CreateManager()
        {
            //TODO: error + early out if not null
            _manager = new CoroutineManager();
        }

        private FieldInfo waitForSecondValue;

        public CoroutineManager()
        {
            waitForSecondValue = ReflectionExtensions.GetFieldInfo<float>(typeof(WaitForSeconds), "m_Seconds");
        }

        //TODO: move static methods into own static class, purely for API?
        public static void RunCoroutines()
        {
            //GC
            _manager.Run();
        }

        private void Run()
        {
            int numCoroutines = _coroutines.Count;
            for (int i=0;i<numCoroutines;i++)
            {
                IEnumerator co = _coroutines[i];
                RunCoroutine(co);
            }
            
            //no GC
            //handle kill list
            foreach(var kill in _killList)
                _coroutines.Remove(kill);
            _killList.Clear();
        }

        private float waitTimerStart = -1.0f;
        private List<IEnumerator> _endOfFrameRunners = new List<IEnumerator>();

        private void RunCoroutine(IEnumerator co)
        {
            bool moveNext = true;
            
            //are we waiting on something?
            if (co.Current != null)
            {
                moveNext = false;
                if (co.Current is WaitForSeconds)
                {
                    WaitForSeconds w = co.Current as WaitForSeconds;
                    
                    //use reflection to get the internal state for the seconds
                    
                    //then we need to keep an internal timer of how long we've been waiting for 

                    float waitForSeconds = w.GetFieldValue<float>(waitForSecondValue);

                    //Unity WaitForSeconds appears to use Time.time comparison rather than accumulating deltaTime
                    //TODO: cache Time.time
                    if (waitTimerStart < 0.0f) waitTimerStart = Time.time;
                    
                    //TODO: need to associate a timer with each instance
                    //shove it into the coroutine handle?
                    //waitTimerStart += Time.deltaTime;
                    
                    if ((Time.time - waitTimerStart) >= waitForSeconds)
                    {
                        waitTimerStart = -1.0f;
                        moveNext = true;
                    }
                    
                 //   Debug.Log($"WaitForSeconds - {waitTimer} - {waitForSeconds}");
                }
                else if (co.Current is WaitForEndOfFrame)
                {
                    //TODO: actually wait for the end of the frame
                    //moveNext = true;
                    _endOfFrameRunners.Add(co);
                }
                else
                {
                    if (co.Current is IEnumerator)
                    {
                        //if this is another coroutine then we just need to know if its done
                        //TODO: 
                        // could we handle this by having the coroutine handle be itself a YieldInstruction?
                        // then we can set a flag in the handle to say its finished
                        bool exists = false;
                        foreach (var c in _coroutines)
                        {
                            if (c == co.Current) exists = true;
                        }
                        
                        if (!exists)
                        {
                            Debug.Log(" we were waiting on a coroutine that seems to be finished" + " @ " + Time.frameCount);
                            moveNext = true;
                        }
                        
                    }
                }
            }

            {
                if (moveNext)
                {
                    if (!co.MoveNext())
                    {
                        _killList.Add(co);
                        
                        //check for any coroutines waiting on this one. Remove them from the wait list and 
                        //immediately trigger them (ie call RunCoroutine with them)
                        
                    }
                    else
                    {
                        //if we are waiting on a timer, start it now?
                        if (co.Current is WaitForSeconds)
                        {
                            if (waitTimerStart < 0.0f) waitTimerStart = Time.time;
                        }
                        else if (co.Current is WaitForEndOfFrame)
                        {
                            //if we yielded on a WaitForEndOfFrame then make sure we queue it up to 
                            //continue within the same frame
                            _endOfFrameRunners.Add(co);
                        }
                        //if we are waiting on another coroutine then put this coroutine onto a wait list
                        //to be triggered when that coroutine finishes
                    }
                }
            }
        }
        
        public void RunEndOfFrame()
        {
            foreach (var co in _endOfFrameRunners)
            {
                if (!co.MoveNext())
                {
                    _killList.Add(co);
                }
            }
            
            _endOfFrameRunners.Clear();
        }

        // Keep the same interface as the Unity version
        public static CoroutineHandle StartCoroutine(IEnumerator coroutine) {
            if (coroutine == null) {
                //TODO: error
                return default(CoroutineHandle);
            }

            if (_manager == null) {
                CreateManager();
            }

            _manager.Initialize();

            //TODO: push coroutine handle onto list rather than enumerator
            _manager.Start(coroutine);
            
            //TODO: did it immediately end?
            
            return new CoroutineHandle(coroutine);
        }

        private void Start(IEnumerator coroutine)
        {
            _coroutines.Add(coroutine);
            
            RunCoroutine(coroutine);
            
            //TODO: did it end?
        }

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