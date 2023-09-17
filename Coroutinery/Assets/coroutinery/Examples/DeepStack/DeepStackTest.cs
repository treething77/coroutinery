using System.Collections;
using System.Text;
using aeric.coroutinery;
using UnityEngine;

namespace aeric.demos
{
    public class DeepStackTest : MonoBehaviour
    {
        private CoroutineHandle rootCoroutineHandle;

        public void Start()
        {
            //Start a coroutine that creates a deep stack.
            //Wait for the leaf coroutine to end, and the stack to unwind.
            //Then start the coroutine again.

            rootCoroutineHandle = CoroutineManager.StartCoroutine(RootCoroutine());
        }

        public void LogStackStatus()
        {
#if UNITY_EDITOR
            var debugInfo = CoroutineManager.LoadDebugInfo();
#else
            CoroutineDebugInfo debugInfo = null;
#endif

            CoroutineHandle coroutineHandle = rootCoroutineHandle;
            StringBuilder sb = new StringBuilder();
            string indentation = "";

            while (coroutineHandle.IsValid)
            {             
                string prettyName = CoroutineManager.Instance.GetCoroutinePrettyName(coroutineHandle, debugInfo);

                sb.AppendLine(indentation + prettyName);

                coroutineHandle = CoroutineManager.Instance.GetCoroutineChild(coroutineHandle);

                indentation += "  ";
            }

            Debug.Log(sb.ToString());
        }

        public IEnumerator RootCoroutine()
        {
            while (true)
            {
                Debug.Log("RootCoroutine: Starting stack coroutine.");

                //Start a coroutine that creates a deep stack to a semi-random depth
                int stackDepth = Random.Range(1, 100);
                yield return StartCoroutine(StackCoroutine(0, stackDepth));
                
                Debug.Log("RootCoroutine: Stack coroutine ended.");
            }
        }

        private new IEnumerator StartCoroutine(IEnumerator cocoForCoroutines)
        {
            var handle = CoroutineManager.StartCoroutine(cocoForCoroutines);
            CoroutineManager.Instance.SetCoroutineContext(handle, this.gameObject);
            return cocoForCoroutines;
        }

        public IEnumerator StackCoroutine(int depth, int maxDepth)
        {
            if (depth < maxDepth)
            {
                //Start a coroutine that creates a deep stack.
                yield return StartCoroutine(StackCoroutine(depth + 1, maxDepth));
            }
            else
            {
                //Start a coroutine that moves the object up and down.
                yield return StartCoroutine(MoveIt());
            }
        }

        public IEnumerator MoveIt()
        {
            float dir = 5.0f;
            while (transform.localPosition.y < 3.0f)
            {
                transform.localPosition += new Vector3(0, dir * Time.deltaTime, 0);
                yield return null;
            }
            dir = -5.0f;
            while (transform.localPosition.y > -3.0f)
            {
                transform.localPosition += new Vector3(0, dir * Time.deltaTime, 0);
                yield return null;
            }
        }

    }
}