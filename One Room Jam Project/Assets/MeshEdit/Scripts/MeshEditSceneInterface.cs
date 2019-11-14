using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Globalization;
using System.Linq;
#if UNITY_EDITOR
[CustomEditor(typeof(MeshEdit))]
class MeshEditSceneInterface : Editor
{
    Texture[] tilesAvailable;

    GUISkin skin;
    GUISkin _skinDefault;
    GUISkin skinDefault
    {
        get
        {
            if (_skinDefault == null)
            {
                _skinDefault = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/MeshEdit/Resources/Default.guiskin");
            }

            return _skinDefault;
        }
    }
    Ray ray;


    int selectedTri = 0;
    int selectedQuad = 0;
    MeshEdit.Triangle t = new MeshEdit.Triangle(Vector3.zero, Vector3.zero, Vector3.zero);

    Texture tileSelected;
    Texture2D texWindow;
    Texture2D texPixel;
    Texture[] tileDirectionTexture;
    string[] editModes = new string[] { "Default", "Mesh Edit", "Tile Edit", "Vertex Colour" };


    Texture2D texColourSwatch;
    
    bool[] selectedEdges;

    int cpWidth = 256, cpHeight = 256;
    float colourPickerHue = 0.0f;
    Texture2D _colourPicker;
    Texture2D colourPicker
    {
        get
        {
            if (_colourPicker == null)
            {
                _colourPicker = new Texture2D(cpWidth, cpHeight, TextureFormat.RGB24, false);
                for (int y = 0; y < cpHeight; y++)
                {

                    for (int x = 0; x < cpWidth; x++)
                    {
                        Color colour = Color.HSVToRGB(colourPickerHue, (float)x / cpWidth, (float)y / cpHeight);
                        _colourPicker.SetPixel(x, y, colour);
                    }
                }
                _colourPicker.Apply();
            }
            return _colourPicker;
        }
        set
        {
            _colourPicker = value;
        }
    }

    Texture2D _huePicker;
    Texture2D huePicker
    {
        get
        {
            if (_huePicker == null)
            {
                _huePicker = new Texture2D(cpWidth, 24, TextureFormat.RGB24, false);
                for (int y = 0; y < 24; y++)
                {

                    for (int x = 0; x < cpWidth; x++)
                    {
                        Color colour = Color.HSVToRGB((float)x / cpWidth, 1.0f, 1.0f);
                        _huePicker.SetPixel(x, y, colour);
                    }
                }
                _huePicker.Apply();
            }
            return _huePicker;
        }
        set
        {
            _huePicker = value;
        }
    }

    DateTime frameStart;

    Texture2D[,] tiles;
    Texture2D tileset;

    Color orange = new Color(0.9f, 0.25f, 0.08f);


    bool shift = false;
    bool ctrl = false;
    bool alt = false;

    Vector2 moveAnchor;
    Vector3 anchorCenter;
    Vector3 transformDimensions;

    List<Vector3> oldVerts;

    bool helpMode = true;


    string[] circleVertices = { "6", "8", "10", "12", "16", "24", "32", "48", "64" };
    public static int[] circleVerticesCount = { 6, 8, 10, 12, 16, 24, 32, 48, 64 };

    Tool tempTool;
    void OnEnable()
    {

        MeshEdit meshEdit = getTargetMeshEditObject();
        if (meshEdit != null)
        {
            meshEdit.clearSavedTool();

            // Line included for the sake of forcing older version meshedit objects to update their lists
            meshEdit.checkMeshValidity();
        }

        Undo.undoRedoPerformed += undoCallback;
    }

    void OnDisable()
    {
        getTargetMeshEditObject().popSavedTool();

        Undo.undoRedoPerformed -= undoCallback;
    }

    void undoCallback()
    {
        // Do not consolidate this version of the callback with the one in the MeshEdit window. They look identical but they affect different instances of MeshEdit objects
        MeshEdit meshEdit = getTargetMeshEditObject();
        if (meshEdit != null)
        {
            meshEdit.pushNewGeometry();

            updateSelectionArray(meshEdit, meshEdit.verts.Count);
            //meshEdit.editorInfo.editOperation = MeshEdit.EditorInfo.MeshEditOperation.Standard;
        }
        else
        {
            //Debug.Log("MeshEdit object can't be found for undo!");
        }
    }

    private MeshEdit getTargetMeshEditObject()
    {
        MeshEdit meshEdit = target as MeshEdit;

        return meshEdit;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        /*
        if (GUILayout.Button("Test Conjugate"))
        {
            ((MeshEdit)target).testConjugateGradientSolver();
        }
        if (GUILayout.Button("Test Matrix ops"))
        {
            ((MeshEdit)target).testMatrixOps();
        }
        */
    }
    
    private void windowTest(int id)
    {
        EditorGUILayout.Popup(0, new string[] { "ho", "hi", "whoa" }, GUILayout.Width(100));
    }
    Rect windowRect = new Rect(600, 100, 100, 100);
    void OnSceneGUI()
    {
        if (skin == null)
        {
            skin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/MeshEdit/Resources/EditorTools.guiskin");
        }
        if (texWindow == null)
        {
            texWindow = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MeshEdit/Resources/texWindow.png");
            skin.box.normal.background = texWindow;
        }
        if (texColourSwatch == null)
        {
            texColourSwatch = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MeshEdit/Resources/texColourSwatch.png");
        }
        if (Selection.activeTransform != null &&
            Selection.activeTransform.gameObject != null &&
            Selection.activeTransform.gameObject.GetComponent<MeshFilter>() != null &&
            Selection.activeTransform.gameObject.GetComponent<MeshEdit>() != null)
        {
            MeshEdit solid = Selection.activeTransform.GetComponent<MeshEdit>();

            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Used)
            {

            }
            if (e.type != EventType.Layout && e.type != EventType.KeyDown && e.type != EventType.Repaint && e.type != EventType.MouseMove && e.type != EventType.MouseEnterWindow && e.type != EventType.MouseLeaveWindow)
            {

            }

            if (Event.current.type == EventType.Repaint)
            {
                solid.drawMeshWithGL();
            }
            // Shortcut setup
            shift = Event.current.shift;
            ctrl = Event.current.control || Event.current.command ||
                Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Command;
            alt = Event.current.alt;
            //Debug.Log("ALT: " + alt.ToString() + ", CTRL: " + ctrl.ToString() + ", SHIFT: " + shift.ToString());


            if (MeshEditWindow.tilesetsAvailable == null || MeshEditWindow.tilesetTexturesAvailable == null)
            {
                MeshEditWindow.loadTilesets();
            }

            if (tileDirectionTexture == null ||
                tileDirectionTexture[0] == null ||
                tileDirectionTexture[1] == null ||
                tileDirectionTexture[2] == null ||
                tileDirectionTexture[3] == null)
            {
                tileDirectionTexture = new Texture[4];
                for (int i = 0; i < 4; i++)
                {
                    tileDirectionTexture[i] = AssetDatabase.LoadAssetAtPath<Texture>("Assets/MeshEdit/Resources/TileDirection_" + i.ToString() + ".png");
                }
            }
            if (tileSelected == null)
            {
                tileSelected = AssetDatabase.LoadAssetAtPath<Texture>("Assets/MeshEdit/Resources/TileDirection_0");
            }
            if (texPixel == null)
            {
                texPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texPixel.SetPixel(0, 0, Color.white);
            }


            #region GUILayout


            guiHeader(solid);

            if (solid.editMode == 0)
            {
                //guiDefault(solid, controlId);
            }
            else if (solid.editMode == 2)
            {
                //guiTextureTiling(solid, controlId);

                operationsTextureTiling(solid, controlId);

            }
            else if (solid.editMode == 1)
            {
                //guiMeshEditing(solid, controlId);
                operationsMeshEdit(solid, controlId);
            }
            else if (solid.editMode == 3)
            {
                //guiColourEditing(solid, controlId);
                operationsColourEdit(solid, controlId);
            }

            if (solid.editMode != 1 || solid.editorInfo.editOperation != MeshEdit.EditorInfo.MeshEditOperation.LoopCut)
            {

                solid.facesCut = null;
                solid.cutsAB = null;
                solid.cutCount = 0;
            }
            #endregion


            #region Keyboard Shortcuts
            // CMD/CTRL can only be used during KeyUp or 
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Tab)
                {
                    focusEditorWindow();

                    //GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
            }

            // CMD/CTRL can only be used during KeyUp or 
            if (Event.current.type == EventType.KeyUp)
            {
                switch (Event.current.keyCode)
                {
                    case (KeyCode.Tab):
                        if (MeshEditWindow.isActive())
                        {
                            if (Event.current.modifiers == EventModifiers.Control ||
                        Event.current.modifiers == EventModifiers.Command)
                            {
                                if (solid.editMode == 1)
                                {
                                    int newVertMode = 1 - solid.vertMode;
                                    setVertMode(solid, newVertMode);
                                    saveSettings(solid);
                                }
                                else if (solid.editMode == 3)
                                {
                                    solid.paintMode = 1 - solid.paintMode;
                                    saveSettings(solid);
                                }
                            }
                            else
                            {

                                int newEditMode = 0;
                                if (shift)
                                {
                                    newEditMode = solid.editMode - 1;
                                }
                                else
                                {
                                    newEditMode = solid.editMode + 1;
                                }

                                if (newEditMode >= editModes.Length)
                                {
                                    newEditMode = 0;
                                }
                                if (newEditMode < 0)
                                {
                                    newEditMode = editModes.Length - 1;
                                }

                                solid.updateEditMode(newEditMode);
                            }

                            focusEditorWindow();
                        }
                        Event.current.Use();
                        break;
                }
            }
            else if (Event.current.type == EventType.KeyDown)
            {
                generalShortcutsOnKeydown(solid, shift, ctrl, alt);
            }
            /*
            //                          Rotate tile            Select tile from tileset                    Translate  Rotate     Scale      Set transform dimensions         Various utility keys
            KeyCode[] bannedKeyCodes = { KeyCode.Q, KeyCode.E, KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.G, KeyCode.R, KeyCode.S, KeyCode.X, KeyCode.Y, KeyCode.Z, KeyCode.Tab, KeyCode.Escape };
            if (Event.current.type == EventType.KeyDown)
            {
                for (int i = 0; i < bannedKeyCodes.Length; i++)
                {
                    if (Event.current.keyCode == bannedKeyCodes[i])
                    {
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                        break;
                    }
                }
            }*/
            #endregion

