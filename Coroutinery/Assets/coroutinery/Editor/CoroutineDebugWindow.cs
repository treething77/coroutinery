using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace aeric.coroutinery
{
    /// <summary>
    /// GUI for managing coroutines.
    /// </summary>
    public class CoroutineDebugWindow : EditorWindow
    {
        private const string IconPath_Base = "/Editor/Resources/";
        private const string IconPath_Logo = IconPath_Base + "logo_small_transparent.png";
        private const string IconPath_StackPtr = IconPath_Base + "aeric_stack_ptr.png";
        private const string IconPath_Selected = IconPath_Base + "aeric_selected.png";
        private const string IconPath_Help = IconPath_Base + "aeric_help.png";
        private const string IconPath_Play = IconPath_Base + "aeric_play.png";
        private const string IconPath_Stop = IconPath_Base + "aeric_stop.png";
        private const string IconPath_Step = IconPath_Base + "aeric_step.png";
        private const string IconPath_Pause = IconPath_Base + "aeric_pause.png";
        private const string IconPath_Reset = IconPath_Base + "aeric_reset.png";
        private const string HelpUrl = "http://aeric.games/coroutinedebugger/api/html/index.html";
        private const string WindowTitle = "Coroutine Debugger";

        private static Color _highlightColor = new Color(0.7f, 0.8f, 0.9f, 1f);

        const float leftPaneWidth = 300;
        const float separatorWidth = 4;
        const int indent = 20;

        [MenuItem("Window/Aeric/Coroutine Debugger")]
        public static void ShowWindow()
        {
            CoroutineDebugWindow wnd = GetWindow<CoroutineDebugWindow>();

            wnd.titleContent = new GUIContent(WindowTitle, LoadCachedTexture(IconPath_Logo));
            wnd.wantsLessLayoutEvents = false;
            wnd.wantsMouseMove = true;
            wnd.autoRepaintOnSceneChange = true;

            wnd.lastRepaint = DateTime.Now;
        }

        private static Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();
        private Texture2D separatorTexture;

        private static Texture2D LoadCachedTexture(string texturePath)
        {
            string baseFolderPath = AssetDatabase.GUIDToAssetPath(CoroutineManager.BaseFolderGUID);
            string fullTexturePath = baseFolderPath + "/" + texturePath;

            if (_textureCache.ContainsKey(fullTexturePath))
            {
                return _textureCache[fullTexturePath];
            }

            Texture2D texture = EditorResources.Load<Texture2D>(fullTexturePath);
            if (texture != null)
            {
                _textureCache.Add(fullTexturePath, texture);
            }
            return texture;
        }


        enum FilterMode
        {
            FILTER_NAME,
            FILTER_TAG,
        }

        FilterMode filterMode = FilterMode.FILTER_NAME;
        static bool filterOnSelection = false;
        static string searchContents = string.Empty;
        static List<CoroutineHandle> coroutines = new List<CoroutineHandle>();

        bool clearedSearch = false;

        CoroutineDebugInfo _debugInfo = null;

        Vector2 coroutineListScrollPosition;
        Vector2 debugInfoScrollPosition;
        int coroutineIndex;

        bool breakOnFinished = false;
        bool logSteps = false;
        bool selectedCoroutineChanged = false;

        private List<CoroutineHandle> selectedCoroutines = new List<CoroutineHandle>();

        void OnGUI()
        {
            if (coroutines == null)
                coroutines = new List<CoroutineHandle>();

            //get the EditorWindow dimensions
            float windowWidth = this.position.width;
            float windowHeight = this.position.height;

            if (_debugInfo == null)
            {
                _debugInfo = CoroutineManager.LoadDebugInfo();
            }

            DrawLeftPane(windowHeight);

            DrawDividor(new Rect(leftPaneWidth + 1, 0, 2, windowHeight));

            DrawRightPane(windowWidth, windowHeight);

            DrawHelpButton(windowWidth);
        }

        DateTime lastRepaint;
        GameObject[] lastSelectedObjectss;

        void Update()
        {
            //check for conditions that should cause a repaint
            if (EditorApplication.isPlaying && (this.hasFocus || (DateTime.Now - lastRepaint).TotalMilliseconds > 1000))
            {
                Repaint();
                lastRepaint = DateTime.Now;
            }

            //if the selection changed
            //compare the arrays contents
            if (lastSelectedObjectss == null)
            {
                lastSelectedObjectss = Selection.gameObjects;
                Repaint();
            }

            if (lastSelectedObjectss.Length != Selection.gameObjects.Length)
            {
                lastSelectedObjectss = Selection.gameObjects;
                Repaint();
            }
            else
            {
                var result = lastSelectedObjectss.Except(Selection.gameObjects);
                if (result.Count() > 0)
                {
                    lastSelectedObjectss = Selection.gameObjects;
                    Repaint();
                }
            }

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
        }

        private Vector2 coroutineStackScrollPosition;

        private Dictionary<string, GUIStyle> cachedGUIStyles = new Dictionary<string, GUIStyle>();

        //If this is set then the selection will not be changed until the user changes the search options
        private static bool customSelection;

        internal GUIStyle GetGUIStyle(string styleName)
        {
            if (cachedGUIStyles.ContainsKey(styleName))
            {
                return cachedGUIStyles[styleName];
            }

            GUIStyle gUIStyle = GUI.skin.FindStyle(styleName) ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (gUIStyle == null)
            {
                Debug.LogError("Missing built-in guistyle " + styleName);
            }
            else
            {
                cachedGUIStyles.Add(styleName, gUIStyle);
            }

            return gUIStyle;
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

            int stackCountToShow = Math.Min(stackHandles.Count, 10);

            float stackHeight = 20 * stackCountToShow;
            stackAreaHeight = stackHeight + (separatorWidth + 4) + 2 + 24;

            //dont take more than half the area for the coroutine stack
            stackAreaHeight = Math.Min(stackAreaHeight, windowHeight * 0.5f);
            stackHeight = stackAreaHeight - (separatorWidth + 4) - 2 - 24;


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

                    if (selectedCoroutines.Count > 0)
                    {
                        DrawCoroutineDetails(selectedCoroutines, debugInfoAreaWidth);
                    }
                }//end scroll view scope
            }//end area scope

            //stack area

            if (selectedCoroutines.Count == 1)
            {
                using (new AreaScope(new Rect(xStart, yStart, debugInfoAreaWidth, stackAreaHeight)))
                {
                    var dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(separatorWidth + 4));
                    dividorRect = new Rect(0, dividorRect.y + 2, debugInfoAreaWidth, 1);
                    DrawDividor(dividorRect);

                    Texture2D stackPtrIcon = LoadCachedTexture(IconPath_StackPtr);

                    EditorGUILayout.LabelField("Coroutine Stack");

                    var listScrollViewScope = new EditorGUILayout.ScrollViewScope(coroutineStackScrollPosition, GUILayout.Height(stackHeight + 6), GUILayout.ExpandHeight(false));
                    using (listScrollViewScope)
                    {
                        coroutineStackScrollPosition = listScrollViewScope.scrollPosition;

                        var selectedCoroutine = selectedCoroutines[0];

                        //if the selection changed we want to find the index of the selected coroutine in the stack
                        if (selectedCoroutineChanged)
                        {
                            coroutineIndex = stackHandles.IndexOf(selectedCoroutine);

                            //then set the scroll position to show the selected coroutine
                            coroutineStackScrollPosition.y = coroutineIndex * 20;
                            selectedCoroutineChanged = false;
                        }

                        using (new EditorGUILayout.VerticalScope())
                        {
                            int i = 0;
                            foreach (var handle in stackHandles)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    bool isSelectedCoroutine = (handle._id == selectedCoroutine._id);
                                    if (isSelectedCoroutine)
                                        GUILayout.Label(stackPtrIcon, GUILayout.Width(20), GUILayout.Height(20));

                                    string prettyName = CoroutineManager.Instance.GetCoroutinePrettyName(handle, _debugInfo);

                                    GUIStyle style = new GUIStyle(GUI.skin.button);
                                    var t = style.normal.background;
                                    Texture2D selectedBG = LoadCachedTexture(IconPath_Selected);

                                    style.normal.background = isSelectedCoroutine ? selectedBG : t;

                                    if (GUILayout.Button(prettyName, style))
                                    {
                                        SetSelectedCoroutine(handle);
                                    }

                                }//end horizontal scope
                                i++;
                            }
                        }//end vertical scope
                    }
                }//end area scope
            }
        }

        private void DrawDividor(Rect rect)
        {
            if (separatorTexture == null)
            {
                separatorTexture = MakeTex(1, 1, Color.black);
            }
            GUI.DrawTexture(rect, separatorTexture);
        }

        private void DrawLeftPane(float windowHeight)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new AreaScope(new Rect(0, 0, leftPaneWidth, windowHeight)))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Search:", EditorStyles.label, GUILayout.MaxWidth(leftPaneWidth * 0.15f));

                        string[] tabOptions = new string[] { "Name", "Tag" };
                        string[] tabTooltips = new string[] { "Search by name", "Search by tag" };
                        GUIContent[] arraytabContent = new GUIContent[tabOptions.Length];
                        for (int i = 0; i < tabOptions.Length; i++)
                        {
                            arraytabContent[i] = new GUIContent(tabOptions[i], tabTooltips[i]);
                        }

                        FilterMode newFilterMode = (FilterMode)GUILayout.Toolbar((int)filterMode, arraytabContent, GUILayout.MaxWidth(leftPaneWidth * 0.3f));

                        GUILayout.FlexibleSpace();
                        var newFilterOnSelection = EditorGUILayout.ToggleLeft(new GUIContent("Selection", "Limit the search to coroutines that have a GameObject context that is included in the scene selection"), filterOnSelection, GUILayout.MaxWidth(leftPaneWidth * 0.3f));

                        if (filterMode != newFilterMode || filterOnSelection != newFilterOnSelection)
                        {
                            filterMode = newFilterMode;
                            filterOnSelection = newFilterOnSelection;
                            customSelection = false;
                            searchContents = string.Empty;
                        }
                    }

                    using (new EditorGUILayout.VerticalScope())
                    {
                        //This is a lot of work to get a search field with a cancel button
                        //This is because Unity keeps certain minor elements as internal, so we have to recreate or workaround them
                        GUIStyle cancelButtonStyle = GetGUIStyle("SearchCancelButton");
                        GUIStyle emptyCancelButtonStyle = GetGUIStyle("SearchCancelButtonEmpty");
                        float fixedWidth = cancelButtonStyle.fixedWidth;

                        float kLabelFloatMaxW = EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + 5f;
                        Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, kLabelFloatMaxW, 18, 18, EditorStyles.toolbarSearchField);
                        rect.width -= fixedWidth;

                        bool empty = string.IsNullOrEmpty(searchContents);

                        Rect position = rect;
                        position.x += rect.width;
                        position.width = fixedWidth;

                        GUI.SetNextControlName("coroutinerysearchfield");

                        string newSearchContents = EditorGUI.TextField(rect, searchContents, EditorStyles.toolbarSearchField);

                        GUI.SetNextControlName("searchcancelbutton");
                        if (GUI.Button(position, GUIContent.none, !empty ? cancelButtonStyle : emptyCancelButtonStyle))
                        {
                            searchContents = string.Empty;
                            //If we don't change focus then the text field doesn't actually clear
                            GUI.FocusControl("searchcancelbutton");

                            Repaint();
                            clearedSearch = true;
                        }
                        else if (clearedSearch)
                        {
                            clearedSearch = false;
                            EditorGUI.FocusTextInControl("coroutinerysearchfield");
                        }

                        if (clearedSearch)
                        {
                            searchContents = string.Empty;
                            customSelection = false;
                        }
                        else if (newSearchContents != searchContents)
                        {
                            if (customSelection) searchContents = string.Empty;
                            else                 searchContents = newSearchContents;
                            customSelection = false;
                        }

                        //draw the dividor
                        Rect dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(separatorWidth + 4));
                        dividorRect = new Rect(0, dividorRect.y + 2, leftPaneWidth, 1);
                        DrawDividor(dividorRect);
                      
                        var listScrollViewScope = new EditorGUILayout.ScrollViewScope(coroutineListScrollPosition);
                        using (listScrollViewScope)
                        {
                            coroutineListScrollPosition = listScrollViewScope.scrollPosition;

                            if (EditorApplication.isPlaying)
                            {
                                if (!customSelection)
                                {
                                    switch (filterMode)
                                    {
                                        case FilterMode.FILTER_TAG:
                                            coroutines = CoroutineManager.Instance.GetCoroutinesByTag(searchContents);
                                            break;
                                        case FilterMode.FILTER_NAME:
                                            coroutines = CoroutineManager.Instance.GetCoroutinesByName(searchContents, _debugInfo);
                                            break;
                                    }

                                    if (filterOnSelection)
                                    {
                                        List<CoroutineHandle> selectionCoroutines = new List<CoroutineHandle>();

                                        //get the selected objects from the hierarchy
                                        var selectedObjects = Selection.gameObjects;
                                        foreach (var go in selectedObjects)
                                        {
                                            selectionCoroutines.AddRange(CoroutineManager.Instance.GetCoroutinesByContext(go));
                                        }

                                        //filter the coroutines list by the selectionCoroutines list
                                        coroutines = coroutines.FindAll((CoroutineHandle handle) => { return selectionCoroutines.Contains(handle); });
                                    }
                                }
                                else
                                {
                                    //since the coroutines list hasn't changed, verify that each of them still exists
                                    for (int i = coroutines.Count - 1; i >= 0; i--)
                                    {
                                        if (!CoroutineManager.Instance.CoroutineExists(coroutines[i]))
                                        {
                                            coroutines.RemoveAt(i);
                                        }
                                    }
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
                                        coroutineIndex = (int)(mousePos.y / 20);
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
                                                selectedCoroutineChanged = true;
                                            }
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
                            else
                            {
                                EditorGUILayout.LabelField("Play mode required");
                                selectedCoroutines.Clear();
                            }
                        }//end scrollview scope

                        dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(separatorWidth + 4));
                        dividorRect = new Rect(0, dividorRect.y + 2, leftPaneWidth, 1);
                        DrawDividor(dividorRect);

                        breakOnFinished = EditorGUILayout.Toggle(new GUIContent("Break on Done", "Issues a Debug.Break call each time a coroutine finishes"), breakOnFinished);
                        if (CoroutineManager.Instance != null)
                            CoroutineManager.Instance.BreakOnFinished = breakOnFinished;

                        logSteps = EditorGUILayout.Toggle(new GUIContent("Log Steps", "Log each step of every coroutines execution"), logSteps);
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
            if (selectedCoroutines.Count == 1)
            {
                if (context is GameObject)
                {
                    //highlight the GameObject
                    Selection.activeGameObject = (GameObject)context;
                }
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
            SourceInfo sourceInfo = null;

            if (coroutineHandles.Count == 1)
            {
                sourceInfo = CoroutineManager.Instance.GetCoroutineSourceInfo(coroutineHandles[0], _debugInfo);
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
                int buttonSize = 20;
                //Load the Textures for the icons and then make a toolbar
                Texture2D playIcon = LoadCachedTexture(IconPath_Play);
                Texture2D stopicon = LoadCachedTexture(IconPath_Stop);
                Texture2D pauseIcon = LoadCachedTexture(IconPath_Pause);
                Texture2D selectedBG = LoadCachedTexture(IconPath_Selected);
                Texture2D stepIcon = LoadCachedTexture(IconPath_Step);
                Texture2D resetIcon = LoadCachedTexture(IconPath_Reset);

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

                    if (GUILayout.Button(new GUIContent(playIcon, "Resume a paused coroutine"), style, GUILayout.MaxWidth(buttonSize + 6)))
                    {
                        //if the coroutine is paused then resume it
                        if (coroutineIsPaused)
                        {
                            CoroutineManager.Instance.ResumeCoroutines(coroutineHandles);
                            Repaint();
                        }
                    }

                    style.normal.background = t;

                    if (GUILayout.Button(new GUIContent(stopicon, "Stop a running coroutine"), style, GUILayout.MaxWidth(buttonSize + 6)))
                    {
                        //kill this coroutine
                        CoroutineManager.Instance.StopCoroutines(coroutineHandles);
                        Repaint();
                    }

                    style.normal.background = coroutineIsPaused ? selectedBG : t;

                    if (GUILayout.Button(new GUIContent(pauseIcon, "Pause a running coroutine"), style, GUILayout.MaxWidth(buttonSize + 6)))
                    {
                        //if the coroutine is not paused then pause it
                        if (!coroutineIsPaused)
                        {
                            CoroutineManager.Instance.PauseCoroutines(coroutineHandles);
                            Repaint();
                        }
                    }

                    style.normal.background = t;


                    GUI.enabled = coroutineIsPaused;//step button is disabled unless we are paused

                    //step button is disabled unless we are paused
                    if (GUILayout.Button(new GUIContent(stepIcon, "Single-step a coroutine while it is paused"), style, GUILayout.MaxWidth(buttonSize + 6)))
                    {
                        //run a step of the coroutine
                        CoroutineManager.Instance.StepCoroutines(coroutineHandles);
                    }

                    GUI.enabled = true;


                    if (GUILayout.Button(new GUIContent(resetIcon, "Reset a coroutine"), style, GUILayout.MaxWidth(buttonSize + 6)))
                    {
                        //run a step of the coroutine
                        CoroutineManager.Instance.ResetCoroutines(coroutineHandles);
                    }

                }//end horizontal scope

                //end status toolbar

                if (coroutineHandles.Count == 1)
                {
                    var coroutineHandle = coroutineHandles[0];
                    IEnumerator c = CoroutineManager.Instance.GetCoroutineEnumerator(coroutineHandle);
                    if (c == null)
                    {
                        RemoveSelectedCoroutine(coroutineHandle);
                    }
                    else
                    {
                        Rect dividorRect = EditorGUILayout.GetControlRect(GUILayout.Height(2 + 4));
                        dividorRect = new Rect(0, dividorRect.y + 2, debugInfoAreaWidth, 1);
                        DrawDividor(dividorRect);

                        //what is the coroutine waiting on?
                        if (c.Current == null)
                        {
                            EditorGUILayout.LabelField("Waiting on: null");
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
                        else if (c.Current is WaitForLateUpdate)
                        {
                            EditorGUILayout.LabelField("Waiting on: " + c.Current.ToString());
                        }
                        else if (c.Current is WaitForFixedUpdate)
                        {
                            EditorGUILayout.LabelField("Waiting on: " + c.Current.ToString());
                        }
                        else if (c.Current is WaitForEndOfFrame)
                        {
                            EditorGUILayout.LabelField("Waiting on: " + c.Current.ToString());
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
                                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(sourceInfo.url, sourceInfo.lineNumber);
                            }
                        }

                        //extract the name between the < and >
                        string methodName = sourceInfo.enumeratorTypeName;
                        int start = methodName.IndexOf('<');
                        int end = methodName.IndexOf('>');
                        if (start != -1 && end != -1)
                        {
                            methodName = methodName.Substring(start + 1, end - start - 1);
                        }

                        EditorGUILayout.LabelField(sourceInfo.outerTypeName + "." + methodName + " (" + sourceInfo.url + ":" + sourceInfo.lineNumber + ")", EditorStyles.textField);
                    }
                }
            }//end vertical scope

            if (coroutineHandles.Count == 1)
            {
                var coroutineHandle = coroutineHandles[0];
                string stackTrace = CoroutineManager.Instance.GetStackTrace(coroutineHandle);
                string[] lines = stackTrace.Split('\n');

                //Find the first line in the stack that is not in aeric library code
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

                    //"Coroutinery/Stack collection"
                    bool collectStackTraces = EditorPrefs.GetBool("Coroutinery/Stack Traces", false);
                    if (!collectStackTraces)
                    {
                        EditorGUILayout.LabelField("Enable \"Coroutinery/Stack Traces\" to see stack traces. You will need to restart Play Mode.");
                        if (GUILayout.Button("Enable Stack Traces"))
                        {
                            EditorPrefs.SetBool("Coroutinery/Stack Traces", true);
                            Repaint();
                        }
                    }
                    else
                    {
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
                    }
                }//end vertical scope 
            }
        }


