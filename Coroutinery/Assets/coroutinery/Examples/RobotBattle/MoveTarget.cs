using aeric.coroutinery;
using System.Collections;
using UnityEngine;

namespace aeric.coroutinery_demos
{

    public class GameObjectCoroutineContext : MonoBehaviour
    {
        public void OnDestroy()
        {
            CoroutineManager.Instance.StopAllCoroutinesWithContext(gameObject);
        }

        public void OnDisable()
        {
            CoroutineManager.Instance.PauseAllCoroutinesWithContext(gameObject);
        }

        public void OnEnable()
        {
            CoroutineManager.Instance.ResumeAllCoroutinesWithContext(gameObject);
        }
    }

    public class MoveTarget : GameObjectCoroutineContext
    {
        public int CapturedTeamIndex;
        public float captureIndicatorTimer;

        public ParticleSystem captureVFX;
        public GameObject captureIndicator;

        //component reference caching
        private Material _material;
        private Material _captureIndicatorMaterial;

        public Material _vfxMaterialBlue;
        public Material _vfxMaterialRed;
        private CoroutineHandle _bounceCoroutine;
        private CoroutineHandle _scaleCoroutine;

        private void Awake()
        {
            _material = GetComponent<MeshRenderer>().material;

            if (captureIndicator != null)
            {
                captureIndicator.SetActive(false);
                _captureIndicatorMaterial = captureIndicator.GetComponent<MeshRenderer>().material;
            }

            _bounceCoroutine = CoroutineManager.StartCoroutine(Bounce());
            CoroutineManager.Instance.SetCoroutineContext(_bounceCoroutine, gameObject);
        }

        private IEnumerator Bounce()
        {
            float time = Random.value;
            Vector3 startPos = transform.localPosition;
            while (true)
            {
                time += Time.deltaTime * 4.0f;

                //use Sin to make the object bob up and down
                float y = Mathf.Abs(Mathf.Sin(time) * 0.5f);

                transform.localPosition = startPos + new Vector3(0, y, 0);

                yield return YieldStatics._WaitForEndOfFrame;
                time += Time.deltaTime * 0.0f;

                yield return YieldStatics._WaitForEndOfFrame;
            }
        }

        private void Update()
        {
            //lerp color back to the team color
            _material.color = Color.Lerp(_material.color, RobotLevel.Instance.GetTeamColor(CapturedTeamIndex), Time.deltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime);

            if (captureIndicatorTimer > 0.0f)
            {
                captureIndicator.transform.rotation = Quaternion.Euler(0.0f, captureIndicatorTimer * 200.0f, 0.0f);

                captureIndicatorTimer -= Time.deltaTime;
                if (captureIndicatorTimer < 0.0f)
                {
                    captureIndicator.SetActive(false);
                }
            }
        }

        private IEnumerator ScaleUp()
        {
            float time = 0.0f;
            while (time < 0.2f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1.5f, 1.8f, 1.5f), time / 0.2f);
                yield return null;
                time += Time.deltaTime;
            }
            while (time < 0.2f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, time / 0.2f);
                yield return null;
                time += Time.deltaTime;
            }
            CoroutineManager.Instance.ResumeCoroutine(_bounceCoroutine);
        }

        public void Capture(Robot robot)
        {
            CapturedTeamIndex = robot.Team.teamIndex;

            //turn white and scale up briefly when captured
            _material.color = Color.white;

            CoroutineManager.Instance.StopCoroutine(_scaleCoroutine);
            CoroutineManager.Instance.PauseCoroutine(_bounceCoroutine);

            _scaleCoroutine = CoroutineManager.StartCoroutine(ScaleUp());
            CoroutineManager.Instance.SetCoroutineContext(_scaleCoroutine, gameObject);

            CoroutineManager.Instance.SetCoroutineOnStop(_scaleCoroutine, () =>
            {
                transform.localScale = Vector3.one;
            });


            //  transform.localScale = new Vector3(1.5f, 1.8f, 1.5f);

            if (robot.Team.teamIndex == 2)
                captureVFX.GetComponent<Renderer>().material = _vfxMaterialRed;
            else
                captureVFX.GetComponent<Renderer>().material = _vfxMaterialBlue;

            captureVFX.Play();

            if (captureIndicator != null)
            {
                captureIndicator.SetActive(true);
                _captureIndicatorMaterial.color = robot.Team.teamColor;
                captureIndicatorTimer = 2.0f;
            }
        }
    }
}