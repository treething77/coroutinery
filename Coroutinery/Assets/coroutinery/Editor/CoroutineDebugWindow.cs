using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.UIElements;

namespace aeric.coroutinery
{
    public class CoroutineDebugWindow : EditorWindow
    {
        struct TagFilter
        {
            public string tag;
            public bool matchCase;
        }

        struct LayerFilter
        {
            public int layer;
        }

        struct ContextFilter
        {
        }

        [MenuItem("Window/Aeric/Coroutine Debugger")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.

            CoroutineDebugWindow wnd = GetWindow<CoroutineDebugWindow>();
            wnd.titleContent = new GUIContent("Coroutines");
            wnd.wantsLessLayoutEvents = false;
            wnd.wantsMouseMove = true;

        }

        enum FilterMode
        {
            FILTER_TAG,
            FILTER_LAYER,
            FILTER_CONTEXT
        }

        FilterMode filterMode = FilterMode.FILTER_TAG;

        CoroutineDebugInfo _debugInfo = null;

        TagFilter tagFilter = new TagFilter();
        LayerFilter layerFilter = new LayerFilter();    
        ContextFilter contextFilter = new ContextFilter();

        static Color _highlightColor = new Color(0.7f, 0.8f, 0.9f, 1f);

        Vector2 sp;
        int coroutineIndex;

        int tabs;

        void OnGUI()
        {
            //Create a 2 pane window using IMGUI

            //IMGUI
            EditorGUILayout.BeginHorizontal();

            GUILayout.BeginArea(new Rect(0, 0, 300, Screen.height));

            //left pane
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Filter Mode");

            string[] tabOptions = new string[] { "Tag", "Layer", "Context" };
            tabs = GUILayout.Toolbar(tabs, tabOptions);

            EditorGUILayout.BeginHorizontal();

            //            filterMode = FilterModeButton("Tag", filterMode, FilterMode.FILTER_TAG);
            //          filterMode = FilterModeButton("Layer", filterMode, FilterMode.FILTER_LAYER);
            //        filterMode = FilterModeButton("Context", filterMode, FilterMode.FILTER_CONTEXT);
            filterMode = (FilterMode)tabs;

            //TODO: set editor pref for mode

            EditorGUILayout.EndHorizontal();
            switch (filterMode)
            {
                case FilterMode.FILTER_TAG:
                    TagFilterOptions();
                    break;
                case FilterMode.FILTER_LAYER:
                    LayerFilterOptions();
                    break;
                case FilterMode.FILTER_CONTEXT:
                    ContextFilterOptions();
                    break;
            }


            sp = EditorGUILayout.BeginScrollView(sp);
            //Detect mouse clicks within the scrollview and determine which item in the list was clicked on
            if (Event.current.type == EventType.MouseDown)
            {
                //get the mouse position
                Vector2 mousePos = Event.current.mousePosition;

                //get the scrollview position
                Vector2 scrollPos = sp;

                //get the scrollview size
                Vector2 scrollSize = new Vector2(300, Screen.height);

                //get the mouse position relative to the scrollview
                Vector2 mousePosRelative = mousePos - scrollPos;

                //check if the mouse is within the scrollview
                if (mousePosRelative.x > 0 && mousePosRelative.x < scrollSize.x && mousePosRelative.y > 0 && mousePosRelative.y < scrollSize.y)
                {
                    //get the index of the item that was clicked on
                    coroutineIndex = (int)(mousePosRelative.y / 20);
                }
            }

            List<CoroutineHandle> coroutines = new List<CoroutineHandle>();
            if (EditorApplication.isPlaying)
            {
  
                switch (filterMode)
                {
                    case FilterMode.FILTER_TAG:
                        coroutines = CoroutineManager.Instance.GetCoroutinesByTag(tagFilter.tag);
                        break;
                    case FilterMode.FILTER_LAYER:
                        coroutines = CoroutineManager.Instance.GetCoroutinesByLayer(layerFilter.layer);
                        break;
                    case FilterMode.FILTER_CONTEXT:
                        //get the selected objects from the hierarchy
                        var selectedObjects = Selection.gameObjects;
                        foreach(var go in selectedObjects)
                        {
                            coroutines.AddRange( CoroutineManager.Instance.GetCoroutinesByContext(go));
                        }
                        break;
                }

                //get the editor text color


                var currentStyle = new GUIStyle(GUI.skin.textField);
                currentStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.3f, 0.4f, 0.5f));



                foreach (var coroutine in coroutines)
                {
                    //Generate a pretty name for each coroutine
                    name = CoroutineManager.Instance.GetCoroutinePrettyName(coroutine, _debugInfo);

                    //highlight the selected coroutine
                    if (coroutineIndex == coroutines.IndexOf(coroutine))
                    {
                        EditorGUILayout.LabelField(name, currentStyle);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(name, GUI.skin.textField);
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

            GUILayout.EndArea();


            //Create a Texture2D and fill it with black
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.black);
            tex.Apply();

            GUI.DrawTexture(new Rect(305, 0, 1, Screen.height), tex);
            GUILayout.BeginArea(new Rect(301, 0, 400, Screen.height), new GUIContent(tex));
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(304, 0, 1200, Screen.height), new GUIContent(tex));



