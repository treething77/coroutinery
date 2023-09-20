using System.Collections.Generic;
using UnityEngine;

namespace aeric.coroutinery_demos
{
    //used in the Rewind and Replay demos
    public class RobotTeam : MonoBehaviour {
        public List<Robot> _robots;

        public Color teamColor;
        public int teamIndex;

        private void Start() {
            foreach (var robot in _robots) {
                robot.Team = this;
                robot.UpdateTeamColor();
            }
        }
    }
}