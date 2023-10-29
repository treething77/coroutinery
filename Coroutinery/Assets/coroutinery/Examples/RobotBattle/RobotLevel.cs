using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace aeric.coroutinery_demos
{
    public class RobotLevel : MonoBehaviour
    {
        private static RobotLevel _instance;

        public static RobotLevel Instance { 
            get { 
                //do the expensive search if we don't have a cached instance for some reason  
                //this can happen due to recompilation for example (causes domain reload)  
                if (_instance == null) 
                    _instance = FindObjectOfType<RobotLevel>();
                return _instance; 
            } 
        }

        public GameObject targetRoot;
        public List<RobotTeam> _robotTeams;
        public Text _redTeamScoreTxt;
        public Text _blueTeamScoreTxt;

        //reference caching
        private List<MoveTarget> _targets;

        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            _targets = targetRoot.GetComponentsInChildren<MoveTarget>().ToList();
        }

        private void Update()
        {
            if (_redTeamScoreTxt != null)
                _redTeamScoreTxt.text = "Red: " + _targets.Count(x => x.CapturedTeamIndex == 1);
            if (_blueTeamScoreTxt != null)
                _blueTeamScoreTxt.text = "Blue: " + _targets.Count(x => x.CapturedTeamIndex == 2);
        }

        public int FindTarget(Robot robot)
        {
            MoveTarget newTarget = null;

            var availableTargets = new List<MoveTarget>();

            //Get this robots team
            var team = _robotTeams.FirstOrDefault(x => x == robot.Team);

            //Choose the closest available target
            //-one that isn't captured by our team
            //-one that isn't being targeted by a robot on the same team)

            for (var i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i];

                if (target.CapturedTeamIndex == team.teamIndex)
                    continue;
                //is this target being targeted by someone on our team already?
                if (team._robots.Any(x => x.moveTargetIndex == i))
                    continue;

                availableTargets.Add(target);
            }

            //If there are no available targets then choose one randomly
            if (availableTargets.Count == 0) availableTargets.Add(_targets[Random.Range(0, _targets.Count)]);

            //choose the closest available target
            var closestDist = float.MaxValue;
            foreach (var target in availableTargets)
            {
                var dist = Vector3.Distance(robot.transform.position, target.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    newTarget = target;
                }
            }

            return _targets.IndexOf(newTarget);
        }

        public Vector3 GetTargetPosition(int moveTargetIndex)
        {
            return _targets[moveTargetIndex].transform.position;
        }

        public void CaptureTarget(int moveTargetIndex, Robot robot)
        {
            if (moveTargetIndex < 0 || moveTargetIndex >= _targets.Count) return;
            _targets[moveTargetIndex].Capture(robot);
        }

        public Color GetTeamColor(int teamIndex)
        {
            foreach (var team in _robotTeams)
                if (team.teamIndex == teamIndex)
                    return team.teamColor;

            return Color.grey;
        }

        public void CaptureTargetsWithinRange(Vector3 pt, float captureRange, Robot robot)
        {
            for (var i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i];
                if (target.CapturedTeamIndex != robot.Team.teamIndex)
                    if ((target.transform.position - pt).magnitude < captureRange)
                        CaptureTarget(i, robot);
            }
        }
    }
}