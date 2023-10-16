using UnityEngine;
using UnityEngine.SceneManagement;

namespace aeric.coroutinery_demos
{
    /// <summary>
    /// Demo menu implementation for accessing various demo scenes.
    /// </summary>
    public class DemoMenu : MonoBehaviour
    {
        public void loadEmulation()
        {
            SceneManager.LoadScene("Assets/coroutinery/Examples/Emulation/Emulation.unity");
        }

        public void loadDeepStack()
        {
            SceneManager.LoadScene("Assets/coroutinery/Examples/DeepStack/DeepStack.unity");
        }

        public void loadLabelsContext()
        {
            SceneManager.LoadScene("Assets/coroutinery/Examples/LabelsContextTags/LabelsContextTags.unity");
        }

        public void loadPause()
        {
            SceneManager.LoadScene("Assets/coroutinery/Examples/Pause/Pause.unity");
        }

        public void loadRobots()
        {
            SceneManager.LoadScene("Assets/coroutinery/Examples/RobotBattle/RobotBattle.unity");
        }
    }
}