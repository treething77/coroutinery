using System;
using System.Collections;
using System.Collections.Generic;
using aeric.coroutinery;
using UnityEngine;

namespace aeric.demos
{
    public class UnityEmulationTest : MonoBehaviour
    {
        public bool useCoroutinery = true;

        public bool testComplete;

        public Dictionary<string, int> frameTimings = new Dictionary<string, int>();

        private bool movingUp = false;
        private bool movingDown = false;

        public int rotCnt = 0;

        public void RunTest()
        {
            var nestedCoroutine = NestedTest();

            if (useCoroutinery)
                StartCoroutine(nestedCoroutine);
            else
                base.StartCoroutine(nestedCoroutine);
        }

        //All the code needed to convert from regular Unity coroutines to Coroutinery coroutines
        private new IEnumerator StartCoroutine(IEnumerator cocoForCoroutines)
        {
            var handle = CoroutineManager.StartCoroutine(cocoForCoroutines);
            CoroutineManager.Instance.SetCoroutineContext(handle, this.gameObject);
            return cocoForCoroutines;
        }

        private void RecordTiming(string label)
        {
            frameTimings[label] = Time.frameCount;

            string prefix = useCoroutinery ? "C " : "U ";
            string color = useCoroutinery ? "<color=green>" : "<color=red>";
            Debug.Log(color + prefix + label + " @ " + Time.frameCount + "</color>");
        }


        public IEnumerator NestedTest()
        {
            if (useCoroutinery)
                yield return StartCoroutine(BreakTest());
            else
                yield return base.StartCoroutine(BreakTest());
            
            yield return new WaitForFrames(1);

            RecordTiming("1a");

            yield return new WaitForSeconds(0.5f);
            RecordTiming("1b");

            if (useCoroutinery)
                yield return StartCoroutine(MoveIt());
            else
                yield return MoveIt();

            RecordTiming("2a");

            if (useCoroutinery)
                yield return StartCoroutine(SpinIt());
            else
                yield return base.StartCoroutine(SpinIt());

            RecordTiming("2b");
                 

            yield return new WaitForFrames(1);
            
            RecordTiming("3a");

            testComplete = true;
        }

        public IEnumerator BreakTest()
        {
            RecordTiming("break - start");

            yield break;
        }

        public IEnumerator SpinIt()
        {
            RecordTiming("spin - start");

            yield return new WaitForSeconds(0.5f);

            RecordTiming("spin - left");
            float startT = Time.time;
            while ((Time.time - startT) < 0.5f)
            {
                this.transform.localRotation *= Quaternion.Euler(0, 2, 0);
                yield return new WaitForEndOfFrame();
            }

            RecordTiming("spin - right");
            
            float startT2 = Time.time;
            while ((Time.time - startT2) < 0.5f)
            {
                this.transform.localRotation *= Quaternion.Euler(0, -2, 0);
                yield return new WaitForFixedUpdate();
            }
            
            RecordTiming("spin - up");

            float startT3 = Time.time;
            while ((Time.time - startT3) < 0.5f)
            {
                Debug.Log(Time.frameCount + " - " + Time.inFixedTimeStep);

            //    RecordTiming("spin - " + Time.time);

                rotCnt++;
                this.transform.localRotation *= Quaternion.Euler(-2, 0, 0);
                yield return null;
            }

            RecordTiming("spin - end");
        }

        public IEnumerator MoveIt()
        {
            RecordTiming("move - start");

            yield return new WaitForSecondsRealtime(1.5f);

            RecordTiming("move - up");
            movingUp = true;
            yield return new WaitUntil(() => this.transform.localPosition.y >= 3.0f);
            movingUp = false;

            RecordTiming("move - down");
            movingDown = true;
            yield return new WaitWhile(() => this.transform.localPosition.y >= -1.0f);
            movingDown = false;

            RecordTiming("move - end");
        }

        private void Update()
        {
            if (movingUp)
            {
                this.transform.localPosition += Vector3.up * 0.05f;
            }
            if (movingDown)
            {
                this.transform.localPosition -= Vector3.up * 0.05f;
            }
        }
    }
}