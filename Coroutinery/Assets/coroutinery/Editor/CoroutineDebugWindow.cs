using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental;
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

        Vector2 coroutineListScrollPosition;
        Vector2 debugInfoScrollPosition;
        int coroutineIndex;

        int tabs;

        bool breakOnFinished = false;
        private CoroutineHandle selectedCoroutine = new CoroutineHandle(0);

        void OnGUI()
        {
            Texture2D separatorTexture = MakeTex(1, 1, Color.black);

            //get the EditorWindow dimensions
            float windowWidth = this.position.width;
            float windowHeight = this.position.height;
            
            float leftPaneWidth = 300;
            float separatorWidth = 4;
            float rightPaneWidth = windowWidth - leftPaneWidth - separatorWidth;

            /////////////////////////////////////////////////////////////////////////////////////
            // LEFT PANE
            EditorGUILayout.BeginHorizontal();
                GUILayout.BeginArea(new Rect(0, 0, leftPaneWidth, windowHeight));
                    EditorGUILayout.BeginVertical();

                        EditorGUILayout.LabelField("Filter");

                        string[] tabOptions = new string[] { "Tag", "Layer", "Selection" };
                        tabs = GUILayout.Toolbar(tabs, tabOptions);
                        //TODO: set editor pref for mode
                        filterMode = (FilterMode)tabs;

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


                        //draw the dividor
                        var dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(separatorWidth+4));

                        GUI.DrawTexture(new Rect(0, dividorRect.y+2, leftPaneWidth, 1), separatorTexture);


                        List<CoroutineHandle> coroutines = new List<CoroutineHandle>();


                        coroutineListScrollPosition = EditorGUILayout.BeginScrollView(coroutineListScrollPosition);
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
                                        foreach (var go in selectedObjects)
                                        {
                                            coroutines.AddRange(CoroutineManager.Instance.GetCoroutinesByContext(go));
                                        }
                                        break;
                                }

                                //Detect mouse clicks within the scrollview and determine which item in the list was clicked on
                                if (Event.current.type == EventType.MouseDown)
                                {

                                    //get the mouse position
                                    Vector2 mousePos = Event.current.mousePosition;

                                    //get the scrollview position
                                    Vector2 scrollPos = coroutineListScrollPosition;

                                    //get the scrollview size
                                    Vector2 scrollSize = new Vector2(leftPaneWidth, windowHeight);

                                    //get the mouse position relative to the scrollview
                                    Vector2 mousePosRelative = mousePos - scrollPos;

                                    //check if the mouse is within the scrollview
                                    if (mousePosRelative.x > 0 && mousePosRelative.x < scrollSize.x && mousePosRelative.y > 0 && mousePosRelative.y < scrollSize.y)
                                    {
                                        //get the index of the item that was clicked on
                                        coroutineIndex = (int)(mousePosRelative.y / 20);
                                        if (coroutineIndex >= 0 && coroutineIndex < coroutines.Count)
                                        {
                                            selectedCoroutine = coroutines[coroutineIndex];
                                            Repaint();
                                        }
                                    }
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

                        dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(separatorWidth + 4));

                        GUI.DrawTexture(new Rect(0, dividorRect.y + 2, leftPaneWidth, 1), separatorTexture);

                        //Settings
                     //   EditorGUILayout.BeginHorizontal();

                            breakOnFinished = EditorGUILayout.Toggle("Break on Done", breakOnFinished);
                            if (CoroutineManager.Instance != null)
                                CoroutineManager.Instance.BreakOnFinished = breakOnFinished;

                            breakOnFinished = EditorGUILayout.Toggle("Log Steps", breakOnFinished);

                       // EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                GUILayout.EndArea();


                //Create a Texture2D and fill it with black
                //TODO: cache this
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.black);
                tex.Apply();

                //draw the dividor
                GUI.DrawTexture(new Rect(leftPaneWidth + 1, 0, 2, windowHeight), tex);

                float stackAreaHeight = 0.0f;
                List<CoroutineHandle> stackHandles = new List<CoroutineHandle>();
                if (selectedCoroutine._id != 0)
                {
                    //get the coroutine stack
                    stackHandles = CoroutineManager.Instance.GetCoroutineStack(selectedCoroutine);
                }
                stackAreaHeight = (30 * stackHandles.Count) + (separatorWidth + 4) + 2;

            float xStart = leftPaneWidth + separatorWidth;
            float yStart = windowHeight - stackAreaHeight;
            float debugInfoAreaWidth = windowWidth - xStart;

            float debugInfoAreaHeight = windowHeight - stackAreaHeight;


                GUILayout.BeginArea(new Rect(leftPaneWidth + separatorWidth, 0, rightPaneWidth, debugInfoAreaHeight), new GUIContent(tex));
       
                    debugInfoScrollPosition = EditorGUILayout.BeginScrollView(debugInfoScrollPosition);

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

                    if (selectedCoroutine._id != 0)
                    {
                        DrawCoroutineDetails(selectedCoroutine, debugInfoAreaWidth);
                    }

                    EditorGUILayout.EndScrollView();
                GUILayout.EndArea();
            EditorGUILayout.EndHorizontal();


            //stack area

            if (selectedCoroutine._id != 0)
            {


                GUILayout.BeginArea(new Rect(xStart, yStart, debugInfoAreaWidth, stackAreaHeight), new GUIContent(tex));
                    dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(separatorWidth + 4));

                    GUI.DrawTexture(new Rect(0, dividorRect.y + 2, debugInfoAreaWidth, 1), separatorTexture);
                    Texture2D stackPtrIcon = EditorResources.Load<Texture2D>("Assets/coroutinery/Editor/Resources/stack_ptr.png");

                    EditorGUILayout.LabelField("Coroutine Stack");
                    EditorGUILayout.BeginVertical();
                        int indent = 20;
                        int i = 0;
                        foreach (var handle in stackHandles)
                        {
                            EditorGUILayout.BeginHorizontal();  
                            if (i > 1)
                                GUILayout.Space(indent * (i-1));

                            if (i>0)
                                GUILayout.Label(stackPtrIcon, GUILayout.Width(20), GUILayout.Height(20));

                                string prettyName = CoroutineManager.Instance.GetCoroutinePrettyName(handle, _debugInfo);   
                                if (GUILayout.Button(prettyName))
                                {
                                    selectedCoroutine = handle;
                                    Repaint();
                                }
             
                            EditorGUILayout.EndHorizontal();

                            i++;
                        }
                    EditorGUILayout.EndVertical();

                GUILayout.EndArea();
            }

            //call ongui again on a timer
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

        private void DrawCoroutineDetails(CoroutineHandle coroutineHandle, float debugInfoAreaWidth)
        {
            SourceInfo debugInfo = CoroutineManager.Instance.GetCoroutineDebugInfo(coroutineHandle, _debugInfo);

            IEnumerator c = CoroutineManager.Instance.GetCoroutineEnumerator(coroutineHandle);


            EditorGUILayout.BeginVertical();

            string prettyName = CoroutineManager.Instance.GetCoroutinePrettyName(coroutineHandle, _debugInfo);
            EditorGUILayout.LabelField(prettyName);

            //Status toolbar
            int buttonSize = 20;
            //Load the Textures for the icons and then make a toolbar
            Texture2D playIcon = Resources.Load<Texture2D>("play");
            Texture2D stopicon = EditorResources.Load<Texture2D>("Assets/coroutinery/Editor/Resources/stop.png");
            Texture2D pauseIcon = EditorResources.Load<Texture2D>("Assets/coroutinery/Editor/Resources/pause.png");
            Texture2D selectedBG = EditorResources.Load<Texture2D>("Assets/coroutinery/Editor/Resources/selected.png");
            Texture2D separatorTexture = MakeTex(1, 1, Color.black);

            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.margin = new RectOffset(2, 2, 2, 2);
            style.padding = new RectOffset(1, 1, 1, 1);

            EditorGUILayout.BeginHorizontal();

            var t = style.normal.background;

            bool coroutineIsPaused = CoroutineManager.Instance.GetCoroutinePaused(coroutineHandle);

            style.normal.background = coroutineIsPaused ? t : selectedBG;

            if (GUILayout.Button(playIcon, style, GUILayout.MaxWidth(buttonSize + 6)))
            {
                //if the coroutine is paused then resume it
                if (coroutineIsPaused)
                {
                    CoroutineManager.Instance.ResumeCoroutine(coroutineHandle);
                    Repaint();
                }
            }

            style.normal.background = t;

            if (GUILayout.Button(stopicon, style, GUILayout.MaxWidth(buttonSize + 6)))
            {
                //kill this coroutine
                CoroutineManager.Instance.StopCoroutine(coroutineHandle);
                Repaint();
            }

            style.normal.background = coroutineIsPaused ? selectedBG : t;

            if (GUILayout.Button(pauseIcon, style, GUILayout.MaxWidth(buttonSize + 6)))
            {
                //if the coroutine is not paused then pause it
                if (!coroutineIsPaused)
                {
                    CoroutineManager.Instance.PauseCoroutine(coroutineHandle);
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
            //end status toolbar

            var dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(2 + 4));
            GUI.DrawTexture(new Rect(0, dividorRect.y + 2, debugInfoAreaWidth, 1), separatorTexture);

            //what is the coroutine waiting on?
            if (c.Current != null)
            {
                EditorGUILayout.LabelField("Waiting on: " + c.Current.ToString());
                if (c.Current is WaitForSeconds)
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
                else if (c.Current is WaitWhile)
                {
                    //can we print out something related to the action?
                    WaitWhile wait = c.Current as WaitWhile;
                }
                else if (c.Current is WaitUntil)
                {

                }
                else if (c.Current is IEnumerator)
                {
                    //get the pretty name for the coroutine we are waiting on
                    IEnumerator current = c.Current as IEnumerator;
                    var currentHandle = CoroutineManager.Instance.GetCoroutineHandle(c.Current as IEnumerator);
                    prettyName = CoroutineManager.Instance.GetCoroutinePrettyName(currentHandle, _debugInfo);

                    EditorGUILayout.BeginHorizontal();
                    //this is a coroutine so add a button to jump to it
                    if (GUILayout.Button(prettyName))
                    {
                        selectedCoroutine = currentHandle;
                        Repaint();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }


            //draw dividor
            dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(2 + 4));
            GUI.DrawTexture(new Rect(0, dividorRect.y + 2, debugInfoAreaWidth, 1), separatorTexture);


            EditorGUILayout.LabelField("Current State:");

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
            string[] lines = stackTrace.Split('\n');

            //Find the first line in the stack that is not in aeric library code
                        

            //" at aeric."

            //split stackTrace into lines
            //get the first line that does not contain "aeric.coroutinery.CoroutineManager"
            int index = 1;

            //Find the line containing the StartCoroutine call
            while (index < lines.Length)
            {
                if (lines[index-1].Contains(".StartCoroutine"))
                {
                    break;
                }
                index++;
            }

            //show the top 3 lines of the stack
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Started from:");

            int indent = 20;
            int i = 0;
            Texture2D stackPtrIcon = EditorResources.Load<Texture2D>("Assets/coroutinery/Editor/Resources/stack_ptr.png");

            int stackLinesToShow = 3;
            int stackLinesShown = 0;
            while (stackLinesShown < stackLinesToShow && index < lines.Length)
            {
                //extract the source file and line number
                string sourceLine = lines[index];

                //every line starts with " at "
                sourceLine = sourceLine.Substring(5);
                //the next space will be the end of the method name
                int methodEnd = sourceLine.IndexOf(" ");
                //extract the method name
                string methodName = sourceLine.Substring(0, methodEnd);

                //split by '.'
                string[] methodParts = methodName.Split('.');
                //the last part will be the method name
                methodName = methodParts[methodParts.Length - 1];

                string className = methodParts[methodParts.Length - 2];

                string callSite = className + "." + methodName;

                int fileStart = sourceLine.IndexOf("] in ") + 5;
                int fileEnd = sourceLine.LastIndexOf(":");

                string sourceFile = sourceLine.Substring(fileStart, fileEnd - fileStart);

                sourceFile = sourceFile.Substring(sourceFile.IndexOf("Assets"));

                string sourceLineNumberStr = sourceLine.Substring(fileEnd + 1, sourceLine.Length - fileEnd - 1);
                int sourceLineNumber = int.Parse(sourceLineNumberStr);

                if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
                {
                    var rc = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    if (rc.Contains(Event.current.mousePosition))
                    {
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(sourceFile, sourceLineNumber);
                    }
                }

                EditorGUILayout.BeginHorizontal();
                if (i > 1)
                    GUILayout.Space(indent * (i - 1));

                if (i > 0)
                    GUILayout.Label(stackPtrIcon, GUILayout.Width(20), GUILayout.Height(20));

                EditorGUILayout.TextArea(callSite + " (" + sourceFile + ":" + sourceLineNumber + ")");
                EditorGUILayout.EndHorizontal();

                i++;

                index++;
                stackLinesShown++;
            }
  
            EditorGUILayout.EndVertical();
  
            
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