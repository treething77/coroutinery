using UnityEngine;

namespace aeric.coroutinery
{
    /// <summary>
    /// GUI scope wrapper around GUILayout.BeginArea and GUILayout.EndArea.
    /// </summary>
    class AreaScope : GUI.Scope
    {
        public AreaScope(Rect rect)
        {
            GUILayout.BeginArea(rect);
        }

        public AreaScope(Rect rect, GUIContent guiContent)
        {
            GUILayout.BeginArea(rect, guiContent);
        }

        protected override void CloseScope()
        {
            GUILayout.EndArea();
        }
    }
}