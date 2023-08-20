using System;
using System.Collections;
using System.Collections.Generic;
using aeric.coroutinery;
using UnityEngine;

namespace aeric.demos
{
    public class UnityEmulationTestRunner : MonoBehaviour
    {
        public UnityEmulationTest unityTest;
        public UnityEmulationTest coroutineryTest;

        private bool testsEvaluated = false;

        public void Start()
        {
            unityTest.RunTest();
            coroutineryTest.RunTest();
        }

        private void Update()
        {
            if (!testsEvaluated && unityTest.testComplete && coroutineryTest.testComplete)
            {
                EvaluateTests();
                testsEvaluated = true;
            }
        }

        private void EvaluateTests()
        {
            Dictionary<string, int> unityTimings = unityTest.frameTimings;
            Dictionary<string, int> coroutineryTimings = coroutineryTest.frameTimings;

            bool passed = true;

            foreach (var key in unityTimings.Keys)
            {
                int unityFrame = unityTimings[key];
                int coroutineryFrame = coroutineryTimings[key];

                if (unityFrame != coroutineryFrame)
                {
                    Debug.LogError("Timing mismatch for " + key + ": Unity=" + unityFrame + ", Coroutinery=" + coroutineryFrame);
                    passed = false;
                }
            }

            if (passed)
            {
                Debug.Log("<color=green>Timing test passed!</color>");
            }
            else
            {
                Debug.LogError("Timing test failed!");
            }

            //Compare the transform state of the two objects
            if (unityTest.transform.position.y != coroutineryTest.transform.position.y)
            {
                Debug.LogError("Transform Y mismatch: Unity=" + unityTest.transform.position.y + ", Coroutinery=" + coroutineryTest.transform.position.y);
            }
            else
            {
                Debug.Log("<color=green>Transform position test passed!</color>");
            }
            if (unityTest.rotCnt != coroutineryTest.rotCnt)
            {
                Debug.LogError("Transform mismatch: Unity=" + unityTest.rotCnt + ", Coroutinery=" + coroutineryTest.rotCnt);
            }
            else
            {
                Debug.Log("<color=green>Transform rotation test passed!</color>");
            }
        }

    }
}