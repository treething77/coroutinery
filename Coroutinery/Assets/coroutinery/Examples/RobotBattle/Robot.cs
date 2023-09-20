using UnityEngine;
using UnityEngine.Serialization;

namespace aeric.coroutinery_demos
{
    /// <summary>
    /// Handles the AI and player behavior/controls in the Rewind and Replay demos
    /// </summary>
    public class Robot : MonoBehaviour {
        private static readonly int _animIDMotionBlend = Animator.StringToHash("Speed");
        private static readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

        public RobotLevel _level;
        public bool playerControlled;
        public Camera playerCamera;
        
        public AudioClip footStepSFX;
        public AudioClip footStepSFXBackwards;

        public bool playReversedSoundInPlayback;
        
        // Rewind state
        public Vector3 moveStartPt;
        public Vector3 moveTargetPt;
        public float moveSpeedStart;
        public float moveSpeedEnd;
        public int moveTargetIndex;

        //component reference caching
        private Animator _animator;
        private CharacterController _controller;
        private Transform _transform;

        
        private Vector3 _motion;
        private bool _playbackActive;
        private float _playerSpeed;

        public RobotTeam Team { get; set; }

        private void Start() {
            _transform = transform;
            _animator = GetComponentInChildren<Animator>();
            _controller = GetComponent<CharacterController>();

            moveTargetIndex = -1;
        }

        public void Step(int i) {
            //Play footstep sounds for the player
            if (playerControlled) {
                float vol = 0.7f;
                if (i == 1) vol = 0.4f;
                if (i == 0) vol = 0.2f;

                //If we are in playback mode then play a reversed version of the sound
                var audioSrc = GetComponent<AudioSource>();
                if (_playbackActive && playReversedSoundInPlayback)
                    audioSrc.PlayOneShot(footStepSFXBackwards, vol);
                else
                    audioSrc.PlayOneShot(footStepSFX, vol);
            }
        }


        // Update is called once per frame
        private void Update() {
            if (playerControlled) {
                UpdatePlayerControls();
            }
            else {
                UpdateAIControls();
            }
        }

        private void UpdatePlayerControls() {
            var keyFwd = Input.GetKey(KeyCode.W);
            var keyLeft = Input.GetKey(KeyCode.A);
            var keyRight = Input.GetKey(KeyCode.D);

            if (keyFwd) {
                _playerSpeed += Time.deltaTime * 4.0f;
                _playerSpeed = Mathf.Min(_playerSpeed, 1.0f);
            }
            else {
                _playerSpeed -= Time.deltaTime * 2.0f;
            }

            _playerSpeed = Mathf.Clamp01(_playerSpeed);

            if (playerCamera != null) {
                float targetFOV = Mathf.Lerp(55.0f, 65.0f, _playerSpeed);
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * 2.0f);
            }

            if (keyLeft) _transform.Rotate(Vector3.up, -100.0f * Time.deltaTime);
            if (keyRight) _transform.Rotate(Vector3.up, 100.0f * Time.deltaTime);
            
            //motion blend is 0-6 and controls blend between idle,walk,run
            _animator.SetFloat(_animIDMotionBlend, _playerSpeed * 6.0f);
            
            //motion speed is an overall multiplier on the animation speed
            _animator.SetFloat(_animIDMotionSpeed, 1.0f);

            Vector3 movement = _transform.forward * (_movementCurve.Evaluate(_playerSpeed) * Time.deltaTime * 50.0f);
            Move(movement);
        }

        private void UpdateAIControls() {
            //pick a target to move towards
            if (moveTargetIndex == -1)
                ChooseTarget();

            var position = moveTargetPt;
            var lookAt = position;
            var position1 = _transform.position;
            lookAt.y = position1.y;

            var ogRot = _transform.rotation;
            _transform.LookAt(lookAt);
            _transform.rotation = Quaternion.Lerp(ogRot, _transform.rotation, 0.1f);

            var moveT = (position1 - moveStartPt).magnitude / (position - moveStartPt).magnitude;
            var moveSpeed = Mathf.Lerp(moveSpeedStart, moveSpeedEnd, moveT);
            
            //motion blend is 0-6 and controls blend between idle,walk,run
            _animator.SetFloat(_animIDMotionBlend, moveSpeed * 6.0f);
            
            //motion speed is an overall multiplier on the animation speed
            _animator.SetFloat(_animIDMotionSpeed, 1.0f);
            
            Vector3 movement = _transform.forward * (_movementCurve.Evaluate(moveSpeed) * Time.deltaTime * 50.0f);
            Move(movement);
        }

        public AnimationCurve _movementCurve;

        private void Move(Vector3 movement) {
            //Called from Unity root motion system
            var gravity = Vector3.up * 5.0f;
            var animMove = movement;
      
            //move forward
            var moveDirection = _transform.forward;
            moveDirection.y = 0.0f;

            var actualMove = moveDirection.normalized * animMove.magnitude;

            //Move the character controller to match the animation movement
            var moveAmount = actualMove - gravity * Time.deltaTime;
            _controller.Move(moveAmount);

            if (playerControlled) {
                _level.CaptureTargetsWithinRange(_transform.position, 1.5f, this);
            }
            else {
                if ((moveTargetPt - _transform.position).magnitude < 1.5f) {
                    _level.CaptureTarget(moveTargetIndex, this);

                    //choose new target
                    ChooseTarget();
                }
            }
        }

        private void ChooseTarget() {
            moveTargetIndex = _level.FindTarget(this);

            moveTargetPt = _level.GetTargetPosition(moveTargetIndex);

            moveStartPt = transform.position;
            moveSpeedStart = Random.Range(0.4f, 1.0f);
            moveSpeedEnd = Random.Range(0.4f, 1.0f);
        }

        public void UpdateTeamColor() {
            if (Team != null) {
                var renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var r in renderers) {
                    foreach(var m in r.materials)
                        m.color = Team.teamColor;
                }
            }
        }
    }
}