            if (solid.editorInfo.tileDirection > 3)
            {
                solid.editorInfo.tileDirection = 0;
            }
            else if (solid.editorInfo.tileDirection < 0)
            {
                solid.editorInfo.tileDirection = 3;
            }


        }
        else
        {

        }


    }

    public static void generalShortcutsOnKeydown(MeshEdit solid, bool shift, bool ctrl, bool alt)
    {
        #region View shortcuts
        
        // View
        if (Event.current.keyCode == KeyCode.Keypad1)
        {
            // Y- Align
            Camera camera = SceneView.lastActiveSceneView.camera;
            float d = Vector3.Distance(camera.transform.position, SceneView.lastActiveSceneView.pivot);
            Vector3 forward = new Vector3(0.0f, 0.0f, 1.0f);

            if (SceneView.lastActiveSceneView.rotation == Quaternion.LookRotation(forward)) { forward = -forward; }
            SceneView.lastActiveSceneView.rotation = Quaternion.LookRotation(forward);

            SceneView.lastActiveSceneView.Repaint();
            
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.Keypad3)
        {
            // X- Align
            Camera camera = SceneView.lastActiveSceneView.camera;
            float d = Vector3.Distance(camera.transform.position, SceneView.lastActiveSceneView.pivot);
            Vector3 forward = new Vector3(1.0f, 0.0f, 0.0f);
            if (shift)
            {
                forward = -forward;
            }

            if (SceneView.lastActiveSceneView.rotation == Quaternion.LookRotation(forward)) { forward = -forward; }
            SceneView.lastActiveSceneView.rotation = Quaternion.LookRotation(forward);

            SceneView.lastActiveSceneView.Repaint();
            
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.Keypad7)
        {
            // Z- Align
            Camera camera = SceneView.lastActiveSceneView.camera;
            float d = Vector3.Distance(camera.transform.position, SceneView.lastActiveSceneView.pivot);
            Vector3 forward = new Vector3(0.0f, -1.0f, 0.0f);
            if (shift)
            {
                forward = -forward;
            }

            if (SceneView.lastActiveSceneView.rotation == Quaternion.LookRotation(forward)) { forward = -forward; }
            SceneView.lastActiveSceneView.rotation = Quaternion.LookRotation(forward);

            SceneView.lastActiveSceneView.Repaint();

            
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.Keypad5)
        {
            // ISO/PERSP
            SceneView.lastActiveSceneView.orthographic = !SceneView.lastActiveSceneView.orthographic;
            SceneView.lastActiveSceneView.Repaint();
            
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.Slash)
        {
            //   Debug.Log("Hide other editable meshes");
            Event.current.Use();

        }
        #endregion
        switch (Event.current.keyCode)
        {
            case (KeyCode.A):
                if (solid.editMode == 2)
                {
                    if (solid.editorInfo.selectedTile % solid.editorInfo.tilesPerRow - 1 < 0)
                    {
                        solid.editorInfo.selectedTile += 7;
                    }
                    else
                    {
                        solid.editorInfo.selectedTile--;
                    }

                    Event.current.Use();
                }
                break;
            case (KeyCode.D):
                if (solid.editMode == 2)
                {
                    if ((solid.editorInfo.selectedTile % solid.editorInfo.tilesPerRow) + 1 >= solid.editorInfo.tilesPerRow || solid.editorInfo.selectedTile + 1 >= solid.editorInfo.tileCount)
                    {
                        solid.editorInfo.selectedTile = solid.editorInfo.selectedTile - solid.editorInfo.selectedTile % solid.editorInfo.tilesPerRow;
                    }
                    else
                    {
                        solid.editorInfo.selectedTile++;
                    }

                    Event.current.Use();
                }
                break;
            case (KeyCode.W):
                if (solid.editMode == 2)
                {
                    if (solid.editorInfo.selectedTile - solid.editorInfo.tilesPerRow < 0)
                    {
                        solid.editorInfo.selectedTile = (solid.editorInfo.tileCount / solid.editorInfo.tilesPerRow) * solid.editorInfo.tilesPerRow + solid.editorInfo.selectedTile - solid.editorInfo.tilesPerRow;
                    }
                    else
                    {
                        solid.editorInfo.selectedTile -= solid.editorInfo.tilesPerRow;
                    }

                    Event.current.Use();
                }
                else if (solid.editMode != 0)
                {
                    Event.current.Use();
                }
                break;
            case (KeyCode.S):
                if (solid.editMode == 2 && !ctrl)
                {
                    if (solid.editorInfo.selectedTile + solid.editorInfo.tilesPerRow >= solid.editorInfo.tileCount)
                    {
                        int x = solid.editorInfo.selectedTile - (solid.editorInfo.selectedTile / solid.editorInfo.tilesPerRow) * solid.editorInfo.tilesPerRow;

                        solid.editorInfo.selectedTile = x;
                    }
                    else
                    {
                        solid.editorInfo.selectedTile += solid.editorInfo.tilesPerRow;
                    }


                    Event.current.Use();
                }
                break;

            case (KeyCode.E):
                if (solid.editMode == 2)
                {
                    solid.editorInfo.tileDirection++;

                    Event.current.Use();
                }
                else if (solid.editMode != 0)
                {
                    Event.current.Use();
                }
                break;

            case (KeyCode.Q):
                if (solid.editMode == 2)
                {
                    solid.editorInfo.tileDirection--;

                    Event.current.Use();
                }
                break;
            case (KeyCode.Z):
                if (Event.current.modifiers == EventModifiers.None)
                {
                    solid.isMeshTransparent = !solid.isMeshTransparent;
                    solid.GetComponent<MeshRenderer>().enabled = solid.isMeshTransparent;

                    Event.current.Use();
                }
                break;
            case (KeyCode.R):
                if (solid.editMode != 0)
                {
                    // Block the transform from being activated
                    Event.current.Use();
                }
                break;
            case (KeyCode.T):
                if (solid.editMode != 0)
                {
                    // Block the transform from being activated
                    Event.current.Use();
                }
                break;
            case (KeyCode.Delete):
                if (solid.editMode != 0)
                {
                    Event.current.Use();
                }
                break;
        }
    }

    private void focusSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        sceneView.Focus();
    }

    private void focusEditorWindow()
    {
        MeshEditWindow window = MeshEditWindow.window;
        if (window != null)
        {
            window.Focus();
        }
    }

    private void saveSettings(MeshEdit meshEdit)
    {
        if (MeshEditWindow.isActive())
        {
            MeshEditWindow.saveSettings(meshEdit, MeshEditWindow.window);
        }
    }
    private void loadSettings(MeshEdit meshEdit)
    {
        if (MeshEditWindow.isActive())
        {
            MeshEditWindow.loadSettings(meshEdit, MeshEditWindow.window);
        }
    }

    private void setVertMode(MeshEdit solid, int newVertMode)
    {
        solid.vertMode = newVertMode;

        if (solid.vertMode == 0)
        {
            // To verts
            solid.selectionConvertToVerts();
            selectionAddTouchingVerts(solid);

            solid.selectedFaces = new bool[solid.selectedFaces.Length];
        }
        else if (solid.vertMode == 1)
        {
            // To faces
            solid.selectionConvertToFaces();
            solid.selectedVerts = new bool[solid.selectedVerts.Length];
        }
    }

    #region bresenham circle
    private void circleBres(Texture2D texture, int xc, int yc, int r, Color colour)
    {
        int x = 0, y = r;
        int d = 3 - 2 * r;
        while (y >= x)
        {
            drawCircle(texture, xc, yc, x, y, colour);
            x++;
            
            if (d > 0)
            {
                y--;
                d = d + 4 * (x - y) + 10;
            }
            else
            {
                d = d + 4 * x + 6;
                drawCircle(texture, xc, yc, x, y, colour);
            }
        }
    }
    private void drawCircle(Texture2D texture, int xc, int yc, int x, int y, Color colour)
    {
        texture.SetPixel(xc + x, yc + y, colour);
        texture.SetPixel(xc - x, yc + y, colour);
        texture.SetPixel(xc + x, yc - y, colour);
        texture.SetPixel(xc - x, yc - y, colour);
        texture.SetPixel(xc + y, yc + x, colour);
        texture.SetPixel(xc - y, yc + x, colour);
        texture.SetPixel(xc + y, yc - x, colour);
        texture.SetPixel(xc - y, yc - x, colour);
    }
    #endregion
    Texture2D _selectCircleTexture;
    Texture2D selectCircleTexture
    {
        get
        {
            int r = (int)(selectionCircleRadius + 0.5f);
            if (_selectCircleTexture == null ||
                _selectCircleTexture.width / 2 != r)
            {
                _selectCircleTexture = new Texture2D(r * 2, r * 2, TextureFormat.RGBA32, false);
                Color32[] bgPixels = new Color32[(r * 2) * (r * 2)];
                for (int i = 0; i < bgPixels.Length; i++)
                {
                    bgPixels[i] = Color.clear;
                }
                _selectCircleTexture.SetPixels32(bgPixels);
                circleBres(_selectCircleTexture, r, r, r - 1, Color.white);
                _selectCircleTexture.Apply();
            }
            return _selectCircleTexture;
        }
    }
    bool transformDimensionsExtrude = false;
    bool transformDimensionsPlanar = false;
    float selectionCircleRadius = 10.0f;
    int closestFace = -1;
    bool isNSLoopCut = false;
    List<int> facesCut;
    List<int> cutsAB;
    List<int[]> cutsAdjacentFaces;
    bool isCutLooping = false;
    float cutCount = 1.5f;

    public void Awake()
    {
        //Debug.Log("onAwake");
    }


    public void OnDestroy()
    {
        //Debug.Log("Destroyed");
    }
    
    private void operationsMeshEdit(MeshEdit solid, int controlId)
    {
        Vector3 qCenter = Vector2.zero;
        
        // This used to be done every opportunity but now we just check validity whenever the interface becomes active (onenable)
        //solid.checkMeshValidity();
        

        Vector2 pos = Vector2.zero;

        //Tools.current = Tool.None;
        //Tools.hidden = true;
        
        bool activateRightClick = false;
        bool isAdditive = false;
        float selectDistance = 25;
        Vector2 mousePos = Vector2.zero;
        mousePos = Event.current.mousePosition;
        mousePos = constrainToScreenSize(mousePos);

        if (solid.editorInfo.editOperation == MeshEdit.EditorInfo.MeshEditOperation.SelectCircle)
        {
            #region Selection Mode
            if (Event.current.type == EventType.ScrollWheel)
            {
                float s = Event.current.delta.y;
                selectionCircleRadius += s;
                if (selectionCircleRadius < 1)
                {
                    selectionCircleRadius = 1;
                }
                else if (selectionCircleRadius > 100)
                {
                    selectionCircleRadius = 100;
                }
                SceneView.RepaintAll();
                GUIUtility.hotControl = controlId;
                Event.current.Use();
            }
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;

            Graphics.DrawTexture(
                new Rect(
                    mousePos.x - selectionCircleRadius,
                    mousePos.y - selectionCircleRadius,
                    selectCircleTexture.width,
                    selectCircleTexture.width),
                selectCircleTexture);

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
            {
                if (Event.current.button == 1)
                {
                    solid.editorInfo.editOperation = MeshEdit.EditorInfo.MeshEditOperation.Standard;
                    //GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
                if (solid.vertMode == 0) // Select Verts
                {
                    if (Event.current.button == 0)
                    {
                        for (int i = 0; i < solid.verts.Count; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.verts[i]);
                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                if (!solid.isVertCovered(i) || solid.isMeshTransparent)
                                {
                                    solid.selectedVerts[i] = true;
                                }
                            }
                        }
                    }
                    else if (Event.current.button == 2)
                    {

                        for (int i = 0; i < solid.verts.Count; i++)
                        {
                            if (solid.selectedVerts[i])
                            {
                                Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.verts[i]);
                                float d = (mousePos - vertScreen).sqrMagnitude;
                                if (d < selectionCircleRadius * selectionCircleRadius)
                                {
                                    if (!solid.isVertCovered(i) || solid.isMeshTransparent)
                                    {
                                        solid.selectedVerts[i] = false;
                                    }
                                }
                            }
                        }

                    }
                }
                else if (solid.vertMode == 1) // Select Quads
                {
                    if (Event.current.button == 0)
                    {


                        for (int i = 0; i < solid.selectedFaces.Length; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.quadCenter(i));

                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                if (!solid.isFaceCovered(i) || solid.isMeshTransparent)
                                {
                                    solid.selectedFaces[i] = true;
                                }
                            }
                        }
                    }
                    else if (Event.current.button == 2)
                    {
                        for (int i = 0; i < solid.selectedFaces.Length; i++)
                        {
                            if (solid.selectedFaces[i])
                            {
                                Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.quadCenter(i));

                                float d = (mousePos - vertScreen).sqrMagnitude;
                                if (d < selectionCircleRadius * selectionCircleRadius)
                                {
                                    if (!solid.isFaceCovered(i) || solid.isMeshTransparent)
                                    {
                                        solid.selectedFaces[i] = false;
                                    }
                                }
                            }
                        }
                    }
                }
                SceneView.RepaintAll();
                GUIUtility.hotControl = controlId;
                Event.current.Use();
            }
            #endregion
        }
        else if (solid.editorInfo.editOperation == MeshEdit.EditorInfo.MeshEditOperation.LoopCut)
        {
            #region Loop Cut
            float loopCutMinDistance = 256 * 256;

            if (Event.current.type == EventType.ScrollWheel)
            {
                cutCount -= Event.current.delta.y / 3.0f;
                if (cutCount < 1)
                {
                    cutCount = 1;
                }
                if (cutCount > 20)
                {
                    cutCount = 20;
                }
                GUIUtility.hotControl = controlId;
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseMove)
            {
                // Get closest face to mouse
                float dist = float.MaxValue;
                int f = -1;

                bool areAnyCutsFound = false;

                for (int i = 0; i < solid.selectedFaces.Length; i++)
                {
                    // Get NS/EW axis on face
                    if (Vector3.Dot(SceneView.lastActiveSceneView.camera.transform.forward, solid.faceNormals[i]) < 0 && !solid.isFaceCovered(i))
                    {
                        Vector3 n = (solid.verts[solid.quads[i * 4 + 0]] + solid.verts[solid.quads[i * 4 + 2]]) / 2;
                        Vector3 s = (solid.verts[solid.quads[i * 4 + 3]] + solid.verts[solid.quads[i * 4 + 1]]) / 2;
                        Vector2 vN = HandleUtility.WorldToGUIPoint(n);
                        Vector2 vS = HandleUtility.WorldToGUIPoint(s);
                        float d2NS = (MeshEdit.closestPoint(vN, vS, mousePos, true) - mousePos).sqrMagnitude;

                        Vector3 e = (solid.verts[solid.quads[i * 4 + 0]] + solid.verts[solid.quads[i * 4 + 1]]) / 2;
                        Vector3 w = (solid.verts[solid.quads[i * 4 + 3]] + solid.verts[solid.quads[i * 4 + 2]]) / 2;
                        Vector2 vE = HandleUtility.WorldToGUIPoint(e);
                        Vector2 vW = HandleUtility.WorldToGUIPoint(w);
                        float d2EW = (MeshEdit.closestPoint(vE, vW, mousePos, true) - mousePos).sqrMagnitude;
                        if (i == 0)
                        {
                        }
                        float min = Mathf.Min(d2NS, d2EW);
                        if (min < dist && min < loopCutMinDistance)
                        {
                            isNSLoopCut = (d2NS < d2EW);

                            dist = min;
                            f = i;
                            areAnyCutsFound = true;
                        }

                    }
                }

                closestFace = f * 4;
                facesCut = new List<int>();
                cutsAB = new List<int>();
                cutsAdjacentFaces = new List<int[]>();

                if (areAnyCutsFound)
                {
                    if (isNSLoopCut)
                    {
                        int face = f;
                        int a = 2;
                        int b = 0;
                        int c = -1;
                        int d = -1;

                        // Find the end of the loop before starting the cut, in case the mesh doesn't actually loop
                        solid.getFirstFaceInLoop(ref face, ref a, ref b, ref c, ref d);

                        solid.getOppositeSideOfQuadRelative(face, c, d, out c, out d);

                        isCutLooping = false;
                        //solid.getLoopCut(face, c, d, ref facesCut, ref cutsAB, ref isCutLooping);
                        solid.getLoopCut(face, c, d, ref facesCut, ref cutsAB, ref cutsAdjacentFaces, ref isCutLooping);
                        //solid.getLoopCutV2(f, false, ref facesCut, ref cutDirection, ref cutsAB, ref isCutLooping);
                    }
                    else
                    {
                        int face = f;
                        int a = 0;
                        int b = 1;
                        int c = -1;
                        int d = -1;

                        // Find the end of the loop before starting the cut, in case the mesh doesn't actually loop

                        solid.getFirstFaceInLoop(ref face, ref a, ref b, ref c, ref d);

                        solid.getOppositeSideOfQuadRelative(face, c, d, out c, out d);

                        //Debug.DrawLine((solid.verts[solid.quads[face * 4 + a]] + solid.verts[solid.quads[face * 4 + b]]) / 2, (solid.verts[solid.quads[face * 4 + c]] + solid.verts[solid.quads[face * 4 + d]]) / 2, Color.blue);

                        isCutLooping = false;
                        solid.getLoopCut(face, c, d, ref facesCut, ref cutsAB, ref cutsAdjacentFaces, ref isCutLooping);
                        //solid.getLoopCutV2(f, true, ref facesCut, ref tempAdjacentFaces, ref cutsAB, ref isCutLooping);

                    }

                    SceneView.RepaintAll();
                }
                else
                {
                    solid.cutsAB = new List<int>();
                    solid.facesCut = new List<int>();
                }
            }

            if (closestFace >= 0)
            {
                solid.facesCut = facesCut;
                solid.cutsAB = cutsAB;
                solid.cutCount = cutCount;
            }
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0)
                {
                    solid.editorInfo.editOperation = MeshEdit.EditorInfo.MeshEditOperation.Standard;

                    Undo.RegisterCompleteObjectUndo(solid, "Mesh Loop Cut");

                    loopCut(solid);

                    solid.selectedFaces = new bool[solid.selectedFaces.Length];
                }
                if (Event.current.button == 1)
                {
                    solid.editorInfo.editOperation = MeshEdit.EditorInfo.MeshEditOperation.Standard;
                }
                GUIUtility.hotControl = controlId;
                Event.current.Use();
            }
            #endregion
        }
        else if (solid.editorInfo.editOperation == MeshEdit.EditorInfo.MeshEditOperation.Standard)
        {
            #region Mesh Editing
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 1)
                {
                    activateRightClick = true;

                    if (shift)
                    {
                        isAdditive = true;
                    }

                    if (solid.editorInfo.transformMode > 0)
                    {
                        // TODO: Revert
                        solid.verts = oldVerts;
                        updateMeshVerts(solid);

                        transformDimensions.x = 1.0f;
                        transformDimensions.y = 1.0f;
                        transformDimensions.z = 1.0f;
                        activateRightClick = false;
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                }
                else if (Event.current.button == 0)
                {
                    if (solid.editorInfo.transformMode == 0)
                    {
                        bool wereAnyVertsSelected = false;
                        for (int i = 0; i < solid.selectedVerts.Length; i++)
                        {
                            if (solid.selectedVerts[i])
                            {
                                wereAnyVertsSelected = true;
                                break;
                            }

                        }
                        if (!wereAnyVertsSelected)
                        {
                            for (int i = 0; i < solid.selectedFaces.Length; i++)
                            {
                                if (solid.selectedFaces[i])
                                {
                                    wereAnyVertsSelected = true;
                                    break;
                                }

                            }
                        }

                        if (wereAnyVertsSelected)
                        {
                            Undo.RegisterCompleteObjectUndo(solid, "Deselect all");
                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        clearSelected(solid);
                        SceneView sceneView = SceneView.lastActiveSceneView;
                        sceneView.Focus();

                    }
                    else
                    {

                    }
                }
                if (solid.editorInfo.transformMode > 0)
                {
                    solid.editorInfo.transformMode = 0;
                    transformDimensions = new Vector3(0, 0, 0);
                    transformDimensionsPlanar = false;

                    updateMeshEditNormals(solid);

                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
            }



            float closestSelect = float.MaxValue;
            int closestVert = -1;
            int closestEdgeA = -1;
            int closestEdgeB = -1;
            bool wasALineSelected = false;
            Vector3 closestColPoint = Vector3.zero;

            #region Vertex/Face selection
            if (solid.editorInfo.transformMode == 0)
            {
                if (activateRightClick)
                {
                    {
                        #region Standard selection
                        if (solid.vertMode == 0)
                        {
                            if (Event.current.modifiers == (EventModifiers.Alt | EventModifiers.Shift) ||
                                Event.current.modifiers == EventModifiers.Alt)
                            {
                                // Search for edges instead of verts
                                for (int i = 0; i < solid.quads.Count; i += 4)
                                {
                                    for (int j = 0; j < MeshEdit.quadEdgePatternClockwise.Length; j += 2)
                                    {
                                        int va = solid.quads[i + MeshEdit.quadEdgePatternClockwise[j + 0]];
                                        int vb = solid.quads[i + MeshEdit.quadEdgePatternClockwise[j + 1]];

                                        // TODO: Find a more elegant solution to this issue
                                        Vector2 a = HandleUtility.WorldToGUIPoint(solid.verts[va]);
                                        Vector2 b = HandleUtility.WorldToGUIPoint(solid.verts[vb]);

                                        Vector2 closest = closestPoint(a, b, mousePos);
                                        float d = (closest - mousePos).sqrMagnitude;

                                        if (d < selectDistance * selectDistance &&
                                            d < closestSelect)
                                        {

                                            float r = 0.0f;
                                            float dAB = (b - a).magnitude;
                                            float dAC = (closest - a).magnitude;
                                            if (d > 0)
                                            {
                                                r = dAC / dAB;
                                            }

                                            Vector3 coveredPoint = solid.verts[va] + (solid.verts[vb] - solid.verts[va]) * r;

                                            if (!solid.isMeshTransparent)
                                            {
                                                if (solid.isVertCovered(coveredPoint))
                                                {
                                                    continue;
                                                }
                                            }
                                            closestSelect = d;
                                            // The root face
                                            closestVert = i / 4;
                                            closestEdgeA = va;
                                            closestEdgeB = vb;
                                            wasALineSelected = true;

                                        }
                                    }
                                }
                            }
                            else
                            {

                                for (int i = 0; i < solid.verts.Count; i++)
                                {
                                    pos = HandleUtility.WorldToGUIPoint(solid.verts[i]);

                                    float d = Vector2.Distance(pos, mousePos);
                                    if (d < selectDistance && d < closestSelect)
                                    {
                                        closestSelect = d;
                                        closestVert = i;
                                    }
                                }
                            }
                        }
                        else if (solid.vertMode == 1)
                        {
                            ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                            float dist2Intersection = float.MaxValue;

                            if (solid.tris != null)
                            {
                                Vector3 colPoint = Vector3.zero;
                                float d = float.MaxValue;
                                for (int i = 0; i < solid.tris.Count; i += 3)
                                {
                                    t = new MeshEdit.Triangle(
                                        solid.verts[solid.tris[i + 0]],
                                        solid.verts[solid.tris[i + 1]],
                                        solid.verts[solid.tris[i + 2]]);

                                    if (rayIntersectsTriangle(ray.origin, ray.direction, t, ref colPoint))
                                    {
                                        pos = HandleUtility.WorldToGUIPoint(colPoint);

                                        if (solid.isMeshTransparent)
                                        {
                                            float dd = Vector2.Distance(pos, HandleUtility.WorldToGUIPoint(solid.quadCenter(i / 6)));
                                            if (dd < d)
                                            {
                                                d = dd;
                                                if (solid.isMeshTransparent)
                                                {
                                                    closestVert = i / 6;
                                                    closestColPoint = colPoint;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            float dist2IntersectionTemp = Vector3.Distance(colPoint, ray.origin);
                                            if (dist2IntersectionTemp < dist2Intersection)
                                            {
                                                dist2Intersection = dist2IntersectionTemp;
                                                closestVert = i / 6;
                                                closestColPoint = colPoint;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (closestVert >= 0)
                        {
                            Undo.RegisterCompleteObjectUndo(solid, "Select loop");
                            if (!shift) { clearSelected(solid); }


                            if (wasALineSelected)
                            {
                                #region Edge Loop selection

                                List<int> loopPartial = getEdgeLoop(solid, closestVert, closestEdgeA, closestEdgeB);


                                bool select = true;
                                if (shift && solid.selectedVerts[closestEdgeA] && solid.selectedVerts[closestEdgeB])
                                {
                                    select = false;
                                }

                                for (int i = 0; i < loopPartial.Count; i++)
                                {
                                    setSelectVert(solid, loopPartial[i], select);
                                }
                                #endregion
                            }
                            else if (
                                solid.vertMode == 1 &&
                                (Event.current.modifiers == (EventModifiers.Alt | EventModifiers.Shift) ||
                                Event.current.modifiers == EventModifiers.Alt))
                            {
                                #region Face Loop selection
                                Vector2 middleOfEdge0 = HandleUtility.WorldToGUIPoint(
                                    (solid.verts[solid.quads[closestVert * 4 + 0]] + solid.verts[solid.quads[closestVert * 4 + 1]]) / 2);
                                Vector2 middleOfEdge2 = HandleUtility.WorldToGUIPoint(
                                    (solid.verts[solid.quads[closestVert * 4 + 3]] + solid.verts[solid.quads[closestVert * 4 + 2]]) / 2);

                                Vector2 middleOfEdge1 = HandleUtility.WorldToGUIPoint(
                                    (solid.verts[solid.quads[closestVert * 4 + 1]] + solid.verts[solid.quads[closestVert * 4 + 3]]) / 2);
                                Vector2 middleOfEdge3 = HandleUtility.WorldToGUIPoint(
                                    (solid.verts[solid.quads[closestVert * 4 + 2]] + solid.verts[solid.quads[closestVert * 4 + 0]]) / 2);

                                float dNS = (closestPoint(middleOfEdge0, middleOfEdge2, mousePos) - mousePos).sqrMagnitude;
                                float dEW = (closestPoint(middleOfEdge1, middleOfEdge3, mousePos) - mousePos).sqrMagnitude;

                                bool isNS = true;

                                if (dEW < dNS)
                                {
                                    isNS = false;
                                }

                                List<int> addedFaces = MeshEdit.getFaceLoop(solid.adjacentFaces, closestVert, isNS);
                                if (solid.selectedFaces[closestVert])
                                {
                                    for (int i = 0; i < addedFaces.Count; i++)
                                    {
                                        solid.selectedFaces[addedFaces[i]] = false;
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < addedFaces.Count; i++)
                                    {
                                        solid.selectedFaces[addedFaces[i]] = true;
                                    }
                                }
                                #endregion
                            }
                            else
                            {
                                if (solid.vertMode == 0)
                                {
                                    if (!solid.isVertCovered(closestVert) || solid.isMeshTransparent)
                                    {
                                        selectVert(solid, closestVert, isAdditive);
                                    }
                                }
                                else if (solid.vertMode == 1)
                                {
                                    if (!solid.isFaceCovered(closestVert) || solid.isMeshTransparent)
                                    {
                                        solid.selectedFaces[closestVert] = !isAdditive || !solid.selectedFaces[closestVert];
                                    }
                                }
                            }

                            //GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        #endregion
                    }
                }
            }
            #endregion
            else if (Event.current.type == EventType.MouseMove)
            {
                if (solid.editorInfo.transformMode == 1)
                {
                    Ray r = HandleUtility.GUIPointToWorldRay(moveAnchor);

                    Ray r2 = HandleUtility.GUIPointToWorldRay(mousePos);
                    float d = Vector3.Distance(anchorCenter, SceneView.lastActiveSceneView.camera.transform.position);

                    Vector2 offset = mousePos - moveAnchor;
                    Vector3 realOffset =
                        SceneView.lastActiveSceneView.camera.transform.right * offset.x +
                        SceneView.lastActiveSceneView.camera.transform.up * -offset.y;

                    realOffset = r.GetPoint(d) - r2.GetPoint(d);

                    float s = 0;
                    if (transformDimensionsExtrude)
                    {
                        s = getValueFromMouseAxisPosition(transformDimensions, anchorCenter, moveAnchor, mousePos, false);
                        realOffset = transformDimensions * s;
                    }
                    
                    if (transformDimensionsPlanar)
                    {
                        
                        Vector3 planeNormal = new Vector3(
                            1.0f - transformDimensions.x,
                            1.0f - transformDimensions.y,
                            1.0f - transformDimensions.z).normalized;

                        Ray rAnchor = SceneView.lastActiveSceneView.camera.ScreenPointToRay(new Vector2(moveAnchor.x, SceneView.lastActiveSceneView.camera.pixelHeight - moveAnchor.y));
                        Ray rMouse = SceneView.lastActiveSceneView.camera.ScreenPointToRay(new Vector2(mousePos.x, SceneView.lastActiveSceneView.camera.pixelHeight - mousePos.y));

                        Vector3 o = anchorCenter - SceneView.lastActiveSceneView.camera.transform.position;

                        float offsetValueFromAnchor = Vector3.Dot(anchorCenter - rAnchor.origin, planeNormal);
                        float offsetValueFromMouse = Vector3.Dot(anchorCenter - rMouse.origin, planeNormal);
                        float anchorValue = Vector3.Dot(rAnchor.direction.normalized, planeNormal);
                        float mouseValue = Vector3.Dot(rMouse.direction.normalized, planeNormal);
                        // Distance along the ray toward the plane is a ratio of dot products of the plane normal against the relative position and the ray directions.
                        if (anchorValue != 0 && mouseValue != 0)
                        {
                            float distanceToAnchorIntersection = offsetValueFromAnchor / anchorValue;
                            float distanceToMouseIntersection = offsetValueFromMouse / mouseValue;
                            realOffset = rMouse.GetPoint(distanceToMouseIntersection) - rAnchor.GetPoint(distanceToAnchorIntersection);
                        }

                    }

                    // Snap
                    if (ctrl)
                    {
                        float d2target = (SceneView.lastActiveSceneView.camera.transform.position - anchorCenter).magnitude / 20;
                        d2target = Mathf.Round(d2target);

                        float scale = 2 * d2target;
                        if (scale < 1)
                        {
                            scale = 1;
                        }
                        if (shift)
                        {
                            scale /= 10;
                        }
                        if (!transformDimensionsExtrude)
                        {
                            realOffset.x = Mathf.Round(realOffset.x / scale) * scale;
                            realOffset.y = Mathf.Round(realOffset.y / scale) * scale;
                            realOffset.z = Mathf.Round(realOffset.z / scale) * scale;
                        }
                        else
                        {

                            realOffset = transformDimensions * (Mathf.Round(s / scale) * scale);
                        }
                    }


                    if (transformDimensionsExtrude || transformDimensionsPlanar)
                    {
                        for (int i = 0; i < solid.selectedVerts.Length; i++)
                        {
                            if (solid.selectedVerts[i])
                            {
                                solid.verts[i] = oldVerts[i] + realOffset;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < solid.selectedVerts.Length; i++)
                        {
                            if (solid.selectedVerts[i])
                            {
                                solid.verts[i] = oldVerts[i] - maskVector(realOffset, transformDimensions);
                            }
                        }
                    }
                    updateMeshVerts(solid, affectedVerts, affectedFaces);

                }
                else if (solid.editorInfo.transformMode == 2)
                {
                    Vector2 screenAnchorCenter = HandleUtility.WorldToGUIPoint(anchorCenter);

                    Vector2 originOffset = (screenAnchorCenter - moveAnchor);

                    Vector2 currentOffset = (screenAnchorCenter - mousePos);

                    float angleOrigin = Mathf.Atan2(-originOffset.y, originOffset.x);
                    float angleCurrent = Mathf.Atan2(-currentOffset.y, currentOffset.x);
                    float angle = (angleCurrent - angleOrigin) * Mathf.Rad2Deg;

                    Vector3 axis = SceneView.lastActiveSceneView.camera.transform.forward;

                    if (transformDimensions.sqrMagnitude == 1)
                    {
                        axis = transformDimensions;
                    }

                    // Snap
                    if (ctrl)
                    {
                        float scale = 180.0f / 8.0f;
                        if (shift)
                        {
                            scale /= 4.0f;
                        }

                        angle = Mathf.Round(angle / scale) * scale;
                    }
                    Quaternion q = Quaternion.AngleAxis(angle, axis);

                    for (int i = 0; i < solid.selectedVerts.Length; i++)
                    {
                        if (solid.selectedVerts[i])
                        {
                            solid.verts[i] = anchorCenter + q * (oldVerts[i] - anchorCenter);
                        }
                    }
                    updateMeshVerts(solid, affectedVerts, affectedFaces);
                }
                else if (solid.editorInfo.transformMode == 3)
                {
                    Vector2 screenAnchorCenter = HandleUtility.WorldToGUIPoint(anchorCenter);

                    float d2 = (screenAnchorCenter - moveAnchor).magnitude;

                    float d = (screenAnchorCenter - mousePos).magnitude;

                    float dot = Vector2.Dot(screenAnchorCenter - moveAnchor, screenAnchorCenter - mousePos);

                    float s = d / d2;

                    // Snap
                    if (ctrl)
                    {
                        float scale = 10;
                        if (shift)
                        {
                            scale *= 10;
                        }

                        s = Mathf.Round(s * scale) / scale;
                    }
                    for (int i = 0; i < solid.selectedVerts.Length; i++)
                    {
                        if (solid.selectedVerts[i])
                        {
                            Vector3 newVert = Vector3.zero;
                            float sx = s;
                            if (transformDimensions.x == 0) { sx = 1; }
                            newVert.x = anchorCenter.x + (oldVerts[i] - anchorCenter).x * (sx) * Mathf.Sign(dot);
                            sx = s;
                            if (transformDimensions.y == 0) { sx = 1; }
                            newVert.y = anchorCenter.y + (oldVerts[i] - anchorCenter).y * (sx) * Mathf.Sign(dot);
                            sx = s;
                            if (transformDimensions.z == 0) { sx = 1; }
                            newVert.z = anchorCenter.z + (oldVerts[i] - anchorCenter).z * (sx) * Mathf.Sign(dot);

                            solid.verts[i] = newVert;
                        }
                    }
                    updateMeshVerts(solid, affectedVerts, affectedFaces);
                }
            }

            if (solid.editorInfo.transformMode > 0 && transformDimensions.sqrMagnitude > 0 && transformDimensions.sqrMagnitude < 3 && !transformDimensionsExtrude)
            {
                Vector3 x = new Vector3(1.0f, 0.0f, 0.0f) * transformDimensions.x;
                Vector3 y = new Vector3(0.0f, 1.0f, 0.0f) * transformDimensions.y;
                Vector3 z = new Vector3(0.0f, 0.0f, 1.0f) * transformDimensions.z;
                Debug.DrawLine(anchorCenter, anchorCenter + x * 1000000000000.0f, Color.red);
                Debug.DrawLine(anchorCenter, anchorCenter + x * -1000000000000.0f, Color.red);

                Debug.DrawLine(anchorCenter, anchorCenter + y * 1000000000000.0f, Color.green);
                Debug.DrawLine(anchorCenter, anchorCenter + y * -1000000000000.0f, Color.green);

                Debug.DrawLine(anchorCenter, anchorCenter + z * 1000000000000.0f, Color.blue);
                Debug.DrawLine(anchorCenter, anchorCenter + z * -1000000000000.0f, Color.blue);
            }

            if (Event.current.type == EventType.ExecuteCommand)
            {
                // For some reason this was included to block the usage of the F key. I'm not sure why as it just gets in the way now
                /*
                if (Event.current.commandName == "FrameSelected")
                {
                    Debug.Log("Frame Selected!");
                   // Event.current.commandName = "";
                   // Event.current.Use();
                }*/
            }
            if (Event.current.type == EventType.KeyDown)
            {
                bool areAnyVertsSelected = false;

                List<int> verticesSelected = new List<int>();

                bool areAnyFacesSelected = false;

                for (int i = 0; i < solid.selectedFaces.Length; i++)
                {
                    if (solid.selectedFaces[i])
                    {
                        areAnyVertsSelected = true;
                        areAnyFacesSelected = true;
                        break;
                    }
                }

                for (int i = 0; i < solid.selectedVerts.Length; i++)
                {
                    if (solid.selectedVerts[i])
                    {
                        areAnyVertsSelected = true;

                        bool isDuplicateVert = false;

                        // Check if the vert already belongs to the selected list
                        for (int ii = 0; ii < verticesSelected.Count; ii++)
                        {
                            if (solid.connectedVerts[verticesSelected[ii]].Contains(i))
                            {
                                isDuplicateVert = true;
                                break;
                            }
                        }
                        if (!isDuplicateVert) { verticesSelected.Add(i); }
                    }
                }

                #region LoopCut
                // Loop Cut
                if (solid.editorInfo.transformMode == 0)
                {
                    if (Event.current.keyCode == KeyCode.R && shift)
                    {
                        if (solid.editorInfo.editOperation == MeshEdit.EditorInfo.MeshEditOperation.LoopCut)
                        {
                            solid.editorInfo.editOperation = MeshEdit.EditorInfo.MeshEditOperation.Standard;
                        }
                        else
                        {
                            solid.editorInfo.editOperation = MeshEdit.EditorInfo.MeshEditOperation.LoopCut;
                        }
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                }
                #endregion

                if (areAnyVertsSelected)
                {
                    if (solid.editorInfo.transformMode == 0)
                    {
                        #region Delete
                        if (Event.current.keyCode == KeyCode.Delete)
                        {
                            Undo.RegisterCompleteObjectUndo(solid, "Face Delete");

                            solid.selectionConvertToFaces();

                            if (solid.vertMode == 0 && !solid.selectedFaces.Contains<bool>(true))
                            {
                                bool canDissolve = solid.dissolve(solid.selectedVerts, solid.selectedFaces);

                                if (!canDissolve)
                                {
                                    solid.delete(solid.selectedVerts, solid.selectedFaces);
                                }
                            }
                            else
                            {

                                solid.delete(solid.selectedVerts, solid.selectedFaces);

                                // GUIUtility.hotControl = controlId;
                            }

                            solid.selectedVerts = new bool[solid.verts.Count];
                            solid.selectedFaces = new bool[solid.quads.Count / 4];
                            Event.current.Use();
                        }
                        #endregion

                        #region  Create Face / Flip Face
                        if (Event.current.keyCode == KeyCode.F)
                        {
                            bool flip = false;
                            if (verticesSelected.Count == 4 ||
                                verticesSelected.Count == 3 ||
                                verticesSelected.Count == 2)
                            {
                                // TODO: Check to make sure no faces are selected
                                solid.selectionConvertToFaces();
                                bool freeFace = true;
                                for (int i = 0; i < solid.selectedFaces.Length; i++)
                                {
                                    if (solid.selectedFaces[i])
                                    {
                                        freeFace = false;
                                        break;
                                    }
                                }

                                if (freeFace)
                                {
                                    Undo.RegisterCompleteObjectUndo(solid, "Mesh Face Create");

                                    List<int> vertsToSelect = new List<int>();

                                    if (verticesSelected.Count == 4)
                                    {
                                        solid.addQuadBetweenFaces(
                                            verticesSelected[0],
                                            verticesSelected[1],
                                            verticesSelected[2],
                                            verticesSelected[3]);
                                    }
                                    else if (verticesSelected.Count == 3)
                                    {
                                        solid.addQuadBetweenFaces(
                                            verticesSelected[0],
                                            verticesSelected[1],
                                            verticesSelected[2]);
                                    }
                                    else
                                    {
                                        clearSelected(solid);
                                        vertsToSelect = solid.addQuadBetweenFaces(
                                            verticesSelected[0],
                                            verticesSelected[1]);
                                    }

                                    solid.cleanVertConnections(solid.verts.Count - 1);
                                    solid.cleanVertConnections(solid.verts.Count - 2);
                                    solid.cleanVertConnections(solid.verts.Count - 3);
                                    solid.cleanVertConnections(solid.verts.Count - 4);

                                    solid.rebuildAdjacentFaces(0);

                                    solid.pushNewGeometry();

                                    updateSelectionArray(solid, solid.verts.Count);

                                    if (vertsToSelect != null)
                                    {
                                        for (int i = 0; i < vertsToSelect.Count; i++)
                                        {
                                            setSelectVert(solid, vertsToSelect[i], true);
                                        }
                                    }

                                    Event.current.Use();
                                }
                                else
                                {
                                    flip = true;
                                    //Debug.Log("4 vertices and 0 faces must be selected to create a new face");
                                }

                            }
                            else
                            {
                                flip = true;
                            }

                            if (flip)
                            {
                                if (areAnyFacesSelected)
                                {
                                    // Error: Undo only works 
                                    Undo.RecordObject(solid, "Flip Face");
                                    solid.selectionConvertToFaces();
                                    solid.flipFaces(solid.selectedFaces);

                                    Event.current.Use();
                                }
                            }


                        }
                        #endregion

                        #region Flip saddling
                        if (Event.current.keyCode == KeyCode.D)
                        {
                            if (areAnyFacesSelected)
                            {
                                Undo.RecordObject(solid, "Flip Saddling");
                                solid.selectionConvertToFaces();
                                solid.flipSaddling(solid.selectedFaces);

                                GUIUtility.hotControl = controlId;
                                Event.current.Use();
                            }
                        }
                        #endregion

                        #region Extrude
                        if (Event.current.keyCode == KeyCode.E && solid.editorInfo.transformMode == 0)
                        {
                            // 1. Extrude function runs on selected object
                            Undo.RegisterCompleteObjectUndo(solid, "Extrusion");
                            /*

                            Undo.RecordObject(solid, "Mesh Extrusion step");
                            Undo.RecordObject(solid.gameObject, "Mesh Extrusion step");*/
                            if (solid.vertMode == 1)
                            {
                                solid.selectionConvertToVerts();
                            }
                            solid.selectionConvertToFaces();
                            selectionAddTouchingVerts(solid);

                            bool[] selectedVertsTemp = new bool[solid.selectedVerts.Length];
                            bool[] selectedFacesTemp = new bool[solid.selectedFaces.Length];
                            for (int i = 0; i < solid.selectedVerts.Length; i++)
                            {
                                selectedVertsTemp[i] = solid.selectedVerts[i];
                            }
                            for (int i = 0; i < solid.selectedFaces.Length; i++)
                            {
                                selectedFacesTemp[i] = solid.selectedFaces[i];
                            }

                            Vector3 n = Vector3.zero;
                            float nCount = 0;
                            for (int i = 0; i < solid.selectedFaces.Length; i++)
                            {

                                if (solid.selectedFaces[i])
                                {
                                    n += solid.faceNormals[i];
                                    nCount++;
                                }
                            }

                            transformDimensionsExtrude = true;
                            transformDimensions = n.normalized;

                            if (transformDimensions.sqrMagnitude == 0)
                            {
                                transformDimensionsExtrude = false;
                            }

                            // TODO: If no faces, only verts are selected, extrude based on direction of corner verts

                            // Extrude
                            List<int> faceRefs = new List<int>();
                            List<int> selectedEdges = getSelectedPerimeter(solid, solid.selectedVerts, solid.selectedFaces, out faceRefs, solid.vertMode);

                            solid.selectedVerts = solid.extrude(selectedEdges, faceRefs, solid.selectedVerts, solid.selectedFaces, transformDimensions);

                            updateSelectionArray(solid, solid.selectedVerts.Length);

                            // 2. Interface switches to Movement, of type "Movement along an axis 'a'"

                            activateTransformMode(solid, 1);

                            // Add the original verts to the list of verts that should have their normals updated

                            for (int i = 0; i < affectedVerts.Count; i++)
                            {
                                if (affectedVerts[i] < selectedVertsTemp.Length)
                                {
                                    selectedVertsTemp[affectedVerts[i]] = false;
                                }
                            }
                            for (int i = 0; i < affectedFaces.Count; i++)
                            {
                                if (affectedFaces[i] < selectedFacesTemp.Length)
                                {
                                    selectedFacesTemp[affectedFaces[i]] = false;
                                }
                            }

                            for (int i = 0; i < selectedVertsTemp.Length; i++)
                            {
                                if (selectedVertsTemp[i])
                                {
                                    affectedVerts.Add(i);
                                }
                            }

                            for (int i = 0; i < selectedFacesTemp.Length; i++)
                            {
                                if (selectedFacesTemp[i])
                                {
                                    affectedFaces.Add(i);
                                }
                            }


                            moveAnchor = constrainToScreenSize(mousePos);

                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }

                        #endregion

                        #region Unwrap
                        if (solid.editorInfo.transformMode == 0)
                        {
                            if (Event.current.keyCode == KeyCode.U &&
                            Event.current.modifiers == EventModifiers.None)
                            {
                                if (solid.vertMode == 0)
                                {
                                    solid.selectionConvertToFaces();
                                }
                                else
                                {
                                    solid.selectionConvertToVerts();
                                }

                                solid.lscmUnwrap(solid.selectedVerts, solid.selectedFaces);
                            }
                        }
                        #endregion
                    }

                    #region Activate/Control transforms
                    if (Event.current.keyCode == KeyCode.G)
                    {
                        if (transformDimensionsExtrude)
                        {
                            transformDimensions = Vector3.one;
                            transformDimensionsExtrude = false;
                        }
                        if (solid.editorInfo.transformMode != 1)
                        {
                            if (solid.vertMode == 1)
                            {
                                solid.selectionConvertToVerts();
                            }
                            solid.selectionConvertToFaces();
                            selectionAddTouchingVerts(solid);

                            activateTransformMode(solid, 1);
                        }
                        else if (solid.editorInfo.transformMode == 1)
                        {
                            solid.editorInfo.transformMode = 0;
                        }

                        moveAnchor = constrainToScreenSize(mousePos);
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }


                    if (Event.current.type == EventType.KeyDown &&
                        Event.current.keyCode == KeyCode.R)
                    {
                        if (transformDimensionsExtrude)
                        {
                            transformDimensions = Vector3.one;
                            transformDimensionsExtrude = false;
                        }
                        if (solid.editorInfo.transformMode != 2)
                        {
                            if (solid.vertMode == 1)
                            {
                                solid.selectionConvertToVerts();
                            }
                            solid.selectionConvertToFaces();
                            selectionAddTouchingVerts(solid);

                            activateTransformMode(solid, 2);
                        }
                        else if (solid.editorInfo.transformMode == 2)
                        {
                            solid.editorInfo.transformMode = 0;
                        }
                        moveAnchor = constrainToScreenSize(mousePos);
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }

                    if (Event.current.keyCode == KeyCode.S)
                    {
                        if (transformDimensionsExtrude)
                        {
                            transformDimensions = Vector3.one;
                            transformDimensionsExtrude = false;
                        }
                        if (solid.editorInfo.transformMode != 3)
                        {
                            if (solid.vertMode == 1)
                            {
                                solid.selectionConvertToVerts();
                            }
                            solid.selectionConvertToFaces();
                            selectionAddTouchingVerts(solid);

                            activateTransformMode(solid, 3);
                        }
                        else if (solid.editorInfo.transformMode == 3)
                        {
                            solid.editorInfo.transformMode = 0;
                        }
                        moveAnchor = constrainToScreenSize(mousePos);
                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                    if (solid.editorInfo.transformMode > 0)
                    {
                        Vector3 oldTransformDimensions = transformDimensions;
                        bool dimensionsChanged = false;

                        if (Event.current.keyCode == KeyCode.X)
                        {
                            if (shift && solid.editorInfo.transformMode != 2)
                            {
                                transformDimensions = new Vector3(0.0f, 1.0f, 1.0f);
                                transformDimensionsPlanar = true;
                            }
                            else
                            {
                                transformDimensions = new Vector3(1.0f, 0.0f, 0.0f);
                                transformDimensionsPlanar = false;
                            }


                            dimensionsChanged = true;
                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        if (Event.current.keyCode == KeyCode.Z)
                        {
                            if (shift && solid.editorInfo.transformMode != 2)
                            {
                                transformDimensions = new Vector3(1.0f, 0.0f, 1.0f);
                                transformDimensionsPlanar = true;
                            }
                            else
                            {
                                transformDimensions = new Vector3(0.0f, 1.0f, 0.0f);
                                transformDimensionsPlanar = false;
                            }

                            dimensionsChanged = true;
                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }
                        if (Event.current.keyCode == KeyCode.Y)
                        {
                            if (shift && solid.editorInfo.transformMode != 2)
                            {
                                transformDimensions = new Vector3(1.0f, 1.0f, 0.0f);
                                transformDimensionsPlanar = true;
                            }
                            else
                            {
                                transformDimensions = new Vector3(0.0f, 0.0f, 1.0f);
                                transformDimensionsPlanar = false;
                            }

                            dimensionsChanged = true;
                            GUIUtility.hotControl = controlId;
                            Event.current.Use();
                        }

                        if (dimensionsChanged &&
                            oldTransformDimensions == transformDimensions)
                        {
                            transformDimensions = Vector3.one;
                            transformDimensionsPlanar = false;
                        }
                    }
                    #endregion
                }
                #region Selection Functions

                if (solid.editorInfo.transformMode == 0)
                
                {
                    if (Event.current.keyCode == KeyCode.C && Event.current.modifiers == EventModifiers.None)
                    {
                        if (solid.editorInfo.editOperation != MeshEdit.EditorInfo.MeshEditOperation.SelectCircle)
                        {
                            Undo.RegisterCompleteObjectUndo(solid, "Activate circle selection");
                            solid.editorInfo.editOperation = MeshEdit.EditorInfo.MeshEditOperation.SelectCircle;
                        }
                        else
                        {
                            solid.editorInfo.editOperation = MeshEdit.EditorInfo.MeshEditOperation.Standard;
                        }

                        GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.A && Event.current.modifiers == EventModifiers.None)
                    {
                        Undo.RegisterCompleteObjectUndo(solid, "Select all verts");

                        bool isFull = true;
                        if (solid.vertMode == 1)
                        {
                            for (int i = 0; i < solid.selectedVerts.Length; i++)
                            {
                                if (!solid.selectedVerts[i])
                                {
                                    isFull = false;
                                    break;
                                }
                            }
                        }
                        else if (solid.vertMode == 0)
                        {
                            for (int i = 0; i < solid.selectedFaces.Length; i++)
                            {
                                if (!solid.selectedFaces[i])
                                {
                                    isFull = false;
                                    break;
                                }
                            }
                        }

                        for (int i = 0; i < solid.selectedVerts.Length; i++)
                        {
                            solid.selectedVerts[i] = !isFull;
                        }
                        for (int i = 0; i < solid.selectedFaces.Length; i++)
                        {
                            solid.selectedFaces[i] = !isFull;
                        }
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.L)
                    {
                        Vector3 origin = SceneView.lastActiveSceneView.camera.transform.position;
                        Vector3 colPoint = Vector3.zero;
                        float d = float.MaxValue;
                        int closestFace = -1;

                        solid.beginTimer("Find nearest face to mouse");
                        ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                        for (int i = 0; i < solid.tris.Count; i += 3)
                        {
                            t = new MeshEdit.Triangle(
                                solid.verts[solid.tris[i + 0]],
                                solid.verts[solid.tris[i + 1]],
                                solid.verts[solid.tris[i + 2]]);

                            if (rayIntersectsTriangle(ray.origin, ray.direction, t, ref colPoint))
                            {
                                float dd = (colPoint - origin).sqrMagnitude;

                                if (dd < d)
                                {
                                    d = dd;
                                    closestFace = i / 6;
                                }
                            }
                        }
                        solid.endTimer();

                        solid.beginTimer("Select all connected verts and faces ");


                        Undo.RegisterCompleteObjectUndo(solid, "Select island");

                        if (!shift)
                        {
                            clearSelected(solid);
                        }

                        if (closestFace >= 0)
                        {
                            solid.selectedFaces[closestFace] = true;
                            selectAllAdjacentFaces(solid, closestFace);
                        }

                        solid.selectionConvertToVerts();

                        //GUIUtility.hotControl = controlId;
                        Event.current.Use();
                    }
                }
                #endregion


                // Use the default delete key on any other mode to prevent the model from being accidentally deleted
                if (Event.current.keyCode == KeyCode.Delete && solid.editMode != 0)
                {
                    Event.current.Use();
                }
            }



            #region Copy/Paste
            Event e = Event.current;
            if (e.type == EventType.ValidateCommand && e.commandName == "Copy")
            {
                e.Use(); // without this line we won't get ExecuteCommand
            }

            if (e.type == EventType.ExecuteCommand && e.commandName == "Copy")
            {
                copy(solid);
                e.Use();
            }

            if (e.type == EventType.ValidateCommand && e.commandName == "Cut")
            {
                e.Use(); // without this line we won't get ExecuteCommand
            }

            if (e.type == EventType.ExecuteCommand && e.commandName == "Cut")
            {
                cut(solid);
                e.Use();
            }

            if (e.type == EventType.ValidateCommand && e.commandName == "Paste")
            {
                e.Use(); // without this line we won't get ExecuteCommand
            }

            if (e.type == EventType.ExecuteCommand && e.commandName == "Paste")
            {
                paste(solid);
                e.Use();
            }

            #endregion



            if (Event.current.type == EventType.MouseDown)
            {
                //Debug.Log("MP: " + mousePos.ToString("f4"));
                if (Event.current.button == 1)
                {
                    //Tools.viewTool = ViewTool.FPS;
                    //Tools.current = Tool.View;
                    // GUIUtility.hotControl = controlId;
                    // Event.current.Use();
                }
            }

            #endregion

            //Event.current.modifiers == EventModifiers.Alt;
            //Event.current.modifiers = EventModifiers.None;
            //Tools.current = Tool.Move;
        }

    }

    private void cut(MeshEdit solid)
    {
        for (int i = 0; i < solid.selectedFaces.Length; i++)
        {
            if (solid.selectedFaces[i])
            {
                solid.beginTimer("Copy faces for cut");
                Undo.RegisterCompleteObjectUndo(solid, "Cut faces");
                solid.selectionConvertToFaces();
                EditorGUIUtility.systemCopyBuffer = solid.getCopyString(solid.selectedFaces);
                solid.endTimer();

                solid.selectedVerts = new bool[solid.selectedVerts.Length];
                solid.selectionConvertToVerts();
                solid.delete(solid.selectedVerts, solid.selectedFaces);
                solid.selectedVerts = new bool[solid.verts.Count];
                solid.selectedFaces = new bool[solid.quads.Count / 4];
            }
        }
    }

    private int breakCount = 0;
    private void selectAllAdjacentFaces(MeshEdit solid, int face)
    {
        int adjacentFace = -1;

        breakCount++;
        if (breakCount > 10000)
        {
            return;
        }

        for (int i = 0; i < solid.adjacentFaces[face].list.Count; i++)
        {
            adjacentFace = solid.adjacentFaces[face].list[i];

            if (adjacentFace >= 0 &&
                !solid.selectedFaces[adjacentFace] &&
                !solid.halfEdges[face].edges[i].isSeam)
            {
                solid.selectedFaces[adjacentFace] = true;
                selectAllAdjacentFaces(solid, adjacentFace);
            }
        }
    }

    private void copy(MeshEdit solid)
    {
        solid.beginTimer("Copy faces");
        Undo.RegisterCompleteObjectUndo(solid, "Copy faces");
        solid.selectionConvertToFaces();
        for (int i = 0; i < solid.selectedFaces.Length; i++)
        {
            if (solid.selectedFaces[i])
            {
                EditorGUIUtility.systemCopyBuffer = solid.getCopyString(solid.selectedFaces);
                break;
            }
        }
        solid.endTimer();
    }

    private void paste(MeshEdit solid)
    {
        solid.beginTimer("Pasting faces");

        Undo.RegisterCompleteObjectUndo(solid, "Paste faces");
        int vertCount = solid.verts.Count;
        int faceCount = solid.quads.Count / 4;
        if (!solid.pasteData(EditorGUIUtility.systemCopyBuffer))
        {
            Debug.Log("Copied data is not formatted correctly. Cannot paste data from clipboard.");
        }
        else
        {
            solid.selectedFaces = new bool[solid.quads.Count / 4];
            solid.selectedVerts = new bool[solid.verts.Count];

            for (int i = faceCount; i < solid.quads.Count / 4; i++)
            {
                solid.selectedFaces[i] = true;
            }
            for (int i = vertCount; i < solid.verts.Count; i++)
            {
                solid.selectedVerts[i] = true;
            }
        }


        solid.endTimer();
    }

    private void loopCut(MeshEdit solid)
    {

        // Add new quads
        List<int> quadsCutAsIndices = new List<int>();
        List<List<Vector2>> facesCutAsUVs = new List<List<Vector2>>();
        List<List<Vector2>> newUvs = new List<List<Vector2>>();

        if (solid.uvMaps != null && solid.uvMaps.Count > 0)
        {
            for (int u = 0; u < solid.uvMaps.Count; u++)
            {
                newUvs.Add(new List<Vector2>());
                facesCutAsUVs.Add(new List<Vector2>());
            }
        }

        for (int i = 0; i < facesCut.Count; i++)
        {
            // These get overwritten in the next loop
            quadsCutAsIndices.Add(0);
            quadsCutAsIndices.Add(0);
            quadsCutAsIndices.Add(0);
            quadsCutAsIndices.Add(0);
            if (solid.uvMaps != null && solid.uvMaps.Count > 0)
            {
                for (int u = 0; u < solid.uvMaps.Count; u++)
                {
                    facesCutAsUVs[u].Add(Vector2.zero);
                    facesCutAsUVs[u].Add(Vector2.zero);
                    facesCutAsUVs[u].Add(Vector2.zero);
                    facesCutAsUVs[u].Add(Vector2.zero);
                }
            }

            int parityCount = 0;
            for (int ii = 0; ii < solid.verts.Count; ii++)
            {
                int faceIndex = solid.quads.IndexOf(ii);
                int face = faceIndex / 4;

                if (facesCut[i] == face)
                {
                    for (int iii = 0; iii < 4; iii++)
                    {
                        if (ii == cutsAB[i * 4 + iii])// solid.verts[solid.quads[facesCut[i] * 4 + iii]])
                        {

                            quadsCutAsIndices[i * 4 + iii] = ii;

                            parityCount++;


                            if (solid.uvMaps != null && solid.uvMaps.Count > 0)
                            {
                                for (int u = 0; u < solid.uvMaps.Count; u++)
                                {
                                    facesCutAsUVs[u][i * 4 + iii] = solid.uvMaps[u].uvs[ii];
                                }
                            }
                        }
                    }
                }
            }

            if (parityCount != 4)
            {
                Debug.Log("Parity Error on loop cut!");
                // return;
            }
        }
        int faceLoops = Mathf.FloorToInt(cutCount) + 1;
        bool cutUVsWithoutReapplying = true;

        int newFacesToBeCreated = facesCut.Count * faceLoops;
        int newVertsToBeCreated = newFacesToBeCreated * 4;

        int vertsBeforeAddingNewFaces = solid.verts.Count;
        int facesBeforeAddingNewFaces = solid.faceNormals.Count;

        for (int i = 0; i < facesCut.Count; i++)
        {
            float sep = 1.0f / (faceLoops);
            for (int ii = 0; ii < faceLoops; ii++)
            {
                #region Diagram
                /*
                 *   ------------> ii
                 *  | ..._____ __________________ __________________ ______...
                 *  |  / / / | |a"     [0]     b" |                | |/ / / /
                 *  |   / / /| |                | |                | | / / / 
                 *  |  / / / | |    i=0         | |  i=0           | |/ / / /
                 *  |   / / /| [3]  ii=0       [1]|  ii=1          | | / / /   <--facesCut 0
                 * \/  / / / | |                | |                | |/ / / /
                 *  i   / / /| |                | |                | | / / / 
                 *    ...____| |c"_____[2]_____d" |________________| |_____...
                 *    ..._____ __________________ __________________ ______...  <-Pre-existing edge
                 *     / / / | |a"             b" |                | |/ / / /
                 *      / / /| |                | |                | | / / / 
                 *     / / / | |                | |                | |/ / / /
                 *      / / /| |                | |                | | / / /   <--facesCut 1
                 *     / / / | |                | |                | |/ / / /
                 *      / / /| |                | |                | | / / / 
                 *    ...____| |c"_____________d" |________________| |_____...
                 *         ^           ^         ^         ^        ^
                 *         |           |         |         |        |
                 *     ratio = 0.0     |     ratio = 0.5   |    ratio = 1.0
                 *                     |         |         |
                 *                 faceLoop 0    |     faceLoop 1
                 *                               |
                 *                          A single cut
                 */
                #endregion

                float ratio = sep * ii;

                int currentNewFace = (ii + faceLoops * i);

                //Debug.Log("FC:" + "{ " + cutsAdjacentFaces[i][0] + ", " + cutsAdjacentFaces[i][1] + ", " + cutsAdjacentFaces[i][2] + ", " + cutsAdjacentFaces[i][3] + "}");

                #region Create new list of adjacent faces
                List<int>[] newConnectedVerts = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };

                bool addLeftSideVerts = true;
                bool addRightSideVerts = true;

                if (ii == 0)
                {
                    addLeftSideVerts = false;
                    // Adjacent Edge/Face
                    int face = solid.getEdgeIndexFromFaceVerts(facesCut[i], cutsAB[i * 4 + 0], cutsAB[i * 4 + 2]);

                    if (face != -1)
                    {
                        face = solid.adjacentFaces[facesCut[i]].list[face];

                        if (face != -1)
                        {
                            int reverseFaceEdge = solid.adjacentFaces[face].list.IndexOf(facesCut[i]);
                            solid.adjacentFaces[face].list[reverseFaceEdge] = facesBeforeAddingNewFaces + currentNewFace;
                        }
                    }
                    else
                    {
                        Debug.Log("Error in loop cut. Faces not aligned at edge.");
                    }
                    // adjacentFaces.list[3] = face;

                }
                else if (ii == faceLoops - 1)
                {
                    addRightSideVerts = false;
                    int face = solid.getEdgeIndexFromFaceVerts(facesCut[i], cutsAB[i * 4 + 1], cutsAB[i * 4 + 3]);
                    if (face != -1)
                    {
                        face = solid.adjacentFaces[facesCut[i]].list[face];

                        if (face != -1)
                        {
                            int reverseFaceEdge = solid.adjacentFaces[face].list.IndexOf(facesCut[i]);
                            solid.adjacentFaces[face].list[reverseFaceEdge] = facesBeforeAddingNewFaces + currentNewFace;
                        }
                    }
                    else
                    {
                        Debug.Log("Error in loop cut. Faces not aligned at edge.");
                    }

                    //  adjacentFaces.list[1] = face;
                }

                #endregion
                #region Create new list of connected verts

                int upDirection = newVertsToBeCreated - faceLoops;
                int downDirection = faceLoops;

                bool isDownBlocked = i == 0 && cutsAdjacentFaces[i][2] == -1;
                bool isUpBlocked = i == facesCut.Count - 1 && cutsAdjacentFaces[i][0] == -1;

                if (!isDownBlocked)
                {
                    newConnectedVerts[2].Add(vertsBeforeAddingNewFaces + (((currentNewFace + upDirection) * 4 + 0) % newVertsToBeCreated)); // Bottom
                }
                if (!isUpBlocked)
                {
                    newConnectedVerts[0].Add(vertsBeforeAddingNewFaces + (((currentNewFace + downDirection) * 4 + 2) % newVertsToBeCreated)); // Top
                }
                if (addLeftSideVerts)
                {
                    // Top Left vert
                    newConnectedVerts[0].Add(vertsBeforeAddingNewFaces + (((currentNewFace - 1) * 4 + 1) % newVertsToBeCreated)); // Left
                    if (!isUpBlocked) { newConnectedVerts[0].Add(vertsBeforeAddingNewFaces + (((currentNewFace + downDirection - 1) * 4 + 3) % newVertsToBeCreated)); }// Top-Left

                    // Bottom Left vert
                    newConnectedVerts[2].Add(vertsBeforeAddingNewFaces + (((currentNewFace - 1) * 4 + 3) % newVertsToBeCreated)); // Left
                    if (!isDownBlocked) { newConnectedVerts[2].Add(vertsBeforeAddingNewFaces + (((currentNewFace + upDirection - 1) * 4 + 1) % newVertsToBeCreated)); } // Bottom-Left

                }
                else
                {
                    // Connected Verts, A and C If they lie against the "rato=0.0" side quad
                    for (int repeat = 0; repeat < 2; repeat++)
                    {
                        for (int j = 0; j < solid.connectedVerts[cutsAB[i * 4 + repeat * 2]].Count; j++)
                        {
                            // Check if the vert is part of a face that will be deleted at the end of this method
                            // If it is, set it to the matching vert of the new face
                            // Same algorithm as face adjacency, but multiplied by four, + whatever index the vert should be
                            // Do not assume that the connected vert will be whatever comes immedately after in the list of facesCut
                            // Instead, calculate it based on the index of the face in the facesCutList, which will get a definite index for where the new vert will be
                            int newVert = solid.connectedVerts[cutsAB[i * 4 + repeat * 2]].list[j];
                            int newVertFace = solid.getFaceFromVert(newVert);
                            int indexOfNewVertFaceInCutsAB = facesCut.IndexOf(newVertFace);
                            if (indexOfNewVertFaceInCutsAB == -1)
                            {
                                if (newVert < vertsBeforeAddingNewFaces)
                                {
                                    newConnectedVerts[repeat * 2].Add(newVert);
                                    solid.connectedVerts[newVert].Add(vertsBeforeAddingNewFaces + currentNewFace * 4 + repeat * 2);
                                }
                            }
                        }
                    }
                }

                if (!isDownBlocked)
                {
                    newConnectedVerts[3].Add(vertsBeforeAddingNewFaces + (((currentNewFace + upDirection) * 4 + 1) % newVertsToBeCreated)); // Bottom
                }
                if (!isUpBlocked)
                {
                    newConnectedVerts[1].Add(vertsBeforeAddingNewFaces + (((currentNewFace + downDirection) * 4 + 3) % newVertsToBeCreated)); // Top
                }
                if (addRightSideVerts)
                {
                    // Top Right vert
                    newConnectedVerts[1].Add(vertsBeforeAddingNewFaces + (((currentNewFace + 1) * 4 + 0) % newVertsToBeCreated)); // Right
                    if (!isUpBlocked) { newConnectedVerts[1].Add(vertsBeforeAddingNewFaces + (((currentNewFace + downDirection + 1) * 4 + 2) % newVertsToBeCreated)); }// Top-Right 

                    // Bottom Right vert
                    newConnectedVerts[3].Add(vertsBeforeAddingNewFaces + (((currentNewFace + 1) * 4 + 2) % newVertsToBeCreated)); // Right
                    if (!isDownBlocked) { newConnectedVerts[3].Add(vertsBeforeAddingNewFaces + (((currentNewFace + upDirection + 1) * 4 + 0) % newVertsToBeCreated)); }// Bottom-Right
                }
                else
                {
                    // Connected Verts, B and D If they lie against the "rato=0.0" side quad
                    for (int repeat = 0; repeat < 2; repeat++)
                    {
                        for (int j = 0; j < solid.connectedVerts[cutsAB[i * 4 + repeat * 2 + 1]].Count; j++)
                        {
                            // Check if the vert is part of a face that will be deleted at the end of this method
                            // If it is, set it to the matching vert of the new face
                            // Same algorithm as face adjacency, but multiplied by four, + whatever index the vert should be
                            // Do not assume that the connected vert will be whatever comes immedately after in the list of facesCut
                            // Instead, calculate it based on the index of the face in the facesCutList, which will get a definite index for where the new vert will be
                            int newVert = solid.connectedVerts[cutsAB[i * 4 + repeat * 2 + 1]].list[j];
                            int newVertFace = solid.getFaceFromVert(newVert);
                            int indexOfNewVertFaceInCutsAB = facesCut.IndexOf(newVertFace);
                            if (indexOfNewVertFaceInCutsAB == -1)
                            {
                                if (newVert < vertsBeforeAddingNewFaces)
                                {
                                    newConnectedVerts[repeat * 2 + 1].Add(newVert);
                                    solid.connectedVerts[newVert].Add(vertsBeforeAddingNewFaces + currentNewFace * 4 + repeat * 2 + 1);
                                }
                            }
                        }
                    }
                }

                for (int j = 0; j < 4; j++)
                {
                    solid.connectedVerts.Add(new MeshEdit.ListWrapper(newConnectedVerts[j]));
                }
                #endregion

                Vector3 v0 = solid.verts[cutsAB[i * 4 + 0]] + (solid.verts[cutsAB[i * 4 + 1]] - solid.verts[cutsAB[i * 4 + 0]]) * ratio;
                Vector3 v1 = solid.verts[cutsAB[i * 4 + 0]] + (solid.verts[cutsAB[i * 4 + 1]] - solid.verts[cutsAB[i * 4 + 0]]) * (ratio + sep);
                Vector3 v2 = solid.verts[cutsAB[i * 4 + 2]] + (solid.verts[cutsAB[i * 4 + 3]] - solid.verts[cutsAB[i * 4 + 2]]) * ratio;
                Vector3 v3 = solid.verts[cutsAB[i * 4 + 2]] + (solid.verts[cutsAB[i * 4 + 3]] - solid.verts[cutsAB[i * 4 + 2]]) * (ratio + sep);
                Vector3 n = solid.faceNormals[facesCut[i]];

                #region AddQuad

                int vertCount = solid.verts.Count;

                solid.verts.Add(v0);
                solid.verts.Add(v1);
                solid.verts.Add(v2);
                solid.verts.Add(v3);

                solid.colours.Add(Color.white);
                solid.colours.Add(Color.white);
                solid.colours.Add(Color.white);
                solid.colours.Add(Color.white);

                solid.vertNormals.Add(n);
                solid.vertNormals.Add(n);
                solid.vertNormals.Add(n);
                solid.vertNormals.Add(n);
                solid.meshNormals.Add(n);
                solid.meshNormals.Add(n);
                solid.meshNormals.Add(n);
                solid.meshNormals.Add(n);

                solid.customTextureUVMap.Add(Vector2.zero);
                solid.customTextureUVMap.Add(Vector2.zero);
                solid.customTextureUVMap.Add(Vector2.zero);
                solid.customTextureUVMap.Add(Vector2.zero);

                solid.faceNormals.Add(n);



                MeshEdit.ListWrapper adjacentFaces = new MeshEdit.ListWrapper();

                // If the cut loops back on itself
                adjacentFaces = new MeshEdit.ListWrapper(new int[] {
                    facesBeforeAddingNewFaces + (ii + faceLoops * (i + 1)) % newFacesToBeCreated,
                    facesBeforeAddingNewFaces + (ii + faceLoops * i + 1),
                    facesBeforeAddingNewFaces + (ii + faceLoops * (i + (facesCut.Count - 1))) % newFacesToBeCreated,
                    facesBeforeAddingNewFaces + (ii + faceLoops * i - 1) });

                if (i == 0 && cutsAdjacentFaces[i][2] == -1)
                {
                    adjacentFaces.list[2] = -1;
                }
                if (i == facesCut.Count - 1 && cutsAdjacentFaces[i][0] == -1)
                {
                    adjacentFaces.list[0] = -1;
                }
                if (ii == 0)
                {
                    adjacentFaces.list[3] = cutsAdjacentFaces[i][3];
                }
                if (ii == faceLoops - 1)
                {
                    adjacentFaces.list[1] = cutsAdjacentFaces[i][1];
                }

                // Test the normals, if they don't face outward, flip the triangles. (0, 3 are the verts grabbed)
                Vector3 cross = Vector3.Cross(v1 - v0, v2 - v0);
                if (Vector3.Dot(cross, n) < 0)
                {
                    solid.tris.Add(vertCount + 0); // a (v1)
                    solid.tris.Add(vertCount + 2); // b (v0)
                    solid.tris.Add(vertCount + 1); // c (v2)

                    solid.tris.Add(vertCount + 3); // a (v1)
                    solid.tris.Add(vertCount + 1); // b
                    solid.tris.Add(vertCount + 2); // c

                    solid.quads.Add(vertCount + 0);
                    solid.quads.Add(vertCount + 2);
                    solid.quads.Add(vertCount + 1);
                    solid.quads.Add(vertCount + 3);

                    int tempAL0 = adjacentFaces.list[0];
                    int tempAL1 = adjacentFaces.list[1];
                    adjacentFaces.list[0] = adjacentFaces.list[3];
                    adjacentFaces.list[1] = adjacentFaces.list[2];
                    adjacentFaces.list[2] = tempAL1;
                    adjacentFaces.list[3] = tempAL0;
                }
                else
                {
                    solid.tris.Add(vertCount + 0); // a (v1)
                    solid.tris.Add(vertCount + 1); // b (v0)
                    solid.tris.Add(vertCount + 2); // c (v2)
                    solid.tris.Add(vertCount + 3); // a (v1)
                    solid.tris.Add(vertCount + 2); // b
                    solid.tris.Add(vertCount + 1); // c

                    solid.quads.Add(vertCount + 0);
                    solid.quads.Add(vertCount + 1);
                    solid.quads.Add(vertCount + 2);
                    solid.quads.Add(vertCount + 3);


                }


                // Resize all data structures representing this mesh to fit the new mesh size.
                for (int uvi = 0; uvi < solid.uvMaps.Count; uvi++)
                {
                    solid.uvMaps[uvi].resizeUVLength(vertCount + 4);//, newVerts);
                }

                solid.adjacentFaces.Add(adjacentFaces);
                solid.halfEdges.Add(new MeshEdit.MeshEditFaceEdges());
                solid.quadMaterial.Add(solid.quadMaterial[i]);

                #endregion

                int c = solid.colours.Count - 4;


                float ratioNearSide = (ii + 0.0f) / (faceLoops);
                float ratioFarSide = (ii + 1.0f) / (faceLoops);

                solid.colours[c + 0] = Color.Lerp(solid.colours[quadsCutAsIndices[i * 4 + 0]], solid.colours[quadsCutAsIndices[i * 4 + 1]], ratioNearSide);
                solid.colours[c + 2] = Color.Lerp(solid.colours[quadsCutAsIndices[i * 4 + 2]], solid.colours[quadsCutAsIndices[i * 4 + 3]], ratioNearSide);
                solid.colours[c + 1] = Color.Lerp(solid.colours[quadsCutAsIndices[i * 4 + 0]], solid.colours[quadsCutAsIndices[i * 4 + 1]], ratioFarSide);
                solid.colours[c + 3] = Color.Lerp(solid.colours[quadsCutAsIndices[i * 4 + 2]], solid.colours[quadsCutAsIndices[i * 4 + 3]], ratioFarSide);


                if (solid.uvMaps != null && solid.uvMaps.Count > 0)
                {
                    if (cutUVsWithoutReapplying)
                    {
                        for (int u = 0; u < solid.uvMaps.Count; u++)
                        {
                            newUvs[u].Add(facesCutAsUVs[u][i * 4 + 0] + (facesCutAsUVs[u][i * 4 + 1] - facesCutAsUVs[u][i * 4 + 0]) * ratio);
                            newUvs[u].Add(facesCutAsUVs[u][i * 4 + 0] + (facesCutAsUVs[u][i * 4 + 1] - facesCutAsUVs[u][i * 4 + 0]) * (ratio + sep));
                            newUvs[u].Add(facesCutAsUVs[u][i * 4 + 2] + (facesCutAsUVs[u][i * 4 + 3] - facesCutAsUVs[u][i * 4 + 2]) * ratio);
                            newUvs[u].Add(facesCutAsUVs[u][i * 4 + 2] + (facesCutAsUVs[u][i * 4 + 3] - facesCutAsUVs[u][i * 4 + 2]) * (ratio + sep));
                        }
                    }
                }
            }

        }

        updateSelectionArray(solid, solid.verts.Count);

        // Delete old quads
        solid.selectedVerts = new bool[solid.selectedVerts.Length];
        solid.selectedFaces = new bool[solid.selectedFaces.Length];
        for (int i = 0; i < facesCut.Count; i++)
        {
            solid.selectedFaces[facesCut[i]] = true;
        }

        int deletedFaces = solid.faceNormals.Count;

        solid.delete(solid.selectedVerts, solid.selectedFaces);

        deletedFaces -= solid.faceNormals.Count;

        updateSelectionArray(solid, solid.verts.Count);


        // Push new UVS onto the stack
        if (solid.uvMaps != null && solid.uvMaps.Count > 0)
        {
            if (cutUVsWithoutReapplying)
            {
                for (int u = 0; u < solid.uvMaps.Count; u++)
                {
                    solid.uvMaps[u].resizeUVLength(solid.selectedVerts.Length);

                    int startIndex = solid.uvMaps[u].vertCount - newUvs[u].Count;

                    for (int i = 0; i < newUvs[u].Count; i++)
                    {
                        solid.uvMaps[u]._uvs[startIndex + i] = newUvs[u][i];
                        solid.uvMaps[u]._newUvs[startIndex + i] = newUvs[u][i];
                    }
                }
            }
        }

        solid.bleedInwardAllEdgeValues(facesBeforeAddingNewFaces - deletedFaces);
        //solid.rebuildAdjacentFaces(0);

        solid.pushNewGeometry();
    }

    public static Vector3 closestPoint(Vector3 a, Vector3 b, Vector3 o)
    {
        float d = Vector3.SqrMagnitude(b - a);

        if (d > 0)
        {
            Vector3 ao = o - a;
            Vector3 ab = b - a;
            float dot = Vector3.Dot(ao, ab) / d;
            if (dot <= 0) { return a; }
            else if (dot >= 1) { return b; }
            else
            {
                return a + ab * dot;
            }
        }
        else
        {
            return a;
        }
    }

    public static Vector2 closestPoint(Vector2 a, Vector2 b, Vector2 o)
    {
        float d = Vector2.SqrMagnitude(b - a);

        Vector2 ao = o - a;
        Vector2 ab = b - a;
        float dot = Vector2.Dot(ao, ab) / d;
        if (dot <= 0) { return a; }
        else if (dot >= 1) { return b; }
        else
        {
            return a + ab * dot;
        }
    }

    private static Vector2 constrainToScreenSize(Vector2 position)
    {/*
        if (position.x < 0)
        {
            position.x = 0;
        }
        else if (position.x >= Screen.width)
        {
            position.x = Screen.width - 1;
        }
        if (position.y < 0)
        {
            position.y = 0;
        }
        else if (position.y >= Screen.height)
        {
            position.y = Screen.height - 1;
        }
        */
        return position;
    }

    // UNTESTED
    private List<int> getSelectedPerimeter(MeshEdit solid, bool[] selectedVerts, bool[] selectedFaces, out List<int> faces, int vertMode)
    {
        // A perimeter edge is a group of four values and 0-2 face references
        // [verts connected to the selected face,, verts connected to the non-selected face,] [selected face, non-selected face]
        // [verts connected to the selected face,, -1, -1] [selected face, -1]
        // [verts connected to no face, that make an edge, -1, -1] [-1, -1]
        List<int> selectedEdges = new List<int>();

        List<int> relevantFace = new List<int>();

        bool[] tempSelectedVerts = new bool[selectedVerts.Length];
        for (int i = 0; i < selectedVerts.Length; i++)
        {
            tempSelectedVerts[i] = selectedVerts[i];
        }

        // For each vert, check if its assosciated face is selected.
        // If it is, add an edge wherever an edge can be made from selected vertices
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            int ii = i * 4;
            for (int q = 0; q < 4; q++)
            {
                int a = -1; int b = -1;
                if (q == 0)
                {
                    a = solid.quads[ii + 0];
                    b = solid.quads[ii + 1];
                }
                else if (q == 1)
                {
                    a = solid.quads[ii + 0];
                    b = solid.quads[ii + 2];
                }
                else if (q == 2)
                {
                    a = solid.quads[ii + 3];
                    b = solid.quads[ii + 1];
                }
                else if (q == 3)
                {
                    a = solid.quads[ii + 3];
                    b = solid.quads[ii + 2];
                }
                if (selectedVerts[a] && selectedVerts[b])
                {
                    int c = a;
                    int d = b;
                    int reverseFace = getFirstConnectedFace(
                        solid, ref c, ref d, i);

                    // If the opposite face is selected, ignore this line (it's not a part of the perimeter).
                    // If the opposite face is selected and the current isn't, skip it. It will be filled in later with the right orientation.
                    // relevantFace[i * 2 + 0] represents the selected face
                    if (reverseFace >= 0 &&
                        ((selectedFaces[i] && selectedFaces[reverseFace]) ||
                        (!selectedFaces[i] && selectedFaces[reverseFace])))
                    {
                        continue;
                    }

                    // If the face is a lone edge between two non-selected faces
                    // Skip on face selection
                    if (!selectedFaces[i] && (reverseFace == -1 || !selectedFaces[reverseFace]))
                    {
                        if (vertMode == 1)
                        {
                            continue;
                        }
                    }
                    // Don't skip on vert selection
                    if (!selectedFaces[i] && (reverseFace >= 0 && !selectedFaces[reverseFace]))
                    {
                        int[] abcd = { a, b, c, d };
                        // Make sure it isn't a duplicate
                        bool isDuplicate = false;
                        for (int f = 0; f < selectedEdges.Count; f += 4)
                        {
                            int sameCount = 0;
                            for (int ff = f; ff < f + 4; ff++)
                            {
                                for (int ffc = 0; ffc < 4; ffc++)
                                {
                                    if (abcd[ffc] == selectedEdges[ff])
                                    {
                                        sameCount++;
                                        break;
                                    }
                                }
                            }
                            if (sameCount == 4)
                            {
                                // Same edge found
                                isDuplicate = true;
                                break;
                            }
                        }
                        if (isDuplicate)
                        {
                            continue;
                        }
                    }

                    selectedEdges.Add(a);
                    selectedEdges.Add(b);
                    relevantFace.Add(i);
                    // Add the reverse face to the same verts (If it exists)
                    if (reverseFace >= 0)
                    {
                        selectedEdges.Add(c);
                        selectedEdges.Add(d);
                        relevantFace.Add(reverseFace);
                    }
                    else
                    {
                        selectedEdges.Add(-1);
                        selectedEdges.Add(-1);
                        relevantFace.Add(-1);
                    }
                }
            }
        }

        faces = relevantFace;
        return selectedEdges;
    }
    public int getFirstConnectedFace(MeshEdit solid, ref int v0, ref int v1, int face)
    {
        for (int i = 0; i < solid.connectedVerts[v0].Count; i++)
        {
            for (int j = 0; j < solid.connectedVerts[v1].Count; j++)
            {
                int vertFaceA = solid.quads.IndexOf(solid.connectedVerts[v0].list[i]) / 4;
                int vertFaceB = solid.quads.IndexOf(solid.connectedVerts[v1].list[j]) / 4;
                if (vertFaceA == vertFaceB && face != vertFaceB)
                {
                    v0 = solid.connectedVerts[v0].list[i];
                    v1 = solid.connectedVerts[v1].list[j];
                    return vertFaceB;
                }
            }
        }
        return -1;
    }

    private List<int> getSelectedEdges(MeshEdit solid, bool[] selectedVerts, bool[] selectedFaces)
    {

        List<int> selectedEdges = new List<int>();


        bool[] tempSelectedVerts = new bool[selectedVerts.Length];
        for (int i = 0; i < selectedVerts.Length; i++)
        {
            tempSelectedVerts[i] = selectedVerts[i];
        }
        for (int i = 0; i < selectedVerts.Length; i++)
        {
            if (tempSelectedVerts[i])
            {
                for (int ii = 0; ii < solid.quads.Count; ii += 4)
                {
                    for (int iii = 0; iii < 4; iii++)
                    {
                        if (i != solid.quads[ii + iii])
                        {
                            if (tempSelectedVerts[solid.quads[ii + iii]])
                            {
                                tempSelectedVerts[i] = false;
                                tempSelectedVerts[solid.quads[ii + iii]] = false;

                                selectedEdges.Add(i);
                                selectedEdges.Add(solid.quads[ii + iii]);
                            }
                        }
                    }
                }
            }
        }

        // Remove all edges that are exactly equal in positions
        for (int i = 0; i < selectedEdges.Count; i += 2)
        {
            Vector3 a = solid.verts[selectedEdges[i + 0]];
            Vector3 b = solid.verts[selectedEdges[i + 1]];

            for (int ii = i + 2; ii < selectedEdges.Count; ii += 2)
            {
                Vector3 aa = solid.verts[selectedEdges[ii + 0]];
                Vector3 bb = solid.verts[selectedEdges[ii + 1]];

                if ((aa == a && bb == b) ||
                    (aa == b && bb == a))
                {
                    selectedEdges.RemoveAt(ii);
                    selectedEdges.RemoveAt(ii);
                    ii -= 2;
                }
            }
        }

        return selectedEdges;
    }

    private void updateSelectionArray(MeshEdit solid, int length)
    {
        bool[] newSelectedVerts = new bool[length];
        if (solid.selectedFaces != null)
        {
            for (int i = 0; i < Mathf.Min(solid.selectedVerts.Length, length); i++)
            {
                newSelectedVerts[i] = solid.selectedVerts[i];
            }
        }
        solid.selectedVerts = newSelectedVerts;

        bool[] newSelectedFaces = new bool[length / 4];
        if (solid.selectedFaces != null)
        {
            for (int i = 0; i < Mathf.Min(solid.selectedFaces.Length, newSelectedFaces.Length); i++)
            {
                newSelectedFaces[i] = solid.selectedFaces[i];
            }
        }
        solid.selectedFaces = newSelectedFaces;
    }

    private float getValueFromMouseAxisPosition(Vector3 axes, Vector3 origin, Vector2 mouseOrigin, Vector2 mousePosition, bool asRatio)
    {
        axes = axes.normalized;
        float l = 1;
        Vector3 p0 = origin + axes * (l);

        Vector2 p2D0 = HandleUtility.WorldToGUIPoint(p0);
        Vector2 o = HandleUtility.WorldToGUIPoint(origin);
        Vector2 axes2D = p2D0 - o;

        Vector2 b = MeshEdit.closestPoint(o, p2D0, mousePosition, false);
        Vector2 a = MeshEdit.closestPoint(o, p2D0, mouseOrigin, false);
        float bRatio = Vector2.Dot(b - o, axes2D.normalized) / axes2D.magnitude;
        float aRatio = Vector2.Dot(a - o, axes2D.normalized) / axes2D.magnitude;
        Vector3 aAt3DPos = origin + axes * (aRatio * l);
        Vector3 bAt3DPos = origin + axes * (bRatio * l);
        float dot = Vector3.Dot(aAt3DPos - origin, axes);
        Debug.DrawLine(origin + axes * 10000, origin - axes * 0);
        Debug.DrawLine(aAt3DPos, bAt3DPos, Color.red);
        float value = Vector3.Distance(aAt3DPos, bAt3DPos) * Mathf.Sign(bRatio - aRatio);

        Debug.DrawLine(aAt3DPos, aAt3DPos + value * axes, Color.green);

        if (Vector3.Dot(value * axes, bAt3DPos - aAt3DPos) < 0)
        {
            value *= -1;
        }

        if (!asRatio)
        {
            return value;
        }

        float distanceToOrigin = Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, origin);

        Vector3 projectedMouseOrigin = HandleUtility.GUIPointToWorldRay(a).GetPoint(distanceToOrigin);

        Debug.DrawLine(HandleUtility.GUIPointToWorldRay(b).GetPoint(distanceToOrigin), origin, Color.red);

        Vector3 outv = HandleUtility.GUIPointToWorldRay(b).GetPoint(distanceToOrigin) - projectedMouseOrigin;

        float d = Vector3.Dot(outv, axes.normalized);
        if (Mathf.Sign(d) == Mathf.Sign(dot))
        {
            d *= -1;
        }

        return (d);
        // Vector3.Distance(targetPoint, origin)) * Mathf.Sign(dot);// ((b - o).magnitude / (a - o).magnitude));
    }

    private List<int> affectedVerts;
    private List<int> affectedFaces;

    private void getAffectedVerts(MeshEdit solid)
    {
        bool[] arrayAffectedVerts = new bool[solid.selectedVerts.Length];
        bool[] arrayAffectedFaces = new bool[solid.selectedFaces.Length];

        for (int i = 0; i < solid.selectedVerts.Length; i++)
        {
            arrayAffectedVerts[i] = solid.selectedVerts[i];
        }

        for (int i = 0; i < solid.selectedFaces.Length; i++)
        {
            bool areAnyVertsActive = false;
            for (int ii = 0; ii < 4; ii++)
            {
                if (solid.selectedVerts[solid.quads[i * 4 + ii]])
                {
                    areAnyVertsActive = true;
                    break;
                }
            }
            if (areAnyVertsActive)
            {
                arrayAffectedVerts[solid.quads[i * 4 + 0]] = true;
                arrayAffectedVerts[solid.quads[i * 4 + 1]] = true;
                arrayAffectedVerts[solid.quads[i * 4 + 2]] = true;
                arrayAffectedVerts[solid.quads[i * 4 + 3]] = true;
                arrayAffectedFaces[i] = true;
            }
        }
        for (int i = 0; i < arrayAffectedVerts.Length; i++)
        {
            if (arrayAffectedVerts[i])
            {
                for (int j = 0; j < solid.connectedVerts[i].Count; j++)
                {
                    arrayAffectedVerts[solid.connectedVerts[i].list[j]] = true;
                }
            }

            /*
            for (int ii = 0; ii < affectedVerts.Length; ii++)
            {
                if (i != ii &&
                    selectedVerts[i])
                {
                    if (solid.verts[i] == solid.verts[ii])
                    {
                        affectedVerts[i] = true;
                    }
                }
            }*/
        }

        affectedVerts = new List<int>();
        affectedFaces = new List<int>();

        for (int i = 0; i < arrayAffectedVerts.Length; i++)
        {
            if (arrayAffectedVerts[i])
            {
                affectedVerts.Add(i);
            }
        }
        for (int i = 0; i < arrayAffectedFaces.Length; i++)
        {
            if (arrayAffectedFaces[i])
            {
                affectedFaces.Add(i);
            }
        }
    }

    private void updateMeshEditNormals(MeshEdit solid)
    {

        if (affectedVerts == null && affectedFaces == null)
        {
            solid.recalculateNormals(solid.mesh);
        }
        else
        {
            solid.recalculateSelectedNormals(solid.mesh, affectedVerts, affectedFaces);
        }

        solid.mesh.RecalculateBounds();

        solid.pushLocalMeshToGameObject();
    }

    private void selectionAddTouchingVerts(MeshEdit solid)
    {

        for (int i = 0; i < solid.selectedVerts.Length; i++)
        {
            if (solid.selectedVerts[i])
            {
                for (int j = 0; j < solid.connectedVerts[i].Count; j++)
                {
                    solid.selectedVerts[solid.connectedVerts[i].list[j]] = true;
                }
            }
        }
    }


    private void selectVert(MeshEdit solid, int vert, bool isAdditive)
    {
        solid.selectedVerts[vert] = !isAdditive || !solid.selectedVerts[vert];
        // Also select the second vert that is hidden by the originally selected vert.
        for (int i = 0; i < solid.connectedVerts[vert].Count; i++)
        {
            solid.selectedVerts[solid.connectedVerts[vert].list[i]] = solid.selectedVerts[vert];
        }
    }
    private void setSelectVert(MeshEdit solid, int vert, bool state)
    {
        solid.selectedVerts[vert] = state;
        // Also select the second vert that is hidden by the originally selected vert.
        for (int i = 0; i < solid.connectedVerts[vert].Count; i++)
        {
            solid.selectedVerts[solid.connectedVerts[vert].list[i]] = state;
        }
    }

    private void activateTransformMode(MeshEdit solid, int transformMode)
    {
        if (solid.editorInfo.transformMode == 0 && !transformDimensionsExtrude)
        {

            Undo.RegisterCompleteObjectUndo(solid, "Mesh Transformation");


            //Undo.RegisterCompleteObjectUndo(solid, "Mesh Transformation");
            //Undo.RecordObject(solid.gameObject, "Mesh Transformation");
            transformDimensions = Vector3.zero;
            transformDimensionsPlanar = false;
        }
        else if (solid.editorInfo.transformMode != transformMode && solid.editorInfo.transformMode != 0)
        {
            solid.verts = oldVerts;
            updateMeshVerts(solid);
        }

        solid.editorInfo.transformMode = transformMode;
        getAffectedVerts(solid);
        oldVerts = new List<Vector3>();

        int c = 0;
        anchorCenter = Vector3.zero;
        for (int i = 0; i < solid.verts.Count; i++)
        {
            if (solid.selectedVerts[i])
            {
                anchorCenter += solid.verts[i];
                c++;
            }
            oldVerts.Add(solid.verts[i]);
        }
        anchorCenter /= c;

        if (transformDimensions.sqrMagnitude < 0.9f)
        {
            transformDimensions = Vector3.one;
            transformDimensionsPlanar = false;
        }
    }


    /// <summary>
    /// Update the mesh to reflect the changes made to vertices on the model.
    /// Updated the mesh vertices, then recalculates normals on both the in game model and the mesh.
    /// </summary>
    /// <param name="updatedVerts"></param>
    /// <param name="updatedFaces"></param>
    public void updateMeshVerts(MeshEdit meshEdit, List<int> updatedVerts = null, List<int> updatedFaces = null)
    {
        meshEdit.beginTimer("UpdateMeshVerts");
        List<Vector3> vertsNormalised = new List<Vector3>();

        Vector3 p = meshEdit.gameObject.transform.position;
        Quaternion r = meshEdit.gameObject.transform.rotation;
        Vector3 s = meshEdit.gameObject.transform.lossyScale;
        s.x = Mathf.Max(Mathf.Abs(s.x), MeshEdit.minimumScale) * Mathf.Sign(s.x);
        s.y = Mathf.Max(Mathf.Abs(s.y), MeshEdit.minimumScale) * Mathf.Sign(s.y);
        s.z = Mathf.Max(Mathf.Abs(s.z), MeshEdit.minimumScale) * Mathf.Sign(s.z);

        for (int i = 0; i < meshEdit.verts.Count; i++)
        {

            Vector3 v = meshEdit.verts[i];
            v -= p;

            v = Quaternion.Inverse(r) * v;

            v.x /= s.x;
            v.y /= s.y;
            v.z /= s.z;

            vertsNormalised.Add(v);
        }

        meshEdit.mesh.vertices = vertsNormalised.ToArray();

        // Previously, normals were recalculated here every step but it took forever, especially with smoothed normals and sharp edges etc.
        // So now, we only update the normals once the transformation has been commited.
        if (!MeshEditWindow.isActive() || MeshEditWindow.window.recalculateNormalsOnTransform)
        {
            meshEdit.beginTimer("Update mesh normals");
            if (updatedVerts == null || updatedFaces == null)
            {
                meshEdit.recalculateNormals(meshEdit.mesh);
            }
            else
            {
                meshEdit.recalculateSelectedNormals(meshEdit.mesh, updatedVerts, updatedFaces);
            }
            meshEdit.endTimer();
        }

        meshEdit.pushLocalMeshToGameObject();
        meshEdit.endTimer();
    }

    Rect fullGUIRectangle = Rect.zero;
    private void guiHeader(MeshEdit solid)
    {
        if (!MeshEditWindow.isActive())
        {
            Handles.BeginGUI();
            GUI.skin = skin;

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(uiHeaderPosition.x, uiHeaderPosition.y, 340, 800));

            Rect rect = EditorGUILayout.BeginVertical();
            GUI.Box(rect, GUIContent.none);

            GUI.color = Color.white;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(6);

            //int newEditMode = EditorGUILayout.Popup(solid.editMode, editModes);
            if (GUILayout.Button("Open the MeshEdit window", skinDefault.GetStyle("Button")))
            {
                MeshEditWindow.showWindow();
            }
            //updateEditMode(solid, newEditMode);

            GUILayout.Space(6);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Catches clicks on box
            GUI.Button(rect, "", GUIStyle.none);
            GUILayout.EndArea();

            Handles.EndGUI();
        }
    }

    
    private void exportFbx(MeshEdit solid, string path, string name)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Create(path + name + ".fbx")))
        {
            writer.Write("Kaydara FBX Binary  \x00"); // Header
            writer.Write((byte)0x1A); // ???
            writer.Write((byte)0x00); // ???
            writer.Write((uint)7300); // Version number
        }
    }


    Vector2 scrollPosition = Vector2.zero;

    List<Vector2> uvCoord = new List<Vector2>(new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f) });
    
    public static Material lineMat;
    public static void createLineMaterial()
    {
        if (lineMat == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMat = new Material(shader);
            lineMat.hideFlags = HideFlags.HideAndDontSave;

            lineMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMat.SetInt("_ZWrite", 0);
        }
    }


    public static void loadAllUVMaps(MeshEdit solid)
    {
        List<Tileset> tempTilesetsAvailable = new List<Tileset>();
        List<string> tempTexturesAvailable = new List<string>();
        MeshEditWindow.loadTilesets();

        if (solid.uvMaps == null)
        {
            solid.uvMaps = new List<MeshEdit.UVData>();
        }

        if (tempTilesetsAvailable != null && tempTilesetsAvailable.Count > 0)
        {
            for (int i = 0; i < tempTilesetsAvailable.Count; i++)
            {
                string name = tempTilesetsAvailable[i].tilesetName;
                int indexOfCurrent = solid.uvMaps.FindIndex(map => map.name == name);

                if (indexOfCurrent == -1)
                {
                    solid.uvMaps.Add(
                    new MeshEdit.UVData(
                        MeshEditWindow.tilesetsAvailable[i].tilesetName,
                        MeshEditWindow.tilesetsAvailable[i].texturePage.width,
                        MeshEditWindow.tilesetsAvailable[i].texturePage.height,
                        MeshEditWindow.tilesetsAvailable[i].tileWidth,
                        MeshEditWindow.tilesetsAvailable[i].tileHeight,
                        MeshEditWindow.tilesetsAvailable[i].tileOutline,
                        solid.verts.Count,
                        solid.defaultUVs));
                }

            }
        }
    }


    private void operationsTextureTiling(MeshEdit solid, int controlId)
    {
        if ((MeshEditWindow.tilesetsAvailable != null && MeshEditWindow.tilesetsAvailable.Count > 0) ||
            (solid.selectedTileset == -1 && solid.selectedTexture != null))
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();

            GameObject obj = Selection.activeTransform.gameObject;
            //Solid solid = obj.GetComponent<Solid>();
            // meshFilter = obj.GetComponent<MeshFilter>();
            //Mesh meshCopy = Mesh.Instantiate(meshFilter.sharedMesh) as Mesh;
            MeshEdit meshEdit = obj.GetComponent<MeshEdit>();// meshFilter.sharedMesh;

            // Again, moved to onEnable
            //if (false) { solid.checkMeshValidity(); }

            if ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) && Event.current.button == 0)
            {
                selectedTri = -1;

                ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition); //Camera.current.ScreenPointToRay(Input.mousePosition);


                if (meshEdit.mesh != null)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    Vector3 colPoint = Vector3.zero;
                    float d = float.MaxValue;
                    for (int i = 0; i < solid.tris.Count; i += 3)
                    {
                        t = new MeshEdit.Triangle(
                            solid.verts[solid.tris[i + 0]],
                            solid.verts[solid.tris[i + 1]],
                            solid.verts[solid.tris[i + 2]]);

                        if (rayIntersectsTriangle(ray.origin, ray.direction, t, ref colPoint))
                        {
                            float dd = Vector3.SqrMagnitude(colPoint - ray.origin);
                            if (dd < d)
                            {
                                d = dd;
                                selectedTri = i;
                                selectedQuad = i / 6;
                            }
                        }
                    }
                }
            }

            if (Event.current.type == EventType.MouseUp)
            {
                canTextureFaces = true;
            }
            if (selectedTri >= 0)
            {
                if ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown) && Event.current.button == 0)
                {
                    if (Event.current.modifiers == EventModifiers.Alt || Event.current.modifiers == EventModifiers.Command || Event.current.modifiers == EventModifiers.Control)
                    {
                        canTextureFaces = false;

                        if (MeshEditWindow.isActive())
                        {
                            MeshEditWindow.window.readUVsOfSelectedQuads(solid, selectedQuad);
                        }
                    }
                    else if (canTextureFaces)
                    {
                        // Optimisation:
                        // Chec to see if any change is nevessary
                        // Try to eliminate the deep copy

                        //Undo.RecordObject(meshFilter.mesh, "Model Mesh");
                        Undo.RegisterCompleteObjectUndo(solid, "Model Mesh Solid");

                        if (!shift)
                        {
                            //Vector2[] newUVs = solid.uvMaps[solid.currentUVMap].newUvs;
                            Vector2[] newUVs;

                            if (solid.materialUVMap[solid.selectedMaterial] >= 0 &&
                                solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvs != null &&
                                solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvs.Length == meshEdit.mesh.uv.Length)
                            {
                                solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].newUvs = new Vector2[meshEdit.mesh.vertices.Length];
                                newUVs = solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvs;
                                /*
                                for (int i = 0; i < mesh.vertices.Length; i++)
                                {
                                    newUVs[i] = solid.uvMaps[solid.currentUVMap].uvs[i];
                                }*/
                            }
                            else
                            {
                                newUVs = new Vector2[meshEdit.mesh.vertices.Length];

                                for (int i = 0; i < meshEdit.mesh.uv.Length; i++)
                                {
                                    newUVs[i] = meshEdit.mesh.uv[i];
                                }
                            }

                            int[] coords = { -1, -1, -1, -1 };
                            /*coords[0] = mesh.triangles[selectedTri];
                            coords[1] = mesh.triangles[selectedTri + 1];
                            coords[2] = mesh.triangles[selectedTri + 2];
                            coords[3] = findAdjacentTriPoint(mesh.triangles, mesh.vertices, ref coords[0], ref coords[1], ref coords[2]);
                            */
                            coords[0] = solid.quads[selectedQuad * 4 + 0];
                            coords[1] = solid.quads[selectedQuad * 4 + 1];
                            coords[2] = solid.quads[selectedQuad * 4 + 2];
                            coords[3] = solid.quads[selectedQuad * 4 + 3];


                            /*
                            int[] coords = solid.getQuadFromTriTable(selectedTri);*/

                            int topRight = -1;
                            int topLeft = -1;
                            int bottomRight = -1;
                            int bottomLeft = -1;

                            #region reorganise the quad vertices so that they represent the correct tile rotation.

                            Vector2[] screenCoords = new Vector2[4];
                            screenCoords[0] = HandleUtility.WorldToGUIPoint(solid.verts[coords[0]]);
                            screenCoords[1] = HandleUtility.WorldToGUIPoint(solid.verts[coords[1]]);
                            screenCoords[2] = HandleUtility.WorldToGUIPoint(solid.verts[coords[2]]);
                            screenCoords[3] = HandleUtility.WorldToGUIPoint(solid.verts[coords[3]]);


                            float min1 = Mathf.Min(screenCoords[0].y, screenCoords[1].y, screenCoords[2].y, screenCoords[3].y);
                            for (int i = 0; i < 4; i++)
                            {
                                if (Mathf.Abs(screenCoords[i].y - min1) <= float.Epsilon)
                                {
                                    screenCoords[i].y = float.MaxValue;
                                    topRight = i;
                                    break;
                                }
                            }
                            float min2 = Mathf.Min(screenCoords[0].y, screenCoords[1].y, screenCoords[2].y, screenCoords[3].y);
                            for (int i = 0; i < 4; i++)
                            {
                                if (Mathf.Abs(screenCoords[i].y - min2) <= float.Epsilon && topLeft == -1)
                                {
                                    screenCoords[i].y = float.MaxValue;
                                    topLeft = i;
                                }
                                else if (screenCoords[i].y < float.MaxValue)
                                {
                                    if (bottomRight == -1) { bottomRight = i; } else { bottomLeft = i; }
                                }
                            }
                            if (screenCoords[topLeft].x > screenCoords[topRight].x)
                            {
                                int temp = topLeft;
                                topLeft = topRight;
                                topRight = temp;

                            }
                            if (screenCoords[bottomLeft].x > screenCoords[bottomRight].x)
                            {
                                int temp = bottomLeft;
                                bottomLeft = bottomRight;
                                bottomRight = temp;
                            }

                            rotate(solid.editorInfo.tileDirection, ref topLeft, ref topRight, ref bottomRight, ref bottomLeft);
                            #endregion

                            if (solid.materialUVMap[solid.selectedMaterial] >= 0 ||
                                (solid.materialUVMap[solid.selectedMaterial] == -1 && solid.selectedTexture != null))
                            {
                                #region Get the new UVs based on the texture or tileset

                                Color col = Color.white;

                                // The outline between each tile on the texture page
                                float outline = solid.editorInfo.tileOutline;
                                if (solid.selectedTileset >= 0)
                                {
                                    outline = MeshEditWindow.tilesetsAvailable[solid.selectedTileset].tileOutline;
                                }

                                float epsilonX = outline / solid.editorInfo.texturePage.width;
                                float epsilonY = outline / solid.editorInfo.texturePage.height;
                                // Convert from the tileset selected tile to the appropriate tile on the texture page.
                                Vector2 targetTile = new Vector2(solid.editorInfo.selectedTile % solid.editorInfo.tilesPerRow, solid.editorInfo.selectedTile / solid.editorInfo.tilesPerRow);

                                // Index of the selected tile on the texture
                                int indexOfTarget = solid.editorInfo.selectedTile;

                                if (solid.selectedTileset >= 0)
                                {
                                    indexOfTarget = MeshEditWindow.tilesetsAvailable[solid.selectedTileset].tilesetMappedPoints.IndexOf(targetTile);

                                    if (indexOfTarget >= 0)
                                    {
                                        targetTile = MeshEditWindow.tilesetsAvailable[solid.selectedTileset].editorMappedPoints[indexOfTarget];
                                    }

                                }

                                int tx = (int)targetTile.x;
                                int tPC = solid.editorInfo.texturePage.height / (solid.editorInfo.tileHeight + (int)outline * 2);
                                int ty = (tPC - 1) - (int)targetTile.y;
                                float txx = tx / (float)solid.editorInfo.tilesPerRow;
                                float tyy = ty / (float)tPC;
                                float ww = 1.0f / solid.editorInfo.tilesPerRow;
                                float hh = 1.0f / tPC;

                                if (MeshEditWindow.isActive() && !MeshEditWindow.window.useCustomUVCoords)
                                {
                                    newUVs[coords[topLeft]] = new Vector2(txx + epsilonX, tyy + hh - epsilonY);
                                    newUVs[coords[topRight]] = new Vector2(txx + ww - epsilonX, tyy + hh - epsilonY);
                                    newUVs[coords[bottomLeft]] = new Vector2(txx + epsilonX, tyy + epsilonY);
                                    newUVs[coords[bottomRight]] = new Vector2(txx + ww - epsilonX, tyy + epsilonY);
                                }
                                else
                                {
                                    List<Vector2> uvsInEpsilon = new List<Vector2>();
                                    for (int i = 0; i < uvCoord.Count; i++)
                                    {
                                        Vector2 coord = new Vector2(
                                            (solid.editorInfo.uvCoordSnapped[i].x * solid.editorInfo.tileWidth) / (solid.editorInfo.tileWidth + outline * 2),
                                            (solid.editorInfo.uvCoordSnapped[i].y * solid.editorInfo.tileHeight) / (solid.editorInfo.tileHeight + outline * 2));

                                        uvsInEpsilon.Add(coord);
                                    }

                                    float ratioOverTextureWidth = 1.0f;
                                    float ratioOverTextureHeight = 1.0f;
                                    if (solid.selectedTileset >= 0)
                                    {
                                        ratioOverTextureWidth = (solid.editorInfo.tilesPerRow * (solid.editorInfo.tileWidth + outline * 2)) / solid.editorInfo.texturePage.width;
                                        ratioOverTextureHeight = (tPC * (solid.editorInfo.tileHeight + outline * 2)) / solid.editorInfo.texturePage.height;
                                    }
                                    else if (solid.selectedTexture)
                                    {
                                        ratioOverTextureWidth = (solid.editorInfo.tilesPerRow * (solid.editorInfo.tileWidth + outline * 2)) / solid.selectedTexture.width;
                                        ratioOverTextureHeight = (solid.editorInfo.tilesPerColumn * (solid.editorInfo.tileHeight + outline * 2)) / solid.selectedTexture.height;
                                    }

                                    txx = (targetTile.x + ((uvsInEpsilon[0].x))) / solid.editorInfo.tilesPerRow * ratioOverTextureWidth;
                                    tyy = 1.0f - (targetTile.y + uvsInEpsilon[0].y) / tPC * ratioOverTextureHeight;
                                    newUVs[coords[topLeft]] = new Vector2(txx + epsilonX, tyy - epsilonY);
                                    txx = (targetTile.x + uvsInEpsilon[1].x) / solid.editorInfo.tilesPerRow * ratioOverTextureWidth;
                                    tyy = 1.0f - (targetTile.y + uvsInEpsilon[1].y) / tPC * ratioOverTextureHeight;
                                    newUVs[coords[topRight]] = new Vector2(txx + epsilonX, tyy - epsilonY);
                                    txx = (targetTile.x + uvsInEpsilon[2].x) / solid.editorInfo.tilesPerRow * ratioOverTextureWidth;
                                    tyy = 1.0f - (targetTile.y + uvsInEpsilon[2].y) / tPC * ratioOverTextureHeight;
                                    newUVs[coords[bottomRight]] = new Vector2(txx + epsilonX, tyy - epsilonY);
                                    txx = (targetTile.x + uvsInEpsilon[3].x) / solid.editorInfo.tilesPerRow * ratioOverTextureWidth;
                                    tyy = 1.0f - (targetTile.y + uvsInEpsilon[3].y) / tPC * ratioOverTextureHeight;
                                    newUVs[coords[bottomLeft]] = new Vector2(txx + epsilonX, tyy - epsilonY);
                                }


                                #endregion

                                if (solid.selectedTileset >= 0)
                                {
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].vertCount = meshEdit.mesh.uv.Length;

                                    int animationLength = MeshEditWindow.tilesetsAvailable[solid.selectedTileset].getAnimationLength(solid.editorInfo.selectedTile);
                                    // Debug.Log("Animation Length of selected tile = " + animationLength);
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationLength[coords[topLeft]] = animationLength;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationLength[coords[topRight]] = animationLength;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationLength[coords[bottomLeft]] = animationLength;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationLength[coords[bottomRight]] = animationLength;

                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationIndex[coords[topLeft]] = 0;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationIndex[coords[topRight]] = 0;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationIndex[coords[bottomLeft]] = 0;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationIndex[coords[bottomRight]] = 0;

                                    int startPos = tx + ty * solid.editorInfo.tilesPerRow;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationStartPosition[coords[topLeft]] = startPos;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationStartPosition[coords[topRight]] = startPos;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationStartPosition[coords[bottomLeft]] = startPos;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationStartPosition[coords[bottomRight]] = startPos;

                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationQuadPoint[coords[topLeft]] = 0;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationQuadPoint[coords[topRight]] = 1;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationQuadPoint[coords[bottomLeft]] = 3;
                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvAnimationQuadPoint[coords[bottomRight]] = 2;


                                    solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvs = newUVs;
                                }
                                else
                                {
                                    solid.customTextureUVMap[coords[topLeft]] = newUVs[coords[topLeft]];
                                    solid.customTextureUVMap[coords[topRight]] = newUVs[coords[topRight]];
                                    solid.customTextureUVMap[coords[bottomRight]] = newUVs[coords[bottomRight]];
                                    solid.customTextureUVMap[coords[bottomLeft]] = newUVs[coords[bottomLeft]];
                                }
                            }
                        }
                        else
                        {

                        }

                        if (solid.quadMaterial[selectedQuad] != solid.selectedMaterial)
                        {
                            solid.quadMaterial[selectedQuad] = solid.selectedMaterial;
                            solid.setTriangles();
                        }

                        solid.pushUVData();
                        //meshEdit.mesh.uv = newUVs;
                        meshEdit.pushLocalMeshToGameObject();
                        // Tell the UI your event is the main one to use, it override the selection in  the scene view
                    }
                    GUIUtility.hotControl = controlId;

                    Event.current.Use();
                }
            }
        }
    }

    bool canTextureFaces = true;

    Vector2 dumRectangle = new Vector2(20 - 10, 60 + 269);
    Vector2 uiHeaderPosition = new Vector2(20, 20);
    Vector2 uiPosition = new Vector2(20, 60);


    private void guiColourEditing(MeshEdit solid, int controlId)
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        GUILayout.BeginArea(new Rect(uiPosition.x, uiPosition.y, 340, 800));

        Rect editRect = EditorGUILayout.BeginVertical();
        GUI.Box(editRect, GUIContent.none);
        EditorGUILayout.BeginVertical();

        GUILayout.Space(16);
        GUILayout.BeginHorizontal();
        GUILayout.Space(6);

        int oldPaintMode = solid.paintMode;
        GUILayout.Label("Paint Mode: ", GUILayout.Width(80));
        solid.paintMode = EditorGUILayout.Popup(solid.paintMode, new string[] { "Vertices", "Faces" }, GUILayout.Width(100));
        if (oldPaintMode != solid.paintMode)
        {
            saveSettings(solid);
        }


        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Space(6);
        GUILayout.Label("Paint Colour: ", GUILayout.Width(80));

        EditorGUILayout.ColorField(solid.editorInfo.paintColour, GUILayout.Width(100));

        GUILayout.EndHorizontal();
        GUILayout.Space(6);

        if (solid.editorInfo.colourHistory == null)
        {
            solid.editorInfo.colourHistory = new List<Color>();
        }
        skin.button.normal.background = null;
        for (int j = 0; j < solid.editorInfo.maxColours / 10; j++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            Color c = Color.black;
            for (int i = j * 10; i < solid.editorInfo.maxColours && i < (j + 1) * 10; i++)
            {
                if (i < solid.editorInfo.colourHistory.Count)
                {
                    c = solid.editorInfo.colourHistory[i];
                }
                else
                {
                    c = Color.black;
                }

                GUI.color = c;

                if (GUILayout.Button(texColourSwatch, GUILayout.Width(32), GUILayout.Width(22)))
                {
                    solid.editorInfo.paintColour = c;
                }
            }

            GUI.color = Color.white;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        if (helpMode)
        {
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);
            GUILayout.BeginVertical();
            if (solid.paintMode == 0)
            {
                GUILayout.Label("LMB - Paint vert");
                GUILayout.Label("MMB - Remove colour on vert");
            }
            else if (solid.paintMode == 1)
            {
                GUILayout.Label("LMB - Paint face");
                GUILayout.Label("MMB - Remove colour on face");
            }
            GUILayout.Label("Scroll - Change paintbrush size");
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        // Catches clicks on box
        GUI.Button(editRect, "", GUIStyle.none);
        GUILayout.EndArea();
    }

    Material circleMaterial;
    private void createCircleMaterial()
    {
        if (circleMaterial == null)
        {
            Shader shader = Shader.Find("MeshEdit/AOTLine");
            circleMaterial = new Material(shader);

            circleMaterial.SetTexture("_MainTex", selectCircleTexture);
            circleMaterial.SetFloat("_Alpha", 1f);
        }
    }

    private void operationsColourEdit(MeshEdit solid, int controlId)
    {
        Vector2 mousePos = Vector2.zero;
        mousePos = Event.current.mousePosition;
        mousePos = constrainToScreenSize(mousePos);

        #region Selection Mode
        if (Event.current.type == EventType.Repaint)
        {
        }
        if (Event.current.type != EventType.Used)
        {
            if (Event.current.type == EventType.ScrollWheel && Event.current.modifiers == EventModifiers.Shift)
            {
                float s = Event.current.delta.y;
                selectionCircleRadius += s;
                if (selectionCircleRadius < 1)
                {
                    selectionCircleRadius = 1;
                }
                else if (selectionCircleRadius > 100)
                {
                    selectionCircleRadius = 100;
                }
                
                Event.current.Use();
            }

            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;
            /*
            Graphics.DrawTexture(
                new Rect(
                    mousePos.x - selectionCircleRadius,
                    mousePos.y - selectionCircleRadius,
                    selectCircleTexture.width,
                    selectCircleTexture.width),
                selectCircleTexture);
                */
            bool wasChanged = false;
            bool wasErasing = false;
            bool wasAttemptingErase = false;

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
            {
                Undo.RecordObject(solid, "Colouring");

                if (solid.paintMode == 0)
                {
                    if (Event.current.button == 0)
                    {
                        for (int i = 0; i < solid.verts.Count; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.verts[i]);
                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                if (solid.colours[i] != solid.editorInfo.paintColour &&
                                    !solid.isVertCovered(i) || solid.isMeshTransparent)
                                {
                                    solid.colours[i] = solid.editorInfo.paintColour;
                                    for (int ii = 0; ii < solid.connectedVerts[i].Count; ii++)
                                    {
                                        solid.colours[solid.connectedVerts[i].list[ii]] = solid.editorInfo.paintColour;
                                    }
                                    wasChanged = true;
                                }
                            }
                        }
                    }
                    else if (Event.current.button == 2)
                    {
                        for (int i = 0; i < solid.verts.Count; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.verts[i]);
                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                wasAttemptingErase = true;
                                if (solid.colours[i] != Color.white &&
                                    !solid.isVertCovered(i) || solid.isMeshTransparent)
                                {
                                    solid.colours[i] = Color.white;
                                    for (int ii = 0; ii < solid.connectedVerts[i].Count; ii++)
                                    {
                                        solid.colours[solid.connectedVerts[i].list[ii]] = Color.white;
                                    }
                                    wasChanged = true;
                                    wasErasing = true;
                                }
                            }
                        }
                    }
                }
                else if (solid.paintMode == 1)
                {
                    if (Event.current.button == 0)
                    {
                        for (int i = 0; i < solid.faceNormals.Count; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.quadCenter(i));
                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                if (!solid.isFaceCovered(i) || solid.isMeshTransparent)
                                {
                                    for (int ii = 0; ii < 4; ii++)
                                    {

                                        if (solid.colours[i * 4 + ii] != solid.editorInfo.paintColour)
                                        {
                                            solid.colours[i * 4 + ii] = solid.editorInfo.paintColour;
                                            wasChanged = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (Event.current.button == 2)
                    {
                        for (int i = 0; i < solid.faceNormals.Count; i++)
                        {
                            Vector2 vertScreen = HandleUtility.WorldToGUIPoint(solid.quadCenter(i));
                            float d = (mousePos - vertScreen).sqrMagnitude;
                            if (d < selectionCircleRadius * selectionCircleRadius)
                            {
                                wasAttemptingErase = true;
                                if (!solid.isFaceCovered(i) || solid.isMeshTransparent)
                                {
                                    for (int ii = 0; ii < 4; ii++)
                                    {

                                        if (solid.colours[i * 4 + ii] != Color.white)
                                        {
                                            solid.colours[i * 4 + ii] = Color.white;
                                            wasChanged = true;
                                            wasErasing = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (wasChanged)
                {
                    if (!wasErasing)
                    {
                        setLatestColour(solid);
                    }
                    solid.pushColour();
                }

                if (Event.current.button == 0 || wasAttemptingErase)
                {
                    GUIUtility.hotControl = controlId;
                    Event.current.Use();
                }
            }
        }

        solid.tempPaintCircleRadius = selectionCircleRadius;

        SceneView.lastActiveSceneView.Repaint();
        
        #endregion
    }

    private void setLatestColour(MeshEdit solid)
    {
        if (solid.editorInfo.colourHistory == null)
        {
            solid.editorInfo.colourHistory = new List<Color>();
        }

        solid.editorInfo.colourHistory.Insert(0, solid.editorInfo.paintColour);

        for (int i = 1; i < solid.editorInfo.colourHistory.Count; i++)
        {
            if (solid.editorInfo.paintColour == solid.editorInfo.colourHistory[i] ||
                i >= solid.editorInfo.maxColours)
            {
                solid.editorInfo.colourHistory.RemoveAt(i);
                i--;
            }
        }

        saveSettings(solid);
    }

    private Vector3 maskVector(Vector3 v, Vector3 mask)
    {
        return new Vector3(
            v.x * mask.x,
            v.y * mask.y,
            v.z * mask.z);
    }

    private List<int> getEdgeLoop(MeshEdit solid, int faceA, int vertAFromFaceA, int vertBFromFaceA)
    {
        List<int> vertsInLoopPartial = new List<int>();
        // Get the opposite side to begin the edge tracking
        int sideOfEdgeOnFaceA = -1;

        for (int i = 0; i < MeshEdit.quadEdgePatternClockwise.Length; i += 2)
        {
            if ((solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[i + 0]] == vertAFromFaceA &&
                solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[i + 1]] == vertBFromFaceA) ||
                (solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[i + 0]] == vertBFromFaceA &&
                solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[i + 1]] == vertAFromFaceA))
            {
                sideOfEdgeOnFaceA = i / 2;
                break;
            }
        }

        // The inputs were not accurate
        if (sideOfEdgeOnFaceA == -1)
        {
            return null;
        }


        int faceB = solid.adjacentFaces[faceA].list[sideOfEdgeOnFaceA];
        int sideOfEdgeOnFaceB = -1;
        if (faceB != -1)
        {
            sideOfEdgeOnFaceB = solid.adjacentFaces[faceB].list.IndexOf(faceA);
        }

        // Sync the "isNSCut" variables for each of the opposite faces. They must be defined so that they will move in the same direction
        bool isNSCutA = (sideOfEdgeOnFaceA == 1 || sideOfEdgeOnFaceA == 3);
        bool isNSCutB = false;

        if (faceB != -1)
        {
            isNSCutB = (sideOfEdgeOnFaceB == 1 || sideOfEdgeOnFaceB == 3);
        }


        // Face A is always "Real" as in it can't be -1
        // TODO: When we move to a system of polys instead of quads this method will need to be re-thought
        List<int> addedFacesA = new List<int>();
        List<int> addedFacesB = new List<int>();
        List<bool> addedFaceDirectionA = new List<bool>();
        List<bool> addedFaceDirectionB = new List<bool>();
        List<int> partialVertList = new List<int>();

        bool ignoreASide = (faceA == -1);
        bool ignoreBSide = (faceB == -1);

        addedFacesA.Add(faceA);
        addedFacesB.Add(faceB);
        addedFaceDirectionA.Add(!isNSCutA);
        addedFaceDirectionB.Add(!isNSCutB);

        vertsInLoopPartial.Add(vertAFromFaceA);
        vertsInLoopPartial.Add(vertBFromFaceA);



        // keep adding quads to cut until you encounter a quad with no adjacent face to cut, OR if you encounter a quad that has already been cut in the SAME DIRECTION that you try to cut it
        // Repeat this loop forward and backward from the selected face
        // A cut along the North-West (NW) axis refers to the cut travelling from edge 0 to edge 1 (The edges created by verts 0-1 and 3-2)

        // A cut on an edge loop is similar, but it uses TWO, ADJACENT FACES instead of a lone one for the quad loop-select
        // Now, an additional condition is added. If the two face loops ever stop being adjacent in the same direction, the loop ends.
        // The original loop termination conditions apply for each face loop as well

        // There is also the contingency of searching for an edge loop on an edge with no adjacent face. In this case, the relevant side of the edge shouldn't be considered

        // TODO: Figure out why this won't flip-flop

        int edgeTheFirstCutAEnteredFrom = 0;

        int edgeTheFirstCutBEnteredFrom = 0;

        if (!isNSCutA)
        {
            edgeTheFirstCutAEnteredFrom = 1;
        }

        if (!isNSCutB)
        {
            edgeTheFirstCutBEnteredFrom = 1;
        }

        // Find the sides for FaceA and FaceB where one (and only one) vert on the shared edge are shared.
        {
            int vAA = vertAFromFaceA;
            int vAB = vertBFromFaceA;

            int sharedVert = vAA;

            // Find the correct side to start from on Face B
            if (!ignoreBSide)
            {
                for (int ii = 0; ii < 4; ii++)
                {
                    int vBA = solid.quads[faceB * 4 + MeshEdit.quadEdgePatternClockwise[ii * 2 + 0]];
                    int vBB = solid.quads[faceB * 4 + MeshEdit.quadEdgePatternClockwise[ii * 2 + 1]];

                    int sharedCount = 0;

                    if (solid.isShared(vAA, vBA))
                    {
                        sharedVert = vAA;
                        sharedCount++;
                    }
                    if (solid.isShared(vAB, vBA))
                    {
                        sharedVert = vAB;
                        sharedCount++;
                    }
                    if (solid.isShared(vAA, vBB))
                    {
                        sharedVert = vAA;
                        sharedCount++;
                    }
                    if (solid.isShared(vAB, vBB))
                    {
                        sharedVert = vAB;
                        sharedCount++;
                    }

                    if (sharedCount == 1)
                    {
                        edgeTheFirstCutBEnteredFrom = ii;
                        break;
                    }
                }
            }

            // Find the correct side to start from for Face A
            // This is the side that touches the shared vert, but does not face the edge that's added to the selection
            int checkedVert = -1;

            for (int ii = 0; ii < 4; ii++)
            {
                vAA = solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[ii * 2 + 0]];
                vAB = solid.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[ii * 2 + 1]];

                int sharedCount = 0;

                if (vAA == vertAFromFaceA)
                {
                    checkedVert = vAA;
                    sharedCount++;
                }
                if (vAB == vertAFromFaceA)
                {
                    checkedVert = vAB;
                    sharedCount++;
                }
                if (vAA == vertBFromFaceA)
                {
                    checkedVert = vAA;
                    sharedCount++;
                }
                if (vAB == vertBFromFaceA)
                {
                    checkedVert = vAB;
                    sharedCount++;
                }

                if (sharedCount == 1 && (checkedVert == sharedVert || ignoreBSide))
                {
                    edgeTheFirstCutAEnteredFrom = ii;
                    break;
                }
            }
        }

        for (int i = 0; i < 2; i++)
        {
            int edgeTheNextCutAEnteredFrom = edgeTheFirstCutAEnteredFrom;

            int edgeTheNextCutBEnteredFrom = edgeTheFirstCutBEnteredFrom;

            if (i > 0)
            {
                edgeTheNextCutAEnteredFrom = (edgeTheNextCutAEnteredFrom + 2) % 4;
                edgeTheNextCutBEnteredFrom = (edgeTheNextCutBEnteredFrom + 2) % 4;
            }

            int lastFaceA = faceA;
            int lastFaceB = faceB;

            int currentEdgeA = vertAFromFaceA;
            int currentEdgeB = vertBFromFaceA;

            bool isConnectionFound = true;

            int cc = 0;
            while (isConnectionFound)
            {
                cc++;
                if (cc > 10000)
                {
                    Console.WriteLine("Error");
                    break;
                }

                int nextFaceA = solid.adjacentFaces[lastFaceA].list[(edgeTheNextCutAEnteredFrom + 2) % 4];

                if (nextFaceA == -1)
                {
                    break;
                }

                edgeTheNextCutAEnteredFrom = solid.adjacentFaces[nextFaceA].list.IndexOf(lastFaceA);

                int nextFaceB = -1;
                if (!ignoreBSide)
                {
                    nextFaceB = solid.adjacentFaces[lastFaceB].list[(edgeTheNextCutBEnteredFrom + 2) % 4];
                    if (nextFaceB == -1)
                    {
                        break;
                    }
                    edgeTheNextCutBEnteredFrom = solid.adjacentFaces[nextFaceB].list.IndexOf(lastFaceB);

                }

                // The North-South variable isn't absolute, and a series of NS cuts may make a loop that cuts perpendicular along the loop. It's only necessary to compare a quad to itself in the case of a crossover in the loop.
                bool isNextAAnNSCut = (edgeTheNextCutAEnteredFrom == 1 || edgeTheNextCutAEnteredFrom == 3);
                bool isNextBAnNSCut = (edgeTheNextCutBEnteredFrom == 1 || edgeTheNextCutBEnteredFrom == 3);

                // Make sure "nextFace" hasn't already been cut in the same way
                if (!ignoreASide)
                {
                    int repeatIndexA = addedFacesA.IndexOf(nextFaceA);
                    if (repeatIndexA >= 0)
                    {
                        if (addedFaceDirectionA[repeatIndexA] == isNextAAnNSCut)
                        {
                            isConnectionFound = false;
                        }
                    }
                }
                if (!ignoreBSide)
                {
                    int repeatIndexB = addedFacesB.IndexOf(nextFaceB);

                    if (repeatIndexB >= 0)
                    {
                        if (addedFaceDirectionB[repeatIndexB] == isNextBAnNSCut)
                        {
                            isConnectionFound = false;
                        }
                    }
                }

                if (!ignoreASide && !ignoreBSide)
                {
                    // Check to see if the two sides stop being adjacent
                    if (solid.areFacesAdjacent(nextFaceA, nextFaceB))
                    {
                        // No discrepancy found
                    }
                    else
                    {
                        isConnectionFound = false;
                    }
                }

                if (isConnectionFound)
                {
                    bool foundVerts = false;
                    // Pick the correct two verts to add to the partial list

                    for (int j = 0; j < 4; j++)
                    {
                        //if (j / 2 != edgeTheNextCutAEnteredFrom &&
                        //   j / 2 != (edgeTheNextCutAEnteredFrom + 2) % 4)
                        {
                            //if (j == solid.adjacentFaces[nextFaceA].list.IndexOf(nextFaceB))
                            if (solid.adjacentFaces[nextFaceA].list[j] == nextFaceB ||
                                (ignoreBSide && solid.adjacentFaces[nextFaceA].list[j] == -1))
                            {
                                int nextEdgeA = solid.quads[nextFaceA * 4 + MeshEdit.quadEdgePatternClockwise[j * 2 + 0]];
                                int nextEdgeB = solid.quads[nextFaceA * 4 + MeshEdit.quadEdgePatternClockwise[j * 2 + 1]];

                                if (nextFaceB >= 0 ||
                                    (
                                    solid.isShared(nextEdgeA, currentEdgeA) ||
                                    solid.isShared(nextEdgeA, currentEdgeB) ||
                                    solid.isShared(nextEdgeB, currentEdgeA) ||
                                    solid.isShared(nextEdgeB, currentEdgeB)))
                                {
                                    /*
                                    loopDebugMemory2.Add(solid.quadCenter(nextFaceA));
                                    loopDebugMemory2.Add(solid.quadCenter(solid.adjacentFaces[nextFaceA].list[edgeTheNextCutAEnteredFrom]));
                                    loopDebugMemory2.Add(solid.quadCenter(nextFaceB));
                                    loopDebugMemory2.Add(solid.quadCenter(solid.adjacentFaces[nextFaceB].list[edgeTheNextCutBEnteredFrom]));
                                    loopDebugMemory2.Add(solid.quadCenter(nextFaceA));
                                    loopDebugMemory2.Add(solid.quadCenter(nextFaceB));
                                    loopDebugMemory2.Add(solid.quadCenter(nextFaceA));
                                    loopDebugMemory2.Add((solid.verts[nextEdgeA] + solid.verts[nextEdgeB]) / 2);
                                    */
                                    vertsInLoopPartial.Add(nextEdgeA);
                                    vertsInLoopPartial.Add(nextEdgeB);

                                    currentEdgeA = nextEdgeA;
                                    currentEdgeB = nextEdgeB;
                                    foundVerts = true;

                                    break;
                                }
                            }
                        }
                    }

                    if (!foundVerts)
                    {
                        //Debug.Log("Error!! Could not find correct side");
                        isConnectionFound = false;
                    }
                    else
                    {
                        lastFaceA = nextFaceA;
                        lastFaceB = nextFaceB;

                        // A
                        if (isNextAAnNSCut)
                        {
                            addedFaceDirectionA.Add(true);
                        }
                        else
                        {
                            addedFaceDirectionA.Add(false);
                        }
                        // B
                        if (isNextBAnNSCut)
                        {
                            addedFaceDirectionB.Add(true);
                        }
                        else
                        {
                            addedFaceDirectionB.Add(false);
                        }
                    }
                }

            }
        }

        return vertsInLoopPartial;
    }

    private void clearSelected(MeshEdit solid)
    {
        selectedEdges = new bool[0];
        solid.selectedFaces = new bool[solid.quads.Count / 4];
        solid.selectedVerts = new bool[solid.verts.Count];
    }

    public void updateMeshUVs(MeshEdit solid)
    {
        GameObject obj = Selection.activeTransform.gameObject;
        //MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

        MeshEdit meshEdit = obj.GetComponent<MeshEdit>();
        //Mesh mesh = meshFilter.sharedMesh;



        //  solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].newUvs = new Vector2[meshEdit.mesh.vertices.Length];

        meshEdit.mesh.uv = solid.getCombinedUVSFromMaterials();

        meshEdit.pushLocalMeshToGameObject();
    }

    public static int fileCount(DirectoryInfo d)
    {
        int i = 0;
        // Add file sizes.
        FileInfo[] fis = d.GetFiles();
        foreach (FileInfo fi in fis)
        {
            if (fi.Extension.Contains("png"))
            {
                i++;
            }
        }
        return i;
    }
    public static List<string> getFileNames(DirectoryInfo d, string extension)
    {

        List<string> files = new List<string>();
        // Add file sizes.
        FileInfo[] fis = d.GetFiles();
        foreach (FileInfo fi in fis)
        {
            if (fi.Extension.Contains(extension))
            {
                files.Add(fi.Name);
            }
        }
        return files;
    }

    public static void rotate(int rotations, ref int a, ref int b, ref int c, ref int d)
    {
        while (rotations > 0)
        {
            int temp = a;
            a = b;
            b = c;
            c = d;
            d = temp;
            rotations--;
        }
    }
    public static bool intersectionRayTriangle(Ray r, MeshEdit.Triangle t)
    {
        // if (Vector3.Dot(r.direction, t.n) > 0) { return false; }
        Vector3 q = Vector3.Dot(t.a - r.origin, t.n) * t.n;
        q = r.origin - q;

        if (Vector3.Dot(Vector3.Cross((t.b - t.a), (q - t.a)), t.n) < 0) { return false; }
        if (Vector3.Dot(Vector3.Cross((t.c - t.b), (q - t.b)), t.n) < 0) { return false; }
        if (Vector3.Dot(Vector3.Cross((t.a - t.c), (q - t.c)), t.n) < 0) { return false; }

        return true;
    }
    public static bool rayIntersectsTriangle(Vector3 rayOrigin, Vector3 rayDirection, MeshEdit.Triangle inTriangle, ref Vector3 collisionPoint)
    {
        const float e = 0.0000001f;
        Vector3 vertex0 = inTriangle.a;
        Vector3 vertex1 = inTriangle.b;
        Vector3 vertex2 = inTriangle.c;
        Vector3 edge1, edge2, h, s, q;
        float a, f, u, v;
        edge1 = vertex1 - vertex0;
        edge2 = vertex2 - vertex0;
        h = Vector3.Cross(rayDirection, edge2);
        a = Vector3.Dot(edge1, h);

        if (a > -e && a < e)
        {
            return false;
        }
        f = 1.0f / a;
        s = rayOrigin - vertex0;
        u = f * (Vector3.Dot(s, h));
        if (u < 0.0 || u > 1.0)
        {
            return false;
        }
        q = Vector3.Cross(s, edge1);
        v = f * Vector3.Dot(rayDirection, q);
        if (v < 0.0 || u + v > 1.0)
        {
            return false;
        }

        float t = f * Vector3.Dot(edge2, q);
        if (t > e) // ray intersection
        {
            collisionPoint = rayOrigin + rayDirection * t;
            return true;
        }
        else // This means that there is a line intersection but not a ray intersection.
        {
            return false;
        }
    }

    private int findAdjacentTriPoint(int[] triangles, Vector3[] verts, ref int a, ref int b, ref int cOther, int checkIndex = 0)
    {
        int tri = 0;
        bool aFound = false;
        bool bFound = false;
        bool cFound = false;
        int c = -1;

        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i] == a)
            {
                aFound = true;
            }
            else if (triangles[i] == b)
            {
                bFound = true;
            }
            else if (triangles[i] != cOther)
            {
                cFound = true;
                c = triangles[i];
            }

            if (aFound && bFound && cFound)
            {
                break;
            }

            tri++;
            if (tri > 2)
            {
                tri = 0;

                aFound = false;
                bFound = false;
                cFound = false;

                c = -1;
            }
        }
        if (c == -1 && checkIndex < 2)
        {
            int temp = cOther;
            cOther = a;
            a = b;
            b = temp;

            c = findAdjacentTriPoint(triangles, verts, ref a, ref b, ref cOther, checkIndex + 1);
        }
        return c;
    }
    
    












    public static Mesh cube()
    {

        Mesh customCube = new Mesh();
        Vector3[] verts = {
            new Vector3(-0.5f, 0.5f, 0.5f), // Top
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),

            new Vector3(0.5f, -0.5f, 0.5f), // Bottom
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),

            new Vector3(0.5f, 0.5f, 0.5f), // Right
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),

            new Vector3(-0.5f, -0.5f, 0.5f), // Left
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),

            new Vector3(0.5f, 0.5f, 0.5f), // Front
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),

            new Vector3(-0.5f, 0.5f, -0.5f), // Back
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f)};

        int[] tris = {
            0, 1, 2,
            3, 2, 1,

            4, 5, 6,
            7, 6, 5,

            8, 9, 10,
            11, 10, 9,

            12, 13, 14,
            15, 14, 13,

            16, 17, 18,
            19, 18, 17,

            20, 21, 22,
            23, 22, 21};

        Color col = Color.white;

        Color[] colours = {
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col
            };

        Vector2 uv00 = new Vector2(0, 0);
        Vector2 uv01 = new Vector2(0, 1);
        Vector2 uv10 = new Vector2(1, 0);
        Vector2 uv11 = new Vector2(1, 1);
        Vector2[] uv = {
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11
            };

        customCube.vertices = verts;
        customCube.triangles = tris;
        customCube.colors = colours;
        customCube.uv = uv;
        customCube.RecalculateBounds();
        customCube.RecalculateNormals();

        return customCube;
    }
    public static Mesh plane()
    {

        Mesh customPlane = new Mesh();
        customPlane.name = "Plane";
        Vector3[] verts = {
            new Vector3(-0.5f, 0.0f, 0.5f), // Top
            new Vector3(0.5f, 0.0f, 0.5f),
            new Vector3(-0.5f, 0.0f, -0.5f),
            new Vector3(0.5f, 0.0f, -0.5f) };


        int[] tris = {
            0, 1, 2,
            3, 2, 1 };

        Color col = Color.white;

        Color[] colours = {
            col, col, col, col };

        Vector2 uv00 = new Vector2(0, 0);
        Vector2 uv01 = new Vector2(0, 1);
        Vector2 uv10 = new Vector2(1, 0);
        Vector2 uv11 = new Vector2(1, 1);
        Vector2[] uv = {
                uv00, uv01, uv10, uv11};

        customPlane.vertices = verts;
        customPlane.triangles = tris;
        customPlane.colors = colours;
        customPlane.uv = uv;
        customPlane.RecalculateBounds();
        customPlane.RecalculateNormals();

        return customPlane;
    }
    public static Mesh circle(int selectedCircle)
    {
        if (circleVerticesCount[selectedCircle] == 6)
        {
            return circle6();
        }
        else if (circleVerticesCount[selectedCircle] > 6 &&
                 circleVerticesCount[selectedCircle] % 2 == 0)
        {
            return circleDivTwo(circleVerticesCount[selectedCircle] / 2);
        }

        else return (circleDivTwo(16));
    }

    public static Mesh cylinder(int selectedCircle)
    {
        if (circleVerticesCount[selectedCircle] == 6)
        {
            return cylinder6();
        }
        else if (circleVerticesCount[selectedCircle] > 6 &&
                 circleVerticesCount[selectedCircle] % 2 == 0)
        {
            return cylinderDivTwo(circleVerticesCount[selectedCircle] / 2);
        }

        else return (cylinderDivTwo(8));
    }

    public static Mesh circle6()
    {

        Mesh customCircle6 = new Mesh();
        customCircle6.name = "Circle6";
        float x = 0.4330127019f;
        float y = 0.25f;

        bool isLookingDown = SceneView.lastActiveSceneView.camera.transform.forward.y > 0;

        if (isLookingDown)
        {

            Vector3[] verts = {
            new Vector3(0.0f, 0.0f, 0.5f), // A
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector3(x, 0.0f, y),
            new Vector3(x, 0.0f, -y),
            new Vector3(0.0f, 0.0f, 0.5f), // B
            new Vector3(-x, 0.0f, y),
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector3(-x, 0.0f, -y)
         };



            int[] tris = {
            0, 1, 2,
            3, 2, 1,

            4, 5, 6,
            7, 6, 5
        };

            Color col = Color.white;

            Color[] colours = {
            col, col, col, col,
            col, col, col, col};

            Vector2 uv00 = new Vector2(0, 0);
            Vector2 uv01 = new Vector2(0, 1);
            Vector2 uv10 = new Vector2(1, 0);
            Vector2 uv11 = new Vector2(1, 1);
            Vector2[] uv = {
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11};

            customCircle6.vertices = verts;
            customCircle6.triangles = tris;
            customCircle6.colors = colours;
            customCircle6.uv = uv;
            customCircle6.RecalculateBounds();
            customCircle6.RecalculateNormals();
        }
        else
        {
            Vector3[] verts = {
            new Vector3(0.0f, 0.0f, 0.5f), // A
            new Vector3(x, 0.0f, y),
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector3(x, 0.0f, -y),
            new Vector3(0.0f, 0.0f, 0.5f), // B
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector3(-x, 0.0f, y),
            new Vector3(-x, 0.0f, -y)
         };


            int[] tris = {
            0, 1, 2,
            3, 2, 1,

            4, 5, 6,
            7, 6, 5
        };

            Color col = Color.white;

            Color[] colours = {
            col, col, col, col,
            col, col, col, col};

            Vector2 uv00 = new Vector2(0, 0);
            Vector2 uv01 = new Vector2(0, 1);
            Vector2 uv10 = new Vector2(1, 0);
            Vector2 uv11 = new Vector2(1, 1);
            Vector2[] uv = {
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11};

            customCircle6.vertices = verts;
            customCircle6.triangles = tris;
            customCircle6.colors = colours;
            customCircle6.uv = uv;
            customCircle6.RecalculateBounds();
            customCircle6.RecalculateNormals();
        }
        return customCircle6;
    }

    public static Mesh cylinder6()
    {

        Mesh customCylinder6 = new Mesh();
        customCylinder6.name = "Cylinder6";
        float x = 0.4330127019f;
        float y = 0.25f;

        Vector3[] verts = {
            new Vector3(0.0f, 0.5f, -0.5f), // A Bottom
            new Vector3(0.0f, 0.5f, 0.5f),
            new Vector3(x, 0.5f, -y),
            new Vector3(x, 0.5f, y),
            new Vector3(0.0f, 0.5f, 0.5f), // B Bottom
            new Vector3(0.0f, 0.5f, -0.5f),
            new Vector3(-x, 0.5f, y),
            new Vector3(-x, 0.5f, -y),

            new Vector3(0.0f, -0.5f, 0.5f), // A Top
            new Vector3(0.0f, -0.5f, -0.5f),
            new Vector3(x, -0.5f, y),
            new Vector3(x, -0.5f, -y),
            new Vector3(0.0f, -0.5f, -0.5f), // B Top
            new Vector3(0.0f, -0.5f, 0.5f),
            new Vector3(-x, -0.5f, -y),
            new Vector3(-x, -0.5f, y),

            // Sides
            new Vector3(0.0f, 0.5f, 0.5f), // 1
            new Vector3(0.0f, -0.5f, 0.5f),
            new Vector3(x, 0.5f, y),
            new Vector3(x, -0.5f, y),

            new Vector3(x, 0.5f, y), // 2
            new Vector3(x, -0.5f, y),
            new Vector3(x, 0.5f, -y),
            new Vector3(x, -0.5f, -y),

            new Vector3(x, 0.5f, -y), // 3
            new Vector3(x, -0.5f, -y),
            new Vector3(0.0f, 0.5f, -0.5f),
            new Vector3(0.0f, -0.5f, -0.5f),

            new Vector3(0.0f, 0.5f, -0.5f), // 4
            new Vector3(0.0f, -0.5f, -0.5f),
            new Vector3(-x, 0.5f, -y),
            new Vector3(-x, -0.5f, -y),

            new Vector3(-x, 0.5f, -y), // 5
            new Vector3(-x, -0.5f, -y),
            new Vector3(-x, 0.5f, y),
            new Vector3(-x, -0.5f, y),

            new Vector3(-x, 0.5f, y), // 6
            new Vector3(-x, -0.5f, y),
            new Vector3(0.0f, 0.5f, 0.5f),
            new Vector3(0.0f, -0.5f, 0.5f)

        };


        int[] tris = {
            0, 1, 2,
            3, 2, 1,

            4, 5, 6,
            7, 6, 5,

            8, 9, 10,
            11, 10, 9,

            12, 13, 14,
            15, 14, 13,

            16, 17, 18,
            19, 18, 17,

            20, 21, 22,
            23, 22, 21,

            24, 25, 26,
            27, 26, 25,

            28, 29, 30,
            31, 30, 29,

            32, 33, 34,
            35, 34, 33,

            36, 37, 38,
            39, 38, 37
        };

        Color col = Color.white;

        Color[] colours = {
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col,
            col, col, col, col ,
            col, col, col, col };

        Vector2 uv00 = new Vector2(0, 0);
        Vector2 uv01 = new Vector2(0, 1);
        Vector2 uv10 = new Vector2(1, 0);
        Vector2 uv11 = new Vector2(1, 1);
        Vector2[] uv = {
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11 ,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11 ,
                uv00, uv01, uv10, uv11,
                uv00, uv01, uv10, uv11 ,
                uv00, uv01, uv10, uv11};

        customCylinder6.vertices = verts;
        customCylinder6.triangles = tris;
        customCylinder6.colors = colours;
        customCylinder6.uv = uv;
        customCylinder6.RecalculateBounds();
        customCylinder6.RecalculateNormals();

        return customCylinder6;
    }

    public static Mesh circleDivTwo(int steps)
    {

        Mesh customCircle = new Mesh();
        customCircle.name = "Circle" + (steps * 2);

        Color col = Color.white;

        Vector2 uv00 = new Vector2(0, 0);
        Vector2 uv01 = new Vector2(0, 1);
        Vector2 uv10 = new Vector2(1, 0);
        Vector2 uv11 = new Vector2(1, 1);

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color> colour = new List<Color>();
        List<Vector2> uv = new List<Vector2>();

        float degrees = Mathf.Deg2Rad * (360.0f / (4.0f * steps)) * 2;

        float x, y;

        bool isLookingDown = SceneView.lastActiveSceneView.camera.transform.forward.y > 0;

        for (int i = 0; i < steps * 2; i += 2)
        {
            if (isLookingDown)
            {
                x = Mathf.Cos(degrees * (i + 2)) * 0.5f;
                y = Mathf.Sin(degrees * (i + 2)) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));

                verts.Add(new Vector3(0.0f, 0.0f, 0.0f));

                x = Mathf.Cos(degrees * (i + 1)) * 0.5f;
                y = Mathf.Sin(degrees * (i + 1)) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));

                x = Mathf.Cos(degrees * i) * 0.5f;
                y = Mathf.Sin(degrees * i) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));
            }
            else
            {
                x = Mathf.Cos(degrees * (i + 2)) * 0.5f;
                y = Mathf.Sin(degrees * (i + 2)) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));

                x = Mathf.Cos(degrees * (i + 1)) * 0.5f;
                y = Mathf.Sin(degrees * (i + 1)) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));

                verts.Add(new Vector3(0.0f, 0.0f, 0.0f));

                x = Mathf.Cos(degrees * i) * 0.5f;
                y = Mathf.Sin(degrees * i) * 0.5f;
                verts.Add(new Vector3(x, 0.0f, y));
            }




            tris.Add(i * 2 + 0);
            tris.Add(i * 2 + 1);
            tris.Add(i * 2 + 2);

            tris.Add(i * 2 + 3);
            tris.Add(i * 2 + 2);
            tris.Add(i * 2 + 1);

            colour.Add(col);
            colour.Add(col);
            colour.Add(col);
            colour.Add(col);

            uv.Add(uv00);
            uv.Add(uv01);
            uv.Add(uv10);
            uv.Add(uv11);

        }

        customCircle.vertices = verts.ToArray();
        customCircle.triangles = tris.ToArray();
        customCircle.colors = colour.ToArray();
        customCircle.uv = uv.ToArray();
        customCircle.RecalculateBounds();
        customCircle.RecalculateNormals();

        return customCircle;
    }
    public static Mesh cylinderDivTwo(int steps)
    {

        Mesh customCylinder = new Mesh();
        customCylinder.name = "Cylinder" + (steps * 2);

        Color col = Color.white;

        Vector2 uv00 = new Vector2(0, 0);
        Vector2 uv01 = new Vector2(0, 1);
        Vector2 uv10 = new Vector2(1, 0);
        Vector2 uv11 = new Vector2(1, 1);

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color> colour = new List<Color>();
        List<Vector2> uv = new List<Vector2>();

        float degrees = Mathf.Deg2Rad * (360.0f / (4.0f * steps)) * 2;

        float x, y, z;
        for (int i = 0; i < steps * 2; i += 2)
        {
            // Top face
            z = 0.5f;
            x = Mathf.Cos(degrees * (i + 2)) * 0.5f;
            y = Mathf.Sin(degrees * (i + 2)) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            x = Mathf.Cos(degrees * (i + 1)) * 0.5f;
            y = Mathf.Sin(degrees * (i + 1)) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            verts.Add(new Vector3(0.0f, z, 0.0f));


            x = Mathf.Cos(degrees * i) * 0.5f;
            y = Mathf.Sin(degrees * i) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            tris.Add(i * 8 + 0);
            tris.Add(i * 8 + 1);
            tris.Add(i * 8 + 2);

            tris.Add(i * 8 + 3);
            tris.Add(i * 8 + 2);
            tris.Add(i * 8 + 1);

            colour.Add(col);
            colour.Add(col);
            colour.Add(col);
            colour.Add(col);

            uv.Add(uv00);
            uv.Add(uv01);
            uv.Add(uv10);
            uv.Add(uv11);

            // Bottom face
            z = -0.5f;
            x = Mathf.Cos(degrees * (i + 2)) * 0.5f;
            y = Mathf.Sin(degrees * (i + 2)) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            verts.Add(new Vector3(0.0f, z, 0.0f));

            x = Mathf.Cos(degrees * (i + 1)) * 0.5f;
            y = Mathf.Sin(degrees * (i + 1)) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            x = Mathf.Cos(degrees * i) * 0.5f;
            y = Mathf.Sin(degrees * i) * 0.5f;
            verts.Add(new Vector3(x, z, y));

            tris.Add(i * 8 + 4);
            tris.Add(i * 8 + 5);
            tris.Add(i * 8 + 6);

            tris.Add(i * 8 + 7);
            tris.Add(i * 8 + 6);
            tris.Add(i * 8 + 5);

            colour.Add(col);
            colour.Add(col);
            colour.Add(col);
            colour.Add(col);

            uv.Add(uv00);
            uv.Add(uv01);
            uv.Add(uv10);
            uv.Add(uv11);

            // Side1

            z = -0.5f;
            verts.Add(verts[i * 8 + 0]);
            verts.Add(verts[i * 8 + 4]);
            verts.Add(verts[i * 8 + 1]);
            verts.Add(verts[i * 8 + 6]);

            tris.Add(i * 8 + 8);
            tris.Add(i * 8 + 9);
            tris.Add(i * 8 + 10);

            tris.Add(i * 8 + 11);
            tris.Add(i * 8 + 10);
            tris.Add(i * 8 + 9);

            colour.Add(col);
            colour.Add(col);
            colour.Add(col);
            colour.Add(col);

            uv.Add(uv00);
            uv.Add(uv01);
            uv.Add(uv10);
            uv.Add(uv11);
            // Side2

            z = -0.5f;
            verts.Add(verts[i * 8 + 1]);
            verts.Add(verts[i * 8 + 6]);
            verts.Add(verts[i * 8 + 3]);
            verts.Add(verts[i * 8 + 7]);

            tris.Add(i * 8 + 12);
            tris.Add(i * 8 + 13);
            tris.Add(i * 8 + 14);

            tris.Add(i * 8 + 15);
            tris.Add(i * 8 + 14);
            tris.Add(i * 8 + 13);

            colour.Add(col);
            colour.Add(col);
            colour.Add(col);
            colour.Add(col);

            uv.Add(uv00);
            uv.Add(uv01);
            uv.Add(uv10);
            uv.Add(uv11);
        }


        customCylinder.vertices = verts.ToArray();
        customCylinder.triangles = tris.ToArray();
        customCylinder.colors = colour.ToArray();
        customCylinder.uv = uv.ToArray();
        customCylinder.RecalculateBounds();
        customCylinder.RecalculateNormals();

        return customCylinder;
    }








}
#endif