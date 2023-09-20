using UnityEngine;
using UnityEngine.UI;

namespace aeric.coroutinery_demos
{   
    public class RobotDemo : MonoBehaviour {
        public static RobotDemo _instance;

        
        //inspector references
        public Text statusText;
        public GameObject stackParent;
        public GameObject targetsParent;

        public RectTransform livePanel;
        public RectTransform replayPanel;

        // public GameObject replayUI;
        public RectTransform uiRobotMarker;
        public RectTransform canvasRectTransform;
        
        //Cameras
        public GameObject captureCamera;
        public GameObject liveCamera;
        public GameObject replayCamera;
        
        
        public RobotLevel level;
  
        private void Awake() {
            _instance = this;
        }
        
        private void Start() {
        }

        private void Update() {
        }

    }
}