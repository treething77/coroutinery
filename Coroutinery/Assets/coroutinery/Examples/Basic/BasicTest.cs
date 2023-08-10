using System;
using System.Collections;
using UnityEngine;

namespace aeric.demos {
    public class BasicTest : MonoBehaviour {
        private void Start() {
            int i = 224;

            StartCoroutine(MoveIt());
        }

        public IEnumerator MoveIt() {
            Debug.Log("start");
            
            yield return new WaitForSeconds(2.0f);

            Debug.Log("go");
            
            while (this.transform.localPosition.y < 5.0f) {
                this.transform.localPosition += Vector3.up * 0.01f;
                yield return new WaitForEndOfFrame();
            }
            
            Debug.Log("end");
        }
    }
}
