using System;
using System.Collections;
using aeric.coroutinery;
using UnityEngine;

namespace aeric.demos {
    public class BasicTestUnity : MonoBehaviour {
        
        private void Start() {
            int i = 23324;

            var t = NestedTest();
            
            Type typ = t.GetType();
            Debug.Log(typ.Name);
            
            StartCoroutine(t);
        }

        //All the code needed to convert from regular Unity coroutines?
/*        private new IEnumerator StartCoroutine(IEnumerator cocoForCoroutines)
        {
            CoroutineManager.StartCoroutine(cocoForCoroutines);
            return cocoForCoroutines;
        }
*/

        public IEnumerator NestedTest() {
            Debug.Log("<color=green> 1a" + " @ " + Time.frameCount + "</color>");
            yield return new WaitForSeconds(1.0f);
            Debug.Log("<color=green> 1b" + " @ " + Time.frameCount + "</color>");
            
            
            yield return StartCoroutine(MoveIt());
            Debug.Log("<color=green> 2b"+ " @ " + Time.frameCount + "</color>");
            yield return StartCoroutine(SpinIt());
            Debug.Log("<color=green> 3c"+ " @ " + Time.frameCount + "</color>");
        }
        
        public IEnumerator SpinIt() {
            Debug.Log("<color=green> spin"+ " @ " + Time.frameCount + "</color>");
            
            yield return new WaitForSeconds(1.0f);

            Debug.Log("<color=green> go spin"+ " @ " + Time.frameCount + "</color>");

            float startT = Time.time;
            while ((Time.time - startT) < 2.0f) {
                this.transform.localRotation *= Quaternion.Euler(0,2,0);
                yield return new WaitForEndOfFrame();
            }
            
            Debug.Log("<color=green> end spin"+ " @ " + Time.frameCount + "</color>");
        }

        public IEnumerator MoveIt() {
            Debug.Log("<color=green> start"+ " @ " + Time.frameCount + "</color>");
            
            yield return new WaitForSeconds(1.0f);

            Debug.Log("<color=green> go move"+ " @ " + Time.frameCount + "</color>");
            
            while (this.transform.localPosition.y < 4.0f) {
                this.transform.localPosition += Vector3.up * 0.05f;
                yield return new WaitForEndOfFrame();
            }
            
            Debug.Log("<color=green> end move"+ " @ " + Time.frameCount + "</color>");
        }
    }
}