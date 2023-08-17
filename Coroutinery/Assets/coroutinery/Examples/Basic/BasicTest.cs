using System;
using System.Collections;
using aeric.coroutinery;
using UnityEngine;

namespace aeric.demos {
    public class BasicTest : MonoBehaviour {
        
        private void Start() {
            int i = 122;

            var t = NestedTest();
            
            Type typ = t.GetType();
            Debug.Log(typ.Name);
            
            StartCoroutine(t);
        }

        //All the code needed to convert from regular Unity coroutines?
        private new IEnumerator StartCoroutine(IEnumerator cocoForCoroutines)
        {
            CoroutineManager.StartCoroutine(cocoForCoroutines);
            return cocoForCoroutines;
        }


        public IEnumerator NestedTest() {
            Debug.Log("1a" + " @ " + Time.frameCount);
            yield return new WaitForSeconds(1.0f);
            Debug.Log("1b" + " @ " + Time.frameCount);
       
            yield return StartCoroutine(MoveIt());
            Debug.Log("2b"+ " @ " + Time.frameCount);
            yield return StartCoroutine(SpinIt());
            Debug.Log("3c"+ " @ " + Time.frameCount);
        }
        
        public IEnumerator SpinIt() {
            Debug.Log("spin"+ " @ " + Time.frameCount);
            
            yield return new WaitForSeconds(1.0f);

            Debug.Log("go spin"+ " @ " + Time.frameCount);

            float startT = Time.time;
            while ((Time.time - startT) < 2.0f) {
                this.transform.localRotation *= Quaternion.Euler(0,2,0);
                yield return new WaitForEndOfFrame();
            }
            
            Debug.Log("end spin"+ " @ " + Time.frameCount);
        }

        public IEnumerator MoveIt() {
            Debug.Log("start"+ " @ " + Time.frameCount);
            
            yield return new WaitForSeconds(1.0f);

            Debug.Log("go move"+ " @ " + Time.frameCount);
            
            while (this.transform.localPosition.y < 4.0f) {
                this.transform.localPosition += Vector3.up * 0.05f;
                yield return new WaitForEndOfFrame();
            }
            
            Debug.Log("end move"+ " @ " + Time.frameCount);
        }
    }
}