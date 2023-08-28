using aeric.coroutinery;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace aeric.demos
{
    public class TagLayerSetup : MonoBehaviour
    {
        public List<GameObject> layer1 = new List<GameObject>();
        public List<GameObject> layer2 = new List<GameObject>();

        public List<GameObject> allRed = new List<GameObject>();
        public List<GameObject> allBlue = new List<GameObject>();

        void Start()
        {
            float offset = 0.0f;

            //start coroutine on all objects that makes them bob up and down
            foreach (GameObject obj in layer1)
            {
                var coroutineHandle = CoroutineManager.StartCoroutine(MoveIt(obj, 0.5f, offset));

                /*
                //TODO: this would be so much cleaner
                CoroutineManager.StartCoroutine(MoveIt(obj, 0.5f, offset)).SetTag(allRed.Contains(obj)? "red" : "blue" )
                                                                          .SetLayer(1)
                                                                          .SetContext(obj);

                */
                CoroutineManager.Instance.SetCoroutineLayer(coroutineHandle, 1);
                CoroutineManager.Instance.SetCoroutineTag(coroutineHandle, allRed.Contains(obj)? "red" : "blue" );
                CoroutineManager.Instance.SetCoroutineContext(coroutineHandle, obj);

                offset += 0.5f;
            }
            foreach (GameObject obj in layer2)
            {
                var coroutineHandle = CoroutineManager.StartCoroutine(MoveIt(obj, 0.5f, offset));

                CoroutineManager.Instance.SetCoroutineLayer(coroutineHandle, 2);
                CoroutineManager.Instance.SetCoroutineTag(coroutineHandle, allRed.Contains(obj) ? "red" : "blue");
                CoroutineManager.Instance.SetCoroutineContext(coroutineHandle, obj);

                offset -= 0.5f;
            }


            //add buttons to pause/resume by layer and by tag

            //add functionality to pause a coroutine and have it resume when another coroutine is finished
            //could do this by pausing the coroutine and then adding an onfinished callback to the new coroutine that resumes the old one
        }

        //Detect a mouse input, do a raycast into the scene and start a coroutine on that object that inherits the tag and layer of the object
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit))
                {
                    var coroutineHandle = CoroutineManager.StartCoroutine(SpinIt(hit.collider.gameObject));

                    var objectCoroutines = CoroutineManager.Instance.GetCoroutinesByContext(hit.collider.gameObject);
                    var tag = CoroutineManager.Instance.GetCoroutineTag(objectCoroutines[0]);
                    var layer = CoroutineManager.Instance.GetCoroutineLayer(objectCoroutines[0]);

                    CoroutineManager.Instance.SetCoroutineLayer(coroutineHandle, layer);
                    CoroutineManager.Instance.SetCoroutineTag(coroutineHandle, tag);
                    CoroutineManager.Instance.SetCoroutineContext(coroutineHandle, hit.collider.gameObject);
                }
            }
        }

        private IEnumerator SpinIt(GameObject gameObject)
        {
            float time = 0.0f;
            while (time < 2.0f)
            {
                gameObject.transform.localRotation *= Quaternion.Euler(0, 5, 0);
                yield return new WaitForSeconds(0.5f);
                time += Time.deltaTime;
            }
        }

        private IEnumerator MoveIt(GameObject obj, float moveDistance, float timeOffset)
        {
            float time = timeOffset;
            Vector3 startPos = obj.transform.localPosition;
            while (true)
            {
                time += Time.deltaTime * 4.0f;

                //use Sin to make the object bob up and down
                float y = Mathf.Sin(time) * moveDistance;
                                
                obj.transform.localPosition = startPos + new Vector3(0, y, 0);

                yield return YieldStatics._WaitForEndOfFrame;
                time += Time.deltaTime *0.0f;

                yield return YieldStatics._WaitForEndOfFrame;
            }
        }

        public void PauseLayer1()
        {
            CoroutineManager.Instance.PauseCoroutinesByLayer(1);
        }

        public void ResumeLayer1()
        {
            CoroutineManager.Instance.ResumeCoroutinesByLayer(1);
        }

        public void PauseLayer2()
        {
            CoroutineManager.Instance.PauseCoroutinesByLayer(2);
        }

        public void ResumeLayer2()
        {
            CoroutineManager.Instance.ResumeCoroutinesByLayer(2);
        }

        public void PauseRed()
        {
            CoroutineManager.Instance.PauseCoroutinesByTag("red");
        }

        public void ResumeRed()
        {
            CoroutineManager.Instance.ResumeCoroutinesByTag("red");
        }

        public void PauseBlue()
        {
            CoroutineManager.Instance.PauseCoroutinesByTag("blue");
        }

        public void ResumeBlue()
        {
            CoroutineManager.Instance.ResumeCoroutinesByTag("blue");
        }
    }
}
