#if UNITY_EDITOR

using DTT.PublishingTools;
using UnityEditor;

namespace DTT.Utils.CoroutineManagement
{
    /// <summary>
    /// Provides a menu item to open the readme for this package.
    /// </summary>
    internal class ReadMeOpener
    {
        [MenuItem("Tools/DTT/CoroutineManggement/ReadMe")]
        private static void OpenReadMe() => DTTEditorConfig.OpenReadMe("dtt.coroutinemanagement");
        }
    }

#endif