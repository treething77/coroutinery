using aeric.coroutinery;
using System.Collections;
using UnityEngine;

namespace aeric.demos
{
    public class PauseTest : MonoBehaviour
    {
        private CoroutineHandle coroutineHandle;

        public void Start()
        {
            coroutineHandle = CoroutineManager.StartCoroutine(MoveIt());
        }

        public void Pause()
        {
            CoroutineManager.Instance.PauseCoroutine(coroutineHandle);
        }

        public void Resume()
        {
            CoroutineManager.Instance.ResumeCoroutine(coroutineHandle);
        }

        public IEnumerator MoveIt()
        {
            float dir = 5.0f;
            while (true)
            {
                transform.localPosition += new Vector3(0, dir * Time.deltaTime, 0);

                if (transform.localPosition.y >= 3.0f)
                {
                    dir = -5.0f;
                }
                else if (transform.localPosition.y <= -3.0f)
                {
                    dir = 5.0f;
                }

                yield return null;
            }
        }

    }
}