#if UNITY_EDITOR

        [MenuItem("GameObject/Show GameObject Coroutines", priority = 11)]
        static void ShowCoroutines(MenuCommand menuCommand)
        {
            filterOnSelection = true;
            searchContents = "{custom selection}";

            customSelection = true;
            coroutines = new List<CoroutineHandle>();
            var selectedObjects = Selection.gameObjects;
            foreach (var go in selectedObjects)
            {
                coroutines.AddRange(CoroutineManager.Instance.GetCoroutinesByContext(go));
            }
        }

        [MenuItem("Assets/Show Script Coroutines")]
        static void ShowScriptCoroutines()
        {
            //show all coroutines that were launched from this script
            var script = Selection.activeObject as MonoScript;
            if (script == null) return;
            //need to set the coroutines list to something custom and then only update it if the search parameters change
            customSelection = true;
            searchContents = "{custom - " + script.GetClass().Name + "}";

            //TODO: multi-selection?

            //get all coroutines that were launched from this script
            coroutines = CoroutineManager.Instance.GetCoroutinesBySource(script);
        }

        [MenuItem("Assets/Show Script Coroutines", true)]
        static bool ValidateShowScriptCoroutines()
        {
            // Test if the selected asset is a script
            return Application.isPlaying && Selection.activeObject.GetType() == typeof(MonoScript);
        }

#endif
    }
}