            if (_debugInfo == null)
            {
                //Find the debug info object
                string[] debugAssets = AssetDatabase.FindAssets("t:CoroutineDebugInfo");
                string guid = debugAssets[0];
                Debug.LogError(guid);

                //get the asset path from its guid


                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                _debugInfo = AssetDatabase.LoadAssetAtPath<CoroutineDebugInfo>(assetPath);
            }

            if (coroutines.Count > coroutineIndex)
            {
                DrawCoroutineDetails(coroutines[coroutineIndex]);
            }
            GUILayout.EndArea();

            EditorGUILayout.EndHorizontal();

            //call ongui again ondddd a timer
          //  Repaint();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void DrawCoroutineDetails(CoroutineHandle coroutineHandle)
        {
            SourceInfo debugInfo = CoroutineManager.Instance.GetCoroutineDebugInfo(coroutineHandle, _debugInfo);

            IEnumerator c = CoroutineManager.Instance.GetCoroutineEnumerator(coroutineHandle);


            EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Source:");

                if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
                {
                    var rc = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    if (rc.Contains(Event.current.mousePosition))
                    {
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(debugInfo.url, debugInfo.lineNumber);
                    }
                }

                EditorGUILayout.TextArea(debugInfo.url + ":" + debugInfo.lineNumber);
            EditorGUILayout.EndVertical();


            string stackTrace = CoroutineManager.Instance.GetStackTrace(coroutineHandle);

            //split stackTrace into lines
            string[] lines = stackTrace.Split('\n');
            //get the first line that does not contain "aeric.coroutinery.CoroutineManager"
            int index = lines.Length-1;
            do
            {
                if (lines[index-1].Contains("aeric.coroutinery.CoroutineManager"))
                {
                    break;
                }
                index--;
            } while (index >= 1);

            //extract the source file and line number
            string sourceLine = lines[index];

            int fileStart = sourceLine.IndexOf("] in ") + 5;
            int fileEnd = sourceLine.LastIndexOf(":");

            string sourceFile = sourceLine.Substring(fileStart, fileEnd - fileStart);
            string sourceLineNumberStr = sourceLine.Substring(fileEnd + 1, sourceLine.Length - fileEnd - 1);
            int sourceLineNumber = int.Parse(sourceLineNumberStr);

            EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Origin:");
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
            {
                var rc = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                if (rc.Contains(Event.current.mousePosition))
                {
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(sourceFile, sourceLineNumber);
                }
            }
            EditorGUILayout.TextArea(sourceFile + ":" + sourceLineNumber);
            EditorGUILayout.EndVertical();

            EditorGUILayout.LabelField("Status");

            //Load the Textures for the icons and then make a toolbar
            Texture2D playIcon = MakeTex(16, 16, new Color(1.0f, 0.3f, 0.4f, 0.5f));
            Texture2D stopicon = MakeTex(16, 16, new Color(0.0f, 0.3f, 0.4f, 0.5f));
            Texture2D pauseIcon = MakeTex(16, 16, new Color(1.0f, 1.0f, 0.4f, 0.5f));
            Texture2D resumeIcon = MakeTex(16, 16, new Color(0.0f, 0.3f, 1.0f, 0.5f));

            Texture2D[] tabIcns = new Texture2D[] { playIcon, stopicon, pauseIcon, resumeIcon };
            GUILayout.Toolbar(tabs, tabIcns);

            
            //what is the coroutine waiting on?
            if (c.Current != null)
            {
                EditorGUILayout.LabelField("Waiting on: " + c.Current.ToString());
                if (c.Current is IEnumerator)
                {
                    //get the pretty name for the coroutine we are waiting on
                    IEnumerator current = c.Current as IEnumerator;
                    var currentHandle = CoroutineManager.Instance.GetCoroutineHandle(c.Current as IEnumerator);
                    string prettyName = CoroutineManager.Instance.GetCoroutinePrettyName(currentHandle, _debugInfo);

                    //this is a coroutine so add a button to jump to it
                    if (GUILayout.Button(prettyName))
                    {
                        
                        //might not be in our current filter view
                                                                        
                    }
                }
                else if (c.Current is WaitForSeconds)
                {
                    WaitForSeconds wait = c.Current as WaitForSeconds;
                    
                    //need to ask the coroutine manager for the time remaining
                    float remaining = CoroutineManager.Instance.GetWaitTimeRemaining(coroutineHandle);
                    EditorGUILayout.LabelField("Time remaining: " + remaining);
                }
                else if (c.Current is WaitForSecondsRealtime)
                {

                }
                else if (c.Current is WaitForFrames)
                {

                }
            }
            
        }

        private void ContextFilterOptions()
        {
            
        }

        private void LayerFilterOptions()
        {
            layerFilter.layer = EditorGUILayout.LayerField("Layer", layerFilter.layer);

        }

        private void TagFilterOptions()
        {
            tagFilter.matchCase = EditorGUILayout.Toggle( "Match case", tagFilter.matchCase);
            tagFilter.tag = EditorGUILayout.TextField("Tag", tagFilter.tag);
        }

        private FilterMode FilterModeButton(string label, FilterMode currentFilterMode, FilterMode targetFilterMode)
        {
            //Change the button color depending on the filter mode
            GUI.backgroundColor = currentFilterMode == targetFilterMode ? Color.white : _highlightColor;

            if (GUILayout.Button(label, GUILayout.Width(100), GUILayout.Height(20)))
            {
                currentFilterMode = targetFilterMode;
            }
            return currentFilterMode;
        }
    }
}