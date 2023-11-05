using aeric.coroutinery;
using UnityEditor;
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
            string baseFolderPath = AssetDatabase.GUIDToAssetPath(CoroutineManager.BaseFolderGUID);
            SceneManager.LoadScene(baseFolderPath + "/Examples/Emulation/Emulation.unity");
        }

        public void loadDeepStack()
        {
            string baseFolderPath = AssetDatabase.GUIDToAssetPath(CoroutineManager.BaseFolderGUID);
            SceneManager.LoadScene(baseFolderPath + "/Examples/DeepStack/DeepStack.unity");
        }

        public void loadLabelsContext()
        {
            string baseFolderPath = AssetDatabase.GUIDToAssetPath(CoroutineManager.BaseFolderGUID);
            SceneManager.LoadScene(baseFolderPath + "/Examples/Labels/Labels.unity");
        }

        public void loadPause()
        {
            string baseFolderPath = AssetDatabase.GUIDToAssetPath(CoroutineManager.BaseFolderGUID);
            SceneManager.LoadScene(baseFolderPath + "/Examples/Pause/Pause.unity");
        }

        public void loadRobots()
        {
            string baseFolderPath = AssetDatabase.GUIDToAssetPath(CoroutineManager.BaseFolderGUID);
            SceneManager.LoadScene(baseFolderPath + "/Examples/RobotBattle/RobotBattle.unity");
        }
    }
}