using UnityEngine;

namespace aeric.coroutinery_demos {
    public class MoveTarget : MonoBehaviour {
        public int CapturedTeamIndex;
        public float captureIndicatorTimer;
        
        public ParticleSystem captureVFX;
        public GameObject captureIndicator;
        
        //component reference caching
        private Material _material;
        private Material _captureIndicatorMaterial;

        public Material _vfxMaterialBlue;
        public Material _vfxMaterialRed;

        private void Awake() {
            _material = GetComponent<MeshRenderer>().material;

            if (captureIndicator != null) {
                captureIndicator.SetActive(false);
                _captureIndicatorMaterial = captureIndicator.GetComponent<MeshRenderer>().material;
            }
        }

        private void Update() {
            //lerp color back to the team color
            _material.color = Color.Lerp(_material.color, RobotLevel._instance.GetTeamColor(CapturedTeamIndex), Time.deltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime);

            if (captureIndicatorTimer > 0.0f) {
                captureIndicator.transform.rotation = Quaternion.Euler(0.0f, captureIndicatorTimer * 200.0f,0.0f);
                
                captureIndicatorTimer -= Time.deltaTime;
                if (captureIndicatorTimer < 0.0f) {
                    captureIndicator.SetActive(false);
                }
            }
        }

        public void Capture(Robot robot) {
            CapturedTeamIndex = robot.Team.teamIndex;

            //turn white and scale up briefly when captured
            _material.color = Color.white;
            transform.localScale = new Vector3(1.5f, 1.8f, 1.5f);

            if (robot.Team.teamIndex == 2)
               captureVFX.GetComponent<Renderer>().material = _vfxMaterialRed;
            else
               captureVFX.GetComponent<Renderer>().material = _vfxMaterialBlue;
           
            captureVFX.Play();

            if (captureIndicator != null) {
                captureIndicator.SetActive(true);
                _captureIndicatorMaterial.color = robot.Team.teamColor;
                captureIndicatorTimer = 2.0f;
            }
        }
    }
}