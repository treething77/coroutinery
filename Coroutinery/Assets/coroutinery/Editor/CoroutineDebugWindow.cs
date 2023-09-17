using MEC;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace aeric.coroutinery
{
    //TODO: move to its own file
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

      //  struct ContextFilter
      //  {
      //  }

        private const string IconPath_Base     = "Assets/coroutinery/Editor/Resources/";
        private const string IconPath_StackPtr = IconPath_Base + "aeric_stack_ptr.png";
        private const string IconPath_Selected = IconPath_Base + "aeric_selected.png";
        private const string IconPath_Help     = IconPath_Base + "aeric_help.png";
        private const string IconPath_Play     = IconPath_Base + "aeric_play.png";
        private const string IconPath_Stop     = IconPath_Base + "aeric_stop.png";
        private const string IconPath_Pause    = IconPath_Base + "aeric_pause.png";
        private const string HelpUrl = "http://aeric.games/rwnd/api/html/index.html";
        private const string WindowTitle = "Coroutine Debugger";
        private static Color _highlightColor = new Color(0.7f, 0.8f, 0.9f, 1f);

        const float leftPaneWidth = 300;
        const float separatorWidth = 4;
        const int indent = 20;

        [MenuItem("Window/Aeric/Coroutine Debugger")]
        public static void ShowWindow()
        {
            CoroutineDebugWindow wnd = GetWindow<CoroutineDebugWindow>();

            wnd.titleContent = new GUIContent(WindowTitle, LoadCachedTexture(IconPath_StackPtr));
            wnd.wantsLessLayoutEvents = false;
            wnd.wantsMouseMove = true;
        }

        private static Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

        private static Texture2D LoadCachedTexture(string texturePath)
        {
            if (_textureCache.ContainsKey(texturePath))
            {
                return _textureCache[texturePath];
            }

            Texture2D texture = EditorResources.Load<Texture2D>(texturePath);
            if (texture != null)
            {
                _textureCache.Add(texturePath, texture);
            }
            return texture;
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


        Vector2 coroutineListScrollPosition;
        Vector2 debugInfoScrollPosition;
        int coroutineIndex;


        bool breakOnFinished = false;
        bool logSteps = false;

        private List<CoroutineHandle> selectedCoroutines = new List<CoroutineHandle>();

        void OnGUI()
        {
            //get the EditorWindow dimensions
            float windowWidth = this.position.width;
            float windowHeight = this.position.height;

            DrawLeftPane(windowHeight);

            DrawDividor(new Rect(leftPaneWidth + 1, 0, 2, windowHeight));

            DrawRightPane(windowWidth, windowHeight);

            DrawHelpButton(windowWidth);
        }

        private void DrawHelpButton(float windowWidth)
        { 
            Texture2D helpIcon = LoadCachedTexture(IconPath_Help);
            GUIStyle style2 = new GUIStyle(GUI.skin.button);
            style2.margin = new RectOffset(0, 0, 0, 0);
            style2.padding = new RectOffset(0, 0, 0, 0);

            if (GUI.Button(new Rect(windowWidth - 20, 0, 16, 16), helpIcon, style2))
            {
                //open url to the documentation
                Application.OpenURL(HelpUrl);
            }
            //call ongui again on a timer
            //  Repaint();
        }

        private void DrawRightPane(float windowWidth, float windowHeight)
        {

            float stackAreaHeight = 0.0f;
            List<CoroutineHandle> stackHandles = new List<CoroutineHandle>();
            if (selectedCoroutines.Count == 1)
            {
                //get the coroutine stack
                stackHandles = CoroutineManager.Instance.GetCoroutineStack(selectedCoroutines[0]);
            }

            //TODO: this isnt right, 30 was just a guess, and we aren't accounting for the label height
            stackAreaHeight = (20 * stackHandles.Count) + (separatorWidth + 4) + 2 + 24;

            float xStart = leftPaneWidth + separatorWidth;
            float yStart = windowHeight - stackAreaHeight;
            float debugInfoAreaWidth = windowWidth - xStart;

            float debugInfoAreaHeight = windowHeight - stackAreaHeight;

            float rightPaneWidth = windowWidth - leftPaneWidth - separatorWidth;

            using (new AreaScope(new Rect(leftPaneWidth + separatorWidth, 0, rightPaneWidth, debugInfoAreaHeight)))
            {
                var scrollViewScope = new EditorGUILayout.ScrollViewScope(debugInfoScrollPosition);
                using (scrollViewScope)
                {
                    debugInfoScrollPosition = scrollViewScope.scrollPosition;

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

                    if (selectedCoroutines.Count > 0)
                    {
                        DrawCoroutineDetails(selectedCoroutines, debugInfoAreaWidth);
                    }
                }//end scroll view scope
            }//end area scope

            //stack area

            if (selectedCoroutines.Count == 1)
            {
                var selectedCoroutine = selectedCoroutines[0];

                using (new AreaScope(new Rect(xStart, yStart, debugInfoAreaWidth, stackAreaHeight)))
                {
                    var dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(separatorWidth + 4));
                    dividorRect = new Rect(0, dividorRect.y + 2, debugInfoAreaWidth, 1);
                    DrawDividor(dividorRect);

                    Texture2D stackPtrIcon = LoadCachedTexture(IconPath_StackPtr);

                    EditorGUILayout.LabelField("Coroutine Stack");

                    using (new EditorGUILayout.VerticalScope())
                    {
                        int i = 0;
                        foreach (var handle in stackHandles)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (i > 1)
                                    GUILayout.Space(indent * (i - 1));
                                if (i > 0)
                                    GUILayout.Label(stackPtrIcon, GUILayout.Width(20), GUILayout.Height(20));

                                string prettyName = CoroutineManager.Instance.GetCoroutinePrettyName(handle, _debugInfo);

                                GUIStyle style = new GUIStyle(GUI.skin.button);
                                var t = style.normal.background;
                                Texture2D selectedBG = LoadCachedTexture(IconPath_Selected);

                                style.normal.background = handle._id == selectedCoroutine._id ? selectedBG : t;

                                if (GUILayout.Button(prettyName, style))
                                {
                                    SetSelectedCoroutine(handle);
                                }

                            }//end horizontal scope
                            i++;
                        }
                    }//end vertical scope
                }//end area scope
            }
        }

        private void DrawDividor(Rect rect)
        {
            //TODO: cache the textures
            Texture2D separatorTexture = MakeTex(1, 1, Color.black);
            GUI.DrawTexture(rect, separatorTexture);
        }

        private void DrawLeftPane(float windowHeight)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new AreaScope(new Rect(0, 0, leftPaneWidth, windowHeight)))
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        //TODO: string constants
                        EditorGUILayout.LabelField("Filter");

                        string[] tabOptions = new string[] { "Tag", "Layer", "Selection" };

                        filterMode = (FilterMode)GUILayout.Toolbar((int)filterMode, tabOptions);
                        //TODO: set editor pref for mode

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

                        //TODO: helper for horizontal dividors
                        //draw the dividor
                        Rect dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(separatorWidth + 4));
                        dividorRect = new Rect(0, dividorRect.y + 2, leftPaneWidth, 1);
                        DrawDividor(dividorRect);


                        List<CoroutineHandle> coroutines = new List<CoroutineHandle>();

                        var listScrollViewScope = new EditorGUILayout.ScrollViewScope(coroutineListScrollPosition);
                        using (listScrollViewScope)
                        {
                            coroutineListScrollPosition = listScrollViewScope.scrollPosition;

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
                                        //TODO: have to account for the scroll position

                                        //get the index of the item that was clicked on
                                        coroutineIndex = (int)(mousePosRelative.y / 20);
                                        if (coroutineIndex >= 0 && coroutineIndex < coroutines.Count)
                                        {
                                            //check if ctrl or shift are held down
                                            if (Event.current.control)
                                            {
                                                //add this coroutine to the selection
                                                if (selectedCoroutines.Contains(coroutines[coroutineIndex]))
                                                {
                                                    RemoveSelectedCoroutine(coroutines[coroutineIndex]);
                                                }
                                                else
                                                {
                                                    AddSelectedCoroutine(coroutines[coroutineIndex]);
                                                }
                                            }
                                            else if (Event.current.shift)
                                            {
                                                if (!selectedCoroutines.Contains(coroutines[coroutineIndex]))
                                                {
                                                    //calculate the index bounds of the selection set and extend the selection to include this coroutine
                                                    var lastSelected = selectedCoroutines[selectedCoroutines.Count - 1];
                                                    int lastIndex = coroutines.IndexOf(lastSelected);
                                                    if (lastIndex < coroutineIndex)
                                                    {
                                                        for (int j = lastIndex + 1; j <= coroutineIndex; j++)
                                                        {
                                                            if (!selectedCoroutines.Contains(coroutines[j]))
                                                            {
                                                                AddSelectedCoroutine(coroutines[j]);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        for (int j = coroutineIndex; j < lastIndex; j++)
                                                        {
                                                            if (!selectedCoroutines.Contains(coroutines[j]))
                                                            {
                                                                AddSelectedCoroutine(coroutines[j]);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                //no modifier - set this coroutine as the single selection
                                                SetSelectedCoroutine(coroutines[coroutineIndex]);
                                            }
                                        }
                                    }
                                }


                                //get the editor text color


                                var currentStyle = new GUIStyle(GUI.skin.textField);
                                currentStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.3f, 0.4f, 0.5f));//TODO: constant



                                foreach (var coroutine in coroutines)
                                {
                                    //Generate a pretty name for each coroutine
                                    name = CoroutineManager.Instance.GetCoroutinePrettyName(coroutine, _debugInfo);

                                    //highlight the selected coroutine
                                    if (selectedCoroutines.Contains(coroutine))
                                    {
                                        EditorGUILayout.LabelField(name, currentStyle);
                                    }
                                    else
                                    {
                                        EditorGUILayout.LabelField(name, GUI.skin.textField);
                                    }
                                }
                            }
                        }//end scrollview scope

                        dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(separatorWidth + 4));
                        dividorRect = new Rect(0, dividorRect.y + 2, leftPaneWidth, 1);
                        DrawDividor(dividorRect);

                        //TODO: string constants
                        breakOnFinished = EditorGUILayout.Toggle("Break on Done", breakOnFinished);//TODO: string
                        if (CoroutineManager.Instance != null)
                            CoroutineManager.Instance.BreakOnFinished = breakOnFinished;

                        logSteps = EditorGUILayout.Toggle("Log Steps", logSteps);//TODO: string
                        if (CoroutineManager.Instance != null)
                            CoroutineManager.Instance.LogSteps = logSteps;

                    }//end vertical scope
                }//end area scope
            }//end horizontal scope      
        }
        

        private void AddSelectedCoroutine(CoroutineHandle coroutineHandle)
        {
            selectedCoroutines.Add(coroutineHandle);
            Repaint();

            var context = CoroutineManager.Instance.GetCoroutineContext(coroutineHandle);
            if (context is GameObject)
            {
                //highlight the GameObject
             //   Selection.activeGameObject = (GameObject)context;
            }
        }

        private void RemoveSelectedCoroutine(CoroutineHandle coroutineHandle)
        {
            selectedCoroutines.Remove(coroutineHandle);
            Repaint();
        }

        private void SetSelectedCoroutine(CoroutineHandle coroutineHandle)
        {
            selectedCoroutines.Clear();
            AddSelectedCoroutine(coroutineHandle);
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

        private void DrawCoroutineDetails(List<CoroutineHandle> coroutineHandles, float debugInfoAreaWidth)
        {
            SourceInfo debugInfo = null;

            if (coroutineHandles.Count == 1)
            {
                debugInfo = CoroutineManager.Instance.GetCoroutineDebugInfo(coroutineHandles[0], _debugInfo);
            }

 
            using (new EditorGUILayout.VerticalScope())
            {
                string prettyName = string.Empty;
                
                if (selectedCoroutines.Count == 1)
                {
                    var coroutineHandle = selectedCoroutines[0];
                    prettyName = CoroutineManager.Instance.GetCoroutinePrettyName(coroutineHandle, _debugInfo);
                }
                else
                {
                    prettyName = "Multiple Coroutines Selected";
                }
                
                EditorGUILayout.LabelField(prettyName);

                //Status toolbar
                int buttonSize = 20;//TODO: constant
                //Load the Textures for the icons and then make a toolbar
                Texture2D playIcon = LoadCachedTexture(IconPath_Play);
                Texture2D stopicon = LoadCachedTexture(IconPath_Stop);
                Texture2D pauseIcon = LoadCachedTexture(IconPath_Pause);
                Texture2D selectedBG = LoadCachedTexture(IconPath_Selected);

                //Texture2D separatorTexture = MakeTex(1, 1, Color.black);

                GUIStyle style = new GUIStyle(GUI.skin.button);
                style.margin = new RectOffset(2, 2, 2, 2);
                style.padding = new RectOffset(1, 1, 1, 1);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var t = style.normal.background;

                    bool coroutineIsPaused = true;

                    foreach (var co in selectedCoroutines)
                    {
                        coroutineIsPaused &= CoroutineManager.Instance.GetCoroutinePaused(co);
                    }

                    style.normal.background = coroutineIsPaused ? t : selectedBG;

                    if (GUILayout.Button(playIcon, style, GUILayout.MaxWidth(buttonSize + 6)))
                    {
                        //if the coroutine is paused then resume it
                        if (coroutineIsPaused)
                        {
                            CoroutineManager.Instance.ResumeCoroutines(coroutineHandles);
                            Repaint();
                        }
                    }

                    style.normal.background = t;

                    if (GUILayout.Button(stopicon, style, GUILayout.MaxWidth(buttonSize + 6)))
                    {
                        //kill this coroutine
                        CoroutineManager.Instance.StopCoroutines(coroutineHandles);
                        Repaint();
                    }

                    style.normal.background = coroutineIsPaused ? selectedBG : t;

                    if (GUILayout.Button(pauseIcon, style, GUILayout.MaxWidth(buttonSize + 6)))
                    {
                        //if the coroutine is not paused then pause it
                        if (!coroutineIsPaused)
                        {
                            CoroutineManager.Instance.PauseCoroutines(coroutineHandles);
                            Repaint();
                        }
                    }
                }//end horizontal scope

                //end status toolbar

                if (coroutineHandles.Count == 1)
                {
                    var coroutineHandle = coroutineHandles[0];
                    IEnumerator c = CoroutineManager.Instance.GetCoroutineEnumerator(coroutineHandle);

                    Rect dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(2 + 4));
                    dividorRect = new Rect(0, dividorRect.y + 2, debugInfoAreaWidth, 1);
                    DrawDividor(dividorRect);

                    //what is the coroutine waiting on?
                    if (c.Current == null)
                    {
                        EditorGUILayout.LabelField("Waiting on: nothing");
                    }
                    else if ((c.Current is WaitForSeconds) || (c.Current is WaitForSecondsRealtime))
                    {
                        EditorGUILayout.LabelField("Waiting on: " + c.Current.ToString());
                        //need to ask the coroutine manager for the time remaining
                        float remaining = CoroutineManager.Instance.GetWaitTimeRemaining(coroutineHandle);
                        EditorGUILayout.LabelField("Time remaining: " + remaining);
                    }
                    else if (c.Current is WaitForFrames)
                    {
                        EditorGUILayout.LabelField("Waiting on: " + c.Current.ToString());
                        var wf = c.Current as WaitForFrames;
                        EditorGUILayout.LabelField("Frames remaining: " + wf.framesRemaining);
                    }
                    else if (c.Current is WaitWhile)
                    {
                        EditorGUILayout.LabelField("Waiting on: " + c.Current.ToString());
                    }
                    else if (c.Current is WaitUntil)
                    {
                        EditorGUILayout.LabelField("Waiting on: " + c.Current.ToString());
                    }
                    else if (c.Current is IEnumerator)
                    {
                        EditorGUILayout.LabelField("Waiting on:");
                        //get the pretty name for the coroutine we are waiting on
                        IEnumerator current = c.Current as IEnumerator;
                        var currentHandle = CoroutineManager.Instance.GetCoroutineHandle(c.Current as IEnumerator);
                        prettyName = CoroutineManager.Instance.GetCoroutinePrettyName(currentHandle, _debugInfo);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            //this is a coroutine so add a button to jump to it
                            if (GUILayout.Button(prettyName))
                            {
                                SetSelectedCoroutine(currentHandle);
                            }
                        }//end horizontal scope
                    }
                
                    //draw dividor
                    dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(2 + 4));
                    dividorRect = new Rect(0, dividorRect.y + 2, debugInfoAreaWidth, 1);
                    DrawDividor(dividorRect);


                    EditorGUILayout.LabelField("Current State:");

                    if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
                    {
                        var rc = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                        if (rc.Contains(Event.current.mousePosition))
                        {
                            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(debugInfo.url, debugInfo.lineNumber);
                        }
                    }

                    //extract the name between the < and >
                    string methodName = debugInfo.enumeratorTypeName;
                    int start = methodName.IndexOf('<');
                    int end = methodName.IndexOf('>');
                    if (start != -1 && end != -1)
                    {
                        methodName = methodName.Substring(start + 1, end - start - 1);
                    }

                    EditorGUILayout.LabelField(debugInfo.outerTypeName + "." + methodName + " (" + debugInfo.url + ":" + debugInfo.lineNumber + ")", EditorStyles.textField);
                }
            }//end vertical scope

            if (coroutineHandles.Count == 1)
            {
                var coroutineHandle = coroutineHandles[0];
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
                    if (lines[index - 1].Contains(".StartCoroutine"))
                    {
                        break;
                    }
                    index++;
                }

                //show the top 3 lines of the stack
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Started from:");

                    int i = 0;
                    Texture2D stackPtrIcon = LoadCachedTexture(IconPath_StackPtr);

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

                        methodName = className;
                        var start = methodName.IndexOf('<');
                        var end = methodName.IndexOf('>');
                        if (start != -1 && end != -1)
                        {
                            methodName = methodName.Substring(start + 1, end - start - 1);
                        }

                        start = className.IndexOf('+');
                        if (start != -1)
                        {
                            className = className.Substring(0, start);
                        }

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

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (i > 1)
                                GUILayout.Space(indent * (i - 1));

                            if (i > 0)
                                GUILayout.Label(stackPtrIcon, GUILayout.Width(20), GUILayout.Height(20));

                            EditorGUILayout.LabelField(callSite + " (" + sourceFile + ":" + sourceLineNumber + ")", EditorStyles.textField);
                        }//end horizontal scope

                        i++;

                        index++;
                        stackLinesShown++;
                    }
                }//end vertical scope 
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

            //TODO: constants
            if (GUILayout.Button(label, GUILayout.Width(100), GUILayout.Height(20)))
            {
                currentFilterMode = targetFilterMode;
            }
            return currentFilterMode;
        }
    }
}