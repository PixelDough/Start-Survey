using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Globalization;

#if UNITY_EDITOR
public class MeshEditWindow : EditorWindow
{
    [MenuItem("Window/MeshEdit")]
    public static void showWindow()
    {
        MeshEditWindow.window = (MeshEditWindow)EditorWindow.GetWindow(typeof(MeshEditWindow));
    }

    public static MeshEditWindow window;

    void OnEnable()
    {
        MeshEditWindow.window = this;
        Undo.undoRedoPerformed += undoCallback;
    }
    
    void OnDisable()
    {
        Undo.undoRedoPerformed -= undoCallback;
    }

    private void undoCallback()
    {
        // Do not consolidate this version of the callback with the one in the scene interface. They look identical but they affect different instances of MeshEdit objects
        MeshEdit meshEdit = getActiveMeshEditObject();

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
    
    private MeshEdit getActiveMeshEditObject()
    {
        MeshEdit meshEdit = null;
        if (Selection.gameObjects != null)
        {
            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                MeshEdit me = Selection.gameObjects[i].GetComponent<MeshEdit>();

                if (me != null)
                {
                    if (meshEdit == null)
                    {
                        meshEdit = me;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        return meshEdit;
    }



    public static bool isActive()
    {
        if (window)
        {
            return true;
        }
        return false;
    }

    public void Update()
    {
        if (glLogo != null)
        {
            glLogo.updateEvents();
        }
        Repaint();
    }
    
    void OnSceneGUI()
    {

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

    }

    GLLogoDemo glLogo;

    List<MeshEdit> selectedMeshEditObjects;

    void OnGUI()
    {
        MeshEdit meshEdit = null;
        bool duplicateMeshEditObjectFound = false;

        if (Selection.gameObjects != null)
        {
            selectedMeshEditObjects = new List<MeshEdit>();
            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                MeshEdit me = Selection.gameObjects[i].GetComponent<MeshEdit>();

                if (me != null)
                {
                    selectedMeshEditObjects.Add(me);
                }

                if (!duplicateMeshEditObjectFound)
                {
                    if (me != null)
                    {
                        if (meshEdit == null)
                        {
                            meshEdit = me;
                        }
                        else
                        {
                            duplicateMeshEditObjectFound = true;
                            meshEdit = null;
                        }
                    }
                }
            }
        }


        if (!meshEdit)
        {
            if (glLogo == null)
            {
                glLogo = new GLLogoDemo();
            }
            
            float logoHeight = 90.0f;
            EditorGUILayout.BeginVertical();
            GUILayout.Space(17);
            Rect logoRect = GUILayoutUtility.GetRect(0, 128000, logoHeight, logoHeight);

            glLogo.setViewportSize(logoRect);

            GUI.BeginClip(logoRect);
            
            glLogo.glDrawInterface();

            GUI.EndClip();

            EditorGUILayout.EndVertical();




            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(10.0f);
            EditorStyles.label.wordWrap = true;

            if (duplicateMeshEditObjectFound)
            {
                EditorGUILayout.LabelField("Can not edit when multiple MeshEdit objects are selected.");
                GUILayout.Space(10.0f);
                if (selectedMeshEditObjects != null)
                {
                    for (int i =0; i < selectedMeshEditObjects.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();

                        GUILayout.Space(10.0f);
                        GUILayout.Label(selectedMeshEditObjects[i].gameObject.name);

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Edit", GUILayout.MaxWidth(80)))
                        {
                            Selection.objects = new GameObject[] { selectedMeshEditObjects[i].gameObject };
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("Welcome to MeshEdit! Select a MeshEdit object, or create one from scratch to begin.");

                GUILayout.Space(10);

                if (GUILayout.Button("Create a new model", GUILayout.MaxWidth(200)))
                {
                    MeshEdit.createCustomMesh();
                }
                GUILayout.Space(6);
                if (GUILayout.Button("Import an .obj file", GUILayout.MaxWidth(200)))
                {
                    importObj();
                }
                GUILayout.Space(6);
                if (GUILayout.Button("Show the tutorial", GUILayout.MaxWidth(200)))
                {
                    Application.OpenURL("www.jamierollo.com/MeshEdit/Tutorial/index.html");
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Version 0.4");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

        }
        else
        {
            gui(meshEdit);


            #region Keyboard Shortcuts
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Tab)
                {
                    this.Focus();
                    Event.current.Use();
                }
            }
            // CMD/CTRL can only be used during KeyUp or 
            if (Event.current.type == EventType.KeyUp)
            {
                switch (Event.current.keyCode)
                {
                    case (KeyCode.Tab):
                        if (Event.current.modifiers == EventModifiers.Control ||
                            Event.current.modifiers == EventModifiers.Command)
                        {
                            if (meshEdit.editMode == 1)
                            {
                                int newVertMode = 1 - meshEdit.vertMode;
                                meshEdit.setVertMode(newVertMode);
                                saveSettings(meshEdit, this);
                            }
                            else if (meshEdit.editMode == 3)
                            {
                                meshEdit.paintMode = 1 - meshEdit.paintMode;
                                saveSettings(meshEdit, this);
                            }
                        }
                        else
                        {

                            int newEditMode = 0;
                            if (shift)
                            {
                                newEditMode = meshEdit.editMode - 1;
                            }
                            else
                            {
                                newEditMode = meshEdit.editMode + 1;
                            }

                            if (newEditMode >= editModes.Length)
                            {
                                newEditMode = 0;
                            }
                            if (newEditMode < 0)
                            {
                                newEditMode = editModes.Length - 1;
                            }

                            meshEdit.updateEditMode(newEditMode);
                        }

                        this.Focus();
                        Event.current.Use();
                        break;
                }
            }
            else if (Event.current.type == EventType.KeyDown)
            {
                MeshEditSceneInterface.generalShortcutsOnKeydown(meshEdit, shift, ctrl, alt);
                /*
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
                    Event.current.Use();
                }
                #endregion
                switch (Event.current.keyCode)
                {
                    case (KeyCode.A):
                        if (meshEdit.editMode == 2)
                        {
                            if (meshEdit.editorInfo.selectedTile % meshEdit.editorInfo.tilesPerRow - 1 < 0)
                            {
                                meshEdit.editorInfo.selectedTile += meshEdit.editorInfo.tilesPerRow - 1;
                            }
                            else
                            {
                                meshEdit.editorInfo.selectedTile--;
                            }

                            Event.current.Use();
                        }
                        break;
                    case (KeyCode.D):
                        if (meshEdit.editMode == 2)
                        {
                            if ((meshEdit.editorInfo.selectedTile % meshEdit.editorInfo.tilesPerRow) + 1 >= meshEdit.editorInfo.tilesPerRow || meshEdit.editorInfo.selectedTile + 1 >= meshEdit.editorInfo.tileCount)
                            {
                                meshEdit.editorInfo.selectedTile = meshEdit.editorInfo.selectedTile - meshEdit.editorInfo.selectedTile % meshEdit.editorInfo.tilesPerRow;
                            }
                            else
                            {
                                meshEdit.editorInfo.selectedTile++;
                            }

                            Event.current.Use();
                        }
                        break;
                    case (KeyCode.W):
                        if (meshEdit.editMode == 2)
                        {
                            if (meshEdit.editorInfo.selectedTile - meshEdit.editorInfo.tilesPerRow < 0)
                            {
                                meshEdit.editorInfo.selectedTile = (meshEdit.editorInfo.tileCount / meshEdit.editorInfo.tilesPerRow) * meshEdit.editorInfo.tilesPerRow + meshEdit.editorInfo.selectedTile - meshEdit.editorInfo.tilesPerRow;
                            }
                            else
                            {
                                meshEdit.editorInfo.selectedTile -= meshEdit.editorInfo.tilesPerRow;
                            }

                            Event.current.Use();

                        }
                        break;
                    case (KeyCode.S):
                        if (meshEdit.editMode == 2)
                        {
                            if (meshEdit.editorInfo.selectedTile + meshEdit.editorInfo.tilesPerRow >= meshEdit.editorInfo.tileCount)
                            {
                                int x = meshEdit.editorInfo.selectedTile - (meshEdit.editorInfo.selectedTile / meshEdit.editorInfo.tilesPerRow) * meshEdit.editorInfo.tilesPerRow;

                                meshEdit.editorInfo.selectedTile = x;
                            }
                            else
                            {
                                meshEdit.editorInfo.selectedTile += meshEdit.editorInfo.tilesPerRow;
                            }


                            Event.current.Use();
                        }
                        break;

                    case (KeyCode.E):
                        if (meshEdit.editMode == 2)
                        {
                            meshEdit.editorInfo.tileDirection++;

                            Event.current.Use();
                        }
                        break;

                    case (KeyCode.Q):
                        if (meshEdit.editMode == 2)
                        {
                            meshEdit.editorInfo.tileDirection--;

                            Event.current.Use();
                        }
                        break;
                    case (KeyCode.Z):
                        if (Event.current.modifiers == EventModifiers.None)
                        {
                            meshEdit.isMeshTransparent = !meshEdit.isMeshTransparent;
                            meshEdit.GetComponent<MeshRenderer>().enabled = meshEdit.isMeshTransparent;

                            Event.current.Use();
                        }
                        break;
                    case (KeyCode.Delete):
                        if (meshEdit.editMode != 0)
                        {
                            Event.current.Use();
                        }
                        break;
                }
                */
            }

            #endregion

            if (meshEdit.editorInfo.tileDirection > 3)
            {
                meshEdit.editorInfo.tileDirection = 0;
            }
            else if (meshEdit.editorInfo.tileDirection < 0)
            {
                meshEdit.editorInfo.tileDirection = 3;
            }

        }

        lastMeshEditSelection = meshEdit;
    }

    [NonSerialized]
    MeshEdit lastMeshEditSelection;
    private void gui(MeshEdit meshEdit)
    {
        Event e = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Keyboard);

        if (meshEdit != null)
        {
            /*
            if (Event.current.type == EventType.ValidateCommand)
            {
                switch (Event.current.commandName)
                {
                    case "UndoRedoPerformed":
                        {
                            GameObject obj = Selection.activeTransform.gameObject;

                            meshEdit.pushNewGeometry();

                            updateSelectionArray(meshEdit, meshEdit.verts.Count);
                            break;
                        }
                }
            }
            */

            // Shortcut setup
            shift = Event.current.shift;
            ctrl = Event.current.control || Event.current.command;
            alt = Event.current.alt;
            //Debug.Log("ALT: " + alt.ToString() + ", CTRL: " + ctrl.ToString() + ", SHIFT: " + shift.ToString());

            if (lastMeshEditSelection != meshEdit)
            {
                loadSettings(meshEdit, this);
            }
            if (skin == null)
            {
                skin = AssetDatabase.LoadAssetAtPath<GUISkin>("Assets/MeshEdit/Resources/EditorTools.guiskin");
            }

            if (tilesetsAvailable == null || tilesetTexturesAvailable == null)
            {
                loadTilesets();
            }

            if (tileDirectionTexture == null ||
                tileDirectionTexture.Length < 4 ||
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

            guiMeshEditScrollPosition = EditorGUILayout.BeginScrollView(guiMeshEditScrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();


            guiHeader(meshEdit);

            if (meshEdit.editMode == 0)
            {
                guiDefault(meshEdit, controlId);
            }
            else if (meshEdit.editMode == 1)
            {
                guiMeshEditing(meshEdit, controlId);
            }
            else if (meshEdit.editMode == 2)
            {
                guiTextureTiling(meshEdit, controlId);

            }
            else if (meshEdit.editMode == 3)
            {
                guiColourEditing(meshEdit, controlId);
            }

            if (meshEdit.editMode != 1 || meshEdit.editorInfo.editOperation != MeshEdit.EditorInfo.MeshEditOperation.LoopCut)
            {

                meshEdit.facesCut = null;
                meshEdit.cutsAB = null;
                meshEdit.cutCount = 0;
            }

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }
    }

    GUISkin skin;

    Ray ray;
    
    MeshEdit.Triangle t = new MeshEdit.Triangle(Vector3.zero, Vector3.zero, Vector3.zero);

    GLTilesetInterface interfaceTilePicker;

    GLTileUVInterface interfaceTileUVs;

    Texture tileSelected;
    Texture2D texPixel;
    Texture[] tileDirectionTexture;


    string[] editModes = new string[] { "Default", "Mesh Edit", "Tile Edit", "Vertex Colour" };
    
    public static List<Tileset> tilesetsAvailable;
    public static List<string> tilesetTexturesAvailable;

    bool copyTexturesOnExport = true;
    bool copyScriptsToPrefab = true;
    bool copyMeshEditToPrefab = false;
    bool duplicateAssetsForPrefab = false;
    public bool recalculateNormalsOnTransform = true;

    bool showShortCuts = true;


    int circleVerticesSelected = 3;
    string[] circleVertices = { "6", "8", "10", "12", "16", "24", "32", "48", "64" };
    public static int[] circleVerticesCount = { 6, 8, 10, 12, 16, 24, 32, 48, 64 };

    public static string settingsPath = "/MeshEdit/MeshEdit Settings.txt";


    // Tileset UV Editor
    Vector2 scrollPosition = Vector2.zero;

    public bool useCustomUVCoords = true;
    bool snapUVsToPixel = true;
    bool showOnlySelectedVerts = false;

    Texture2D[,] tiles;
    Texture2D tileset;
    
    private void guiHeader(MeshEdit solid)
    {
        //GUI.skin = skin;
        EditorGUILayout.BeginVertical();

        GUI.color = Color.white;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);
        EditorGUILayout.BeginVertical();
        GUILayout.Space(6);

        int newEditMode = EditorGUILayout.Popup(solid.editMode, editModes);
        solid.updateEditMode(newEditMode);

        EditorGUILayout.Space();
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();

        drawSeparator();

        EditorGUILayout.EndVertical();

    }

    Vector2 guiMeshEditScrollPosition;
    private void guiMeshEditing(MeshEdit meshEdit, int controlId)
    {
        EditorGUILayout.BeginVertical();

        // Dropdown selector
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);
        EditorGUILayout.BeginVertical();
        GUILayout.Space(6);

        if (meshEdit.vertMode != 0 && meshEdit.vertMode != 1)
        {
            meshEdit.vertMode = 0;
        }
        int oldVertMode = meshEdit.vertMode;
        EditorGUILayout.BeginHorizontal();

        // newVertMode = EditorGUILayout.Popup(meshEdit.vertMode, new string[] { "Vertices", "Faces" }, GUILayout.Width(100));
        meshEdit.vertMode = GUILayout.Toolbar(meshEdit.vertMode, new string[] { "Vert Mode", "Face Mode" }, GUILayout.Height(30));
       
        EditorGUILayout.EndHorizontal();
        if (oldVertMode != meshEdit.vertMode)
        {
            meshEdit.setVertMode(meshEdit.vertMode);

            saveSettings(meshEdit, this);
        }
        if (meshEdit.selectedFaces != null && meshEdit.selectedVerts != null)
        {
            GUILayout.Label("Faces: " + meshEdit.selectedFaces.Length + " Verts: " + meshEdit.selectedVerts.Length);
        }
        if (meshEdit.debugMode != MeshEdit.DebugShowInfo.Off)
        {
            GUILayout.Label("Quads: " + (meshEdit.quads == null ? "null" : meshEdit.quads.Count.ToString()));
        }
        if (meshEdit.debugMode != MeshEdit.DebugShowInfo.Off)
        {
            GUILayout.Label("ConnectedVerts: " + (meshEdit.connectedVerts == null ? "null" : meshEdit.connectedVerts.Count.ToString()));
        }
        if (meshEdit.debugMode != MeshEdit.DebugShowInfo.Off)
        {
            GUILayout.Label("AdjacentFaces: " + (meshEdit.adjacentFaces == null ? "null" : meshEdit.adjacentFaces.Count.ToString()));
        }

        if (meshEdit.debugMode != MeshEdit.DebugShowInfo.Off)
        {
            GUILayout.Label("HalfEdges: " + (meshEdit.halfEdges == null ? "null" : meshEdit.halfEdges.Count.ToString()));
        }

        guiNormalControls(meshEdit);

        drawSeparator();

        bool wasChanged = false;
        Vector3 centerPos = SceneView.lastActiveSceneView.pivot;

        int faceCount = meshEdit.faceNormals.Count;
        int vertCount = meshEdit.verts.Count;

        meshEdit.editorInfo.showAddPrimitiveControls = EditorGUILayout.Foldout(meshEdit.editorInfo.showAddPrimitiveControls, "Add Primitives");
        if (meshEdit.editorInfo.showAddPrimitiveControls)
        {
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Plane", GUILayout.MaxWidth(100)))
            {
                Undo.RegisterCompleteObjectUndo(meshEdit, "Add Plane");
                meshEdit.addMesh(plane(), centerPos);

                wasChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cube", GUILayout.MaxWidth(100)))
            {
                Undo.RegisterCompleteObjectUndo(meshEdit, "Add Cube");
                meshEdit.addMesh(cube(), centerPos);

                wasChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Circle", GUILayout.MaxWidth(100)))
            {
                Undo.RegisterCompleteObjectUndo(meshEdit, "Add Circle");
                meshEdit.addMesh(circle(circleVerticesSelected), centerPos);

                wasChanged = true;
            }
            GUILayout.Space(6);
            EditorGUILayout.LabelField("Sides: ", GUILayout.Width(40));
            circleVerticesSelected = EditorGUILayout.Popup(circleVerticesSelected, circleVertices, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cylinder", GUILayout.MaxWidth(100)))
            {
                Undo.RegisterCompleteObjectUndo(meshEdit, "Add Circle");
                meshEdit.addMesh(cylinder(circleVerticesSelected), centerPos);

                wasChanged = true;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        drawSeparator();

        meshEdit.editorInfo.showGeneralOperations = EditorGUILayout.Foldout(meshEdit.editorInfo.showGeneralOperations, "General Operations");
        if (meshEdit.editorInfo.showGeneralOperations)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Cut", GUILayout.MaxWidth(60)))
            {
                cut(meshEdit);
            }
            if (GUILayout.Button("Copy", GUILayout.MaxWidth(60)))
            {
                copy(meshEdit);
            }
            if (GUILayout.Button("Paste", GUILayout.MaxWidth(60)))
            {
                paste(meshEdit);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Mark sharp", GUILayout.MaxWidth(100)))
            {
                meshEdit.setSelectedEdgesSharp(meshEdit.selectedVerts, meshEdit.selectedFaces);
                meshEdit.recalculateNormals(meshEdit.mesh);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(6);

            #region Help Mode
            if (showShortCuts)
            {

                GUILayout.Space(6);

                EditorStyles.label.wordWrap = true;

                if (meshEdit.editorInfo.editOperation == MeshEdit.EditorInfo.MeshEditOperation.SelectCircle)
                {
                    EditorGUILayout.LabelField("Mode: Circle Select");
                    if (meshEdit.vertMode == 0)
                    {
                        EditorGUILayout.LabelField("LMB - Select a vert");
                        EditorGUILayout.LabelField("MMB - Deselect a vert");
                    }
                    else
                    {
                        EditorGUILayout.LabelField("LMB - Select a face");
                        EditorGUILayout.LabelField("MMB - Deselect a face");
                    }
                    EditorGUILayout.LabelField("C/LMB - Exit circle-select mode");
                    EditorGUILayout.LabelField("Scroll - Change circle size");
                }
                else if (meshEdit.editorInfo.editOperation == MeshEdit.EditorInfo.MeshEditOperation.LoopCut)
                {
                    EditorGUILayout.LabelField("Mode: Loop Cut");
                    EditorGUILayout.LabelField("LMB - Exit loop-cut mode");
                    EditorGUILayout.LabelField("RMB - Cut");
                    EditorGUILayout.LabelField("Scroll - Change number of cuts");
                }
                else if (meshEdit.editorInfo.transformMode > 0)
                {
                    if (meshEdit.editorInfo.transformMode == 1)
                    {
                        EditorGUILayout.LabelField("Mode: Transform - Move");
                    }
                    else if (meshEdit.editorInfo.transformMode == 2)
                    {
                        EditorGUILayout.LabelField("Mode: Transform - Rotate");
                    }
                    else if (meshEdit.editorInfo.transformMode == 3)
                    {
                        EditorGUILayout.LabelField("Mode: Transform - Scale");
                    }

                    EditorGUILayout.LabelField("LMB - Confirm transform");
                    EditorGUILayout.LabelField("RMB - Undo transform");
                    EditorGUILayout.LabelField("X, Y, Z - Constrain transform to axis");
                }
                else
                {
                    EditorGUILayout.LabelField("Mode: Default");
                    if (meshEdit.vertMode == 0)
                    {
                        EditorGUILayout.LabelField("RMB - Select a vert");
                    }
                    else
                    {
                        EditorGUILayout.LabelField("RMB - Select a face");
                    }
                    EditorGUILayout.LabelField("C - Activate circle-select mode");
                    EditorGUILayout.LabelField("A - Select all");
                    EditorGUILayout.LabelField("G - Move selection");
                    EditorGUILayout.LabelField("R - Rotate selection");
                    EditorGUILayout.LabelField("S - Scale selection");
                    EditorGUILayout.LabelField("E - Extrude selected section");
                    EditorGUILayout.LabelField("Shift + R - Activate loop-cut mode");
                    EditorGUILayout.LabelField("F - If four vertices are selected, create a new face between them");
                    EditorGUILayout.LabelField("F - Flip selected faces");
                    EditorGUILayout.LabelField("D - Flip saddling on selected faces");
                    EditorGUILayout.LabelField("Z - Toggle mesh visibility");
                    EditorGUILayout.LabelField("L - Select all connected faces under the mouse");
                    EditorGUILayout.LabelField("U - Unwrap the selection");

                }

                GUILayout.Space(6);
            }
            #endregion

        }

        if (wasChanged)
        {
            updateSelectionArray(meshEdit, meshEdit.verts.Count);

            for (int i = 0; i < meshEdit.faceNormals.Count; i++)
            {
                meshEdit.selectedFaces[i] = (i >= faceCount);
            }
            for (int i = 0; i < meshEdit.verts.Count; i++)
            {
                meshEdit.selectedVerts[i] = (i >= vertCount);
            }
        }

        EditorGUILayout.EndVertical();

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        // Catches clicks on box
        //GUI.Button(editRect, "", GUIStyle.none);
        drawSeparator();

        #region UV Controls

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);
        meshEdit.editorInfo.showUVEditor = EditorGUILayout.Foldout(meshEdit.editorInfo.showUVEditor, "UV Editor");
        if (meshEdit.editorInfo.showUVEditor)
        {
            GUILayout.Space(20);
            meshEdit.editorInfo.showUVEditorSettings = EditorGUILayout.Foldout(meshEdit.editorInfo.showUVEditorSettings, "UV Settings");
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        if (meshEdit.editorInfo.showUVEditor)
        {
            if (meshEdit.editorInfo.showUVEditorSettings)
            {
                bool isSnapOptionActive = true;
                if (backgroundTexture == null)
                {
                    snapUVsToPixel = false;
                    isSnapOptionActive = false;
                }

                drawSeparator();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(6);
                EditorGUILayout.BeginVertical(GUILayout.MaxWidth(200));
                #region Left side settings
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Pin", GUILayout.MaxWidth(50)))
                {
                    if (meshEdit.vertMode == 1)
                    {
                        cleanSelectedUVs(meshEdit);
                    }
                    pin(meshEdit, meshEdit.selectedUVs);
                }
                if (GUILayout.Button("Unpin", GUILayout.MaxWidth(50)))
                {
                    if (meshEdit.vertMode == 1)
                    {
                        cleanSelectedUVs(meshEdit);
                    }
                    unpin(meshEdit, meshEdit.selectedUVs);
                }
                EditorGUILayout.EndHorizontal();
                drawSubsectionSeparator();
                EditorGUILayout.BeginHorizontal();
                
                bool temp = showOnlySelectedVerts;
                showOnlySelectedVerts = GUILayout.Toggle(showOnlySelectedVerts, "Only show selected faces");
                if (temp != snapUVsToPixel)
                {
                    saveSettings(meshEdit, this);
                }

                temp = snapUVsToPixel;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(!isSnapOptionActive);
                snapUVsToPixel = GUILayout.Toggle(snapUVsToPixel, "Snap UVs to pixel");
                EditorGUI.EndDisabledGroup();
                if (temp != snapUVsToPixel)
                {
                    saveSettings(meshEdit, this);
                }

                EditorGUILayout.EndHorizontal();
                #endregion
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.BeginVertical(GUILayout.MaxWidth(160));
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Export UV map", GUILayout.MaxWidth(120)))
                {
                    exportUVMapAsTexture(meshEdit);
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 60;
                
                meshEdit.editorInfo.uvMapExportTextureWidth = EditorGUILayout.IntField("Width", meshEdit.editorInfo.uvMapExportTextureWidth, GUILayout.ExpandWidth(false));
                if (meshEdit.editorInfo.uvMapExportTextureWidth < 2)
                {
                    meshEdit.editorInfo.uvMapExportTextureWidth = 2;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                meshEdit.editorInfo.uvMapExportTextureHeight = EditorGUILayout.IntField("Height", meshEdit.editorInfo.uvMapExportTextureHeight, GUILayout.ExpandWidth(false));
                if (meshEdit.editorInfo.uvMapExportTextureHeight < 2)
                {
                    meshEdit.editorInfo.uvMapExportTextureHeight = 2;
                }
                GUILayout.FlexibleSpace();

                EditorGUILayout.EndHorizontal();
                /*EditorGUILayout.BeginHorizontal();
                meshEdit.editorInfo.uvMapUnwrapIslandPadding = EditorGUILayout.FloatField("Unwrap padding", meshEdit.editorInfo.uvMapUnwrapIslandPadding);

                EditorGUILayout.EndHorizontal();*/
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }


            Rect clipRect = GUILayoutUtility.GetRect(0, 10000, 300, 6000);

            glDrawUVArea(clipRect, meshEdit);
            if (Event.current.isMouse || Event.current.isScrollWheel || Event.current.isKey)
            {
                updateUVEvents(meshEdit);
            }

            EditorGUILayout.BeginVertical();
            GUILayout.Space(clipRect.height);
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(6);

            if (GUILayout.Button(new GUIContent("[ ]", "Center the UV View"), GUILayout.Width(30)))
            {
                resetView(meshEdit);
                Repaint();
            }
            GUILayout.FlexibleSpace();
            int oldUVEditorSelectedMaterialTexture = uvEditorSelectedMaterialTexture;

            string[] textureNames = getListOfMaterialTextureNames(meshEdit, true);
            if (textureNames != null && textureNames.Length > 0)
            {
                uvEditorSelectedMaterialTexture = EditorGUILayout.Popup(uvEditorSelectedMaterialTexture + 1, textureNames, GUILayout.MaxWidth(200)) - 1;

                if (uvEditorSelectedMaterialTexture != oldUVEditorSelectedMaterialTexture)
                {
                    if (uvEditorSelectedMaterialTexture == -1)
                    {
                        backgroundTexture = null;
                        textureMat = null;
                    }
                    else
                    {
                        backgroundTexture = getListOfMaterialTextures(meshEdit)[uvEditorSelectedMaterialTexture];
                        textureMat = null;
                        createTextureMat(meshEdit);
                    }
                }
            }
            else
            {
                
                backgroundTexture = null;
                textureMat = null;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(6);
            
            if (GUILayout.Button("Mark seam", GUILayout.MaxWidth(100)))
            {
                meshEdit.setSelectedEdgesSeam(meshEdit.selectedVerts, meshEdit.selectedFaces);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Unwrap selection", GUILayout.MaxWidth(130)))
            {
                if (meshEdit.vertMode == 0)
                {
                    meshEdit.selectionConvertToFaces();
                }
                else
                {
                    meshEdit.selectionConvertToVerts();
                }
                meshEdit.lscmUnwrap(meshEdit.selectedVerts, meshEdit.selectedFaces);
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(8);
            EditorGUILayout.EndVertical();
        }
        #endregion
    }

    private void exportUVMapAsTexture(MeshEdit meshEdit)
    {
        string path = EditorUtility.SaveFilePanel(
            "Export UV map as a .png file",
            "",
            meshEdit.gameObject.name + "-UVs.png",
            "png");

        if (path.Length > 0)
        {
            int pageWidth = meshEdit.editorInfo.uvMapExportTextureWidth;
            int pageHeight = meshEdit.editorInfo.uvMapExportTextureHeight;

            Texture2D tex = new Texture2D(pageWidth, pageHeight, TextureFormat.RGBA32, false);

            Color32[] pixels = new Color32[pageWidth * pageHeight];

            Color bgColour = Color.white;
            Color lineColour = new Color(0.6f, 0.6f, 0.6f, 1.0f); 
            Color outlineColour = new Color(0.4f, 0.4f, 0.4f, 1.0f);
            bool isOutline = false;
            Color drawColour = lineColour;

            #region Line drawing and pixel setting
            for (int i= 0; i < pageWidth * pageHeight; i++)
            {
                pixels[i] = bgColour;
            }
            bool[] hasEdgeBeenDrawn = new bool[meshEdit.selectedFaces.Length * 4];
            for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
            {
                if (isUVFaceAvailableForSelection(meshEdit, i))
                {
                    for (int ii = 0; ii < 4; ii++)
                    {
                        if (!hasEdgeBeenDrawn[i * 4 + ii])
                        {
                            int adjacentFace = meshEdit.adjacentFaces[i].list[ii];

                            hasEdgeBeenDrawn[i * 4 + ii] = true;

                            if (adjacentFace == -1 || !areFacesTrulyAdjacentByUvs(meshEdit, i, adjacentFace) || !isUVFaceAvailableForSelection(meshEdit, adjacentFace))
                            {
                                drawColour = outlineColour;
                                isOutline = true;
                            }
                            else
                            {
                                drawColour = lineColour;
                                isOutline = false;
                                hasEdgeBeenDrawn[adjacentFace * 4 + meshEdit.adjacentFaces[adjacentFace].list.IndexOf(i)] = true;
                            }

                            // Bresenham line
                            int uvA = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[ii * 2 + 0]];
                            int uvB = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[ii * 2 + 1]];
                            Vector2 a = meshEdit.mesh.uv[uvA];
                            Vector2 b = meshEdit.mesh.uv[uvB];
                            
                            int aX = Mathf.RoundToInt(a.x * pageWidth);
                            int aY = Mathf.RoundToInt(a.y * pageHeight);
                            int bX = Mathf.RoundToInt(b.x * pageWidth);
                            int bY = Mathf.RoundToInt(b.y * pageHeight);

                            int deltaX = Mathf.Abs(bX - aX);
                            int deltaY = Mathf.Abs(bY - aY);

                            if (deltaX > 0 || deltaY > 0)
                            {
                                if (deltaY < deltaX)
                                {
                                    if (aX > bX)
                                    {
                                        int temp = aX;
                                        aX = bX;
                                        bX = temp;
                                        temp = aY;
                                        aY = bY;
                                        bY = temp;
                                    }
                                    // Low line
                                    deltaX = bX - aX;
                                    deltaY = bY - aY;
                                    int yi = 1;
                                    if (deltaY < 0)
                                    {
                                        yi = -1;
                                        deltaY *= -1;
                                    }
                                    int d = 2 * deltaY - deltaX;
                                    int y = aY;
                                    for (int x = aX; x <= bX; x++)
                                    {
                                        if (isOutline)
                                        {
                                            for (int yy = -1; yy < 2; yy++)
                                            {
                                                for (int xx = -1; xx < 2; xx++)
                                                {
                                                    if (x + xx>= 0 && x + xx < pageWidth &&
                                                        y + yy >= 0 && y  + yy< pageHeight)
                                                    {
                                                        pixels[x + xx + (y + yy) * pageWidth] = drawColour;
                                                    }
                                                }
                                            }
                                        }
                                        else if (x >= 0 && x < pageWidth &&
                                            y >= 0 && y < pageHeight)
                                        {
                                            pixels[x + y * pageWidth] = drawColour;
                                        }

                                        if (d > 0)
                                        {
                                            y += yi;
                                            d -= 2 * deltaX;
                                        }
                                        d += 2 * deltaY;
                                    }
                                }
                                else
                                {
                                    if (aY > bY)
                                    {
                                        int temp = aX;
                                        aX = bX;
                                        bX = temp;
                                        temp = aY;
                                        aY = bY;
                                        bY = temp;
                                    }

                                    // High line
                                    deltaX = bX - aX;
                                    deltaY = bY - aY;
                                    int xi = 1;
                                    if (deltaX < 0)
                                    {
                                        xi = -1;
                                        deltaX *= -1;
                                    }
                                    int d = 2 * deltaX - deltaY;
                                    int x = aX;
                                    for (int y = aY; y <= bY; y++)
                                    {
                                        if (isOutline)
                                        {
                                            for (int yy = -1; yy < 2; yy++)
                                            {
                                                for (int xx = -1; xx < 2; xx++)
                                                {
                                                    if (x + xx >= 0 && x + xx < pageWidth &&
                                                        y + yy >= 0 && y + yy < pageHeight)
                                                    {
                                                        pixels[x + xx + (y + yy) * pageWidth] = drawColour;
                                                    }
                                                }
                                            }
                                        }
                                        else if (x >= 0 && x < pageWidth &&
                                            y >= 0 && y < pageHeight)
                                        {
                                            pixels[x + y * pageWidth] = drawColour;
                                        }


                                        if (d > 0)
                                        {
                                            x = x + xi;
                                            d -= 2 * deltaY;
                                        }
                                        d += 2 * deltaX;
                                    }
                                }
                                
                            }
                        }
                    }
                }
            }
            #endregion

            tex.SetPixels32(pixels);

            byte[] bytes = tex.EncodeToPNG();

            File.WriteAllBytes(path, bytes);

            Debug.Log("UV Map exported to: " + path);
        }
    }

    private string[] getListOfMaterialTextureNames(MeshEdit meshEdit, bool includeNone = true)
    {
        List<string> materialNames = new List<string>();

        if (includeNone)
        {
            materialNames.Add("None");
        }

        for (int i = 0; i < meshEdit.materials.Count; i++)
        {
            Texture tex = meshEdit.materials[i].GetTexture("_MainTex");
            if (tex != null)
            {
                materialNames.Add(tex.name);
            }
        }

        return materialNames.ToArray();
    }

    private Texture[] getListOfMaterialTextures(MeshEdit meshEdit)
    {
        List<Texture> materialNames = new List<Texture>();

        for (int i = 0; i < meshEdit.materials.Count; i++)
        {
            Texture tex = meshEdit.materials[i].GetTexture("_MainTex");
            if (tex != null)
            {
                materialNames.Add(tex);
            }
        }

        return materialNames.ToArray();
    }

    private void guiNormalControls(MeshEdit meshEdit)
    {
        drawSeparator();
        meshEdit.editorInfo.showNormalControls = EditorGUILayout.Foldout(meshEdit.editorInfo.showNormalControls, "Normals");

        if (meshEdit.editorInfo.showNormalControls)
        {
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Show Normals: ", GUILayout.Width(90));
            meshEdit.drawNormals = EditorGUILayout.Popup(meshEdit.drawNormals, new string[] { "None", "Faces", "Verts", "Triangles" }, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Normal Length: " + meshEdit.normalLength.ToString("f2"), GUILayout.Width(130));
            meshEdit.normalLength = GUILayout.HorizontalSlider(meshEdit.normalLength, 0.01f, 5.0f);// GUILayout.Width(200));
            
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                new GUIContent(
                    "Smooth Angle: " + meshEdit.smoothnessThreshold.ToString("f2"),
                    "If the angle between two normals is less than the \"Smooth Angle,\" they will be merged to create a single smooth normal.") , GUILayout.Width(130));
            float oldSmoothnessThreshold = meshEdit.smoothnessThreshold;
            meshEdit.smoothnessThreshold = GUILayout.HorizontalSlider(meshEdit.smoothnessThreshold, 0.0f, 360.0f);//, GUILayout.Width(200));
            if (oldSmoothnessThreshold != meshEdit.smoothnessThreshold)
            {
                meshEdit.recalculateNormals(meshEdit.mesh);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Recalculate normals", GUI.skin.GetStyle("Button"), GUILayout.Width(140)))
            {
                meshEdit.recalculateNormals(meshEdit.mesh);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            bool temp = recalculateNormalsOnTransform;

            recalculateNormalsOnTransform = GUILayout.Toggle(recalculateNormalsOnTransform, 
                new GUIContent(
                    " Recalculate while editing", 
                    "While rotating, scaling or moving the model, recalculate affected normals each time the mouse is moved. Turning this off will make MeshEdit run faster.") 
                );

            if (temp != recalculateNormalsOnTransform)
            {
                saveSettings(meshEdit, this);
            }

            EditorGUILayout.EndHorizontal();
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="solid"></param>
    /// <param name="controlId"></param>

    int materialButtonSelected = -1;
    bool wasAddMaterialSelected = false;
    private void guiTextureTiling(MeshEdit solid, int controlId)
    {
        EditorGUILayout.BeginVertical();
        // Dropdown selector

        if (interfaceTilePicker == null)
        {
            interfaceTilePicker = new GLTilesetInterface();
        }

        if (interfaceTileUVs == null)
        {
            interfaceTileUVs = new GLTileUVInterface();
        }

        if (solid.hasDefaultUVs)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);

            EditorStyles.label.wordWrap = true;
            EditorGUILayout.LabelField("This mesh has been imported with its own texture and UV coordinates. Once you apply a tileset to this mesh, you will have to manually apply the original texture to revert it, and the current UV data may be changed irreperably.");

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            EditorGUILayout.LabelField("Click this button to convert the UVs to a tileset.");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            if (GUILayout.Button("Convert to Tileset", GUI.skin.GetStyle("Button"), GUILayout.Width(160)))
            {
                solid.hasDefaultUVs = false;
                loadTilesets();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(16);
            EditorGUILayout.EndVertical();
        }
        else
        {
            #region Material selection foldout
            EditorGUILayout.BeginVertical();

            solid.editorInfo.showMaterialPicker = EditorGUILayout.Foldout(solid.editorInfo.showMaterialPicker, "Material Picker");

            

            if (solid.editorInfo.showMaterialPicker)
            {
                EditorGUILayout.BeginVertical();
                int oldSelectedMaterial = solid.selectedMaterial;
                string[] matsAvailable = new string[solid.materials.Count + 1];
                for (int i = 0; i < solid.materials.Count; i++)
                {
                    matsAvailable[i] = i.ToString();
                }

                matsAvailable[solid.materials.Count] = "+";
                int buttonWidth = 64;
                int gapWidth = 4;

                EditorGUILayout.BeginHorizontal();
                int rowWidth = gapWidth;
                int maxRowWidth = (int)position.width;
                for (int i = 0; i < matsAvailable.Length - 1; i++)
                {
                    #region Button with hover help

                    EditorGUILayout.BeginVertical();
                    Texture tex = AssetPreview.GetAssetPreview(solid.materials[i]);

                    if (tex == null || tex.width == 0)
                    {
                        continue;
                        //tex = texUnknown;
                    }
                    EditorGUI.BeginDisabledGroup(solid.selectedMaterial == i);

                    if (GUILayout.Button(new GUIContent(tex, solid.materials[i].name), GUILayout.Width(buttonWidth), GUILayout.Height(buttonWidth)))
                    {
                        materialButtonSelected = i;
                    }
                    GUILayout.Label(new GUIContent(solid.materials[i].name, solid.materials[i].name), GUILayout.Width(buttonWidth));

                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndVertical();

                    rowWidth += buttonWidth + gapWidth;
                    if (rowWidth > maxRowWidth - (buttonWidth + gapWidth + 12))
                    {
                        rowWidth = gapWidth;
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                    #endregion
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                GUILayout.Space(6);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Add Material", GUILayout.Width(120)))
                {
                    wasAddMaterialSelected = true;
                }
                if (GUILayout.Button("- Delete Material", GUILayout.Width(120)))
                {

                    if (solid.removeMaterial(solid.selectedMaterial))
                    {
                        materialButtonSelected = 0;
                    }
                    else
                    {
                        Debug.Log("You cannot have less than one material on a mesh.");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }


            EditorGUILayout.EndVertical();

            if (materialButtonSelected >= 0)
            {
                Undo.RegisterCompleteObjectUndo(solid, "Delete Material");

                solid.selectedMaterial = materialButtonSelected;
                materialButtonSelected = -1;
                // If the tileset being loaded has had its name changed, this will throw an error
                if (solid.materialUVMap[solid.selectedMaterial] >= 0)
                {
                    solid.selectedTileset = tilesetTexturesAvailable.IndexOf(solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].name);
                }
                else
                {
                    solid.selectedTileset = -1;
                    setTextureFromCurrentMaterial(solid);
                }
                solid.isTilesetRefreshRequired = true;


            }
            if (wasAddMaterialSelected)
            {
                Undo.RegisterCompleteObjectUndo(solid, "Add Material");

                wasAddMaterialSelected = false;
                solid.addMaterial();
            }

            #endregion


            drawSeparator();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            EditorGUILayout.BeginVertical();

            GUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();

            #region Tileset popup & behaviour on change


            string[] tilesetSelection = new string[tilesetTexturesAvailable.Count + 1];
            tilesetSelection[0] = "Use a Texture";
            for (int i = 0; i < tilesetSelection.Length - 1; i++)
            {
                tilesetSelection[i + 1] = tilesetTexturesAvailable[i];
            }

            int oldTileset = solid.selectedTileset;
            solid.selectedTileset = EditorGUILayout.Popup(solid.selectedTileset + 1, tilesetSelection) - 1;


            if (solid.selectedTileset != oldTileset || tiles == null || solid.isTilesetRefreshRequired)
            {
                if (solid.selectedTileset < 0 )
                {
                    solid.materialUVMap[solid.selectedMaterial] = -1;
                    setTextureFromCurrentMaterial(solid);
                    solid.isTilesetRefreshRequired = false;

                    if (solid.selectedTexture != null)
                    {
                        MeshEdit.EditorInfo.CustomTilesetEditorSettings setting = solid.editorInfo.customTilesetSettings[solid.selectedMaterial];

                        solid.editorInfo.texturePage = solid.selectedTexture;

                        updateCustomTilesetDimensions(solid);

                        interfaceTilePicker.areaWidth = solid.selectedTexture.width - (int)(setting.tileOutline * 2 - interfaceTilePicker.tileSeparation * 2) * solid.editorInfo.tilesPerRow;
                        interfaceTilePicker.areaHeight = solid.selectedTexture.height - (int)(setting.tileOutline * 2 - interfaceTilePicker.tileSeparation * 2) * solid.editorInfo.tilesPerColumn;


                        Renderer r = solid.gameObject.GetComponent<Renderer>();
                        if (r != null)
                        {
                            try
                            {
                                r.sharedMaterials[solid.selectedMaterial].SetTexture("_MainTex", solid.editorInfo.texturePage);
                            }
                            catch (Exception e)
                            {
                                Debug.Log(e.Message);
                                Debug.Log("Cannot set texture. The material must have a texture property named \"_MainTex\"");
                            }
                        }

                        solid.pushUVData();
                    }
                }
                else if (solid.selectedTileset >= 0)
                {
                    if (tilesetsAvailable != null && tilesetsAvailable.Count > 0)
                    {
                        loadTilesFromTileset(solid, solid.selectedTileset);

                        solid.isTilesetRefreshRequired = false;

                        string name = tilesetsAvailable[solid.selectedTileset].tilesetName;
                        int indexOfCurrent = solid.uvMaps.FindIndex(map => map.name == name);

                        // If we can't find the selected tileset, it's created now
                        if (indexOfCurrent == -1)
                        {
                            solid.uvMaps.Add(
                                new MeshEdit.UVData(
                                    tilesetsAvailable[solid.selectedTileset].tilesetName,
                                    tilesetsAvailable[solid.selectedTileset].texturePage.width,
                                    tilesetsAvailable[solid.selectedTileset].texturePage.height,
                                    tilesetsAvailable[solid.selectedTileset].tileWidth,
                                    tilesetsAvailable[solid.selectedTileset].tileHeight,
                                    tilesetsAvailable[solid.selectedTileset].tileOutline,
                                    solid.verts.Count,
                                    solid.defaultUVs));

                            indexOfCurrent = solid.uvMaps.Count - 1;
                        }
                        else
                        {
                            solid.materialUVMap[solid.selectedMaterial] = indexOfCurrent;

                            //solid.uvMaps[solid.currentUVMap].resizeUVSpace(texturePage.width, texturePage.height, tileWidth, tileHeight, tilesetsAvailable[solid.currentUVMap]);
                        }
                        
                        solid.materialUVMap[solid.selectedMaterial] = indexOfCurrent;

                        Renderer r = solid.gameObject.GetComponent<Renderer>();
                        if (r != null)
                        {
                            try
                            {
                                r.sharedMaterials[solid.selectedMaterial].SetTexture("_MainTex", solid.editorInfo.texturePage);
                            }
                            catch (Exception e)
                            {
                                Debug.Log(e.Message);
                                Debug.Log("Cannot set texture. The material must have a texture property named \"_MainTex\"");
                            }
                        }

                        //updateMeshUVs(solid);
                        solid.pushUVData();

                        solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].tileWidth = solid.editorInfo.tileWidth;
                        solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].tileHeight = solid.editorInfo.tileHeight;
                        solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].tileOutline = solid.editorInfo.tileOutline;
                    }
                }
            }
            #endregion

            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
            
            #region Free texture tileset interface
            if (solid.selectedTileset < 0)
            {

                if (solid.selectedTexture == null)
                {
                    Renderer r = solid.gameObject.GetComponent<Renderer>();
                    if (r != null)
                    {
                        if (r.sharedMaterials != null)
                        {
                            Texture tex = r.sharedMaterials[solid.selectedMaterial].GetTexture("_MainTex");

                            if (tex != null )
                            {
                                solid.selectedTexture = tex;
                            }
                        }
                    }
                }
                
                EditorGUILayout.BeginHorizontal();

                Texture newTexture = (Texture)EditorGUILayout.ObjectField("Main Texture", solid.selectedTexture, typeof(Texture), false);

                if (newTexture != solid.selectedTexture )
                {
                    solid.selectedTexture = newTexture;

                    MeshEdit.EditorInfo.CustomTilesetEditorSettings setting = solid.editorInfo.customTilesetSettings[solid.selectedMaterial];
                    
                    interfaceTilePicker.texture = solid.selectedTexture;
                    interfaceTileUVs.texture = solid.selectedTexture;



                    solid.editorInfo.texturePage = solid.selectedTexture;

                    interfaceTilePicker.areaWidth = solid.selectedTexture.width - (int)(setting.tileOutline * 2 - interfaceTilePicker.tileSeparation * 2) * solid.editorInfo.tilesPerRow;
                    interfaceTilePicker.areaHeight = solid.selectedTexture.height - (int)(setting.tileOutline * 2 - interfaceTilePicker.tileSeparation * 2) * solid.editorInfo.tilesPerColumn;
                    
                    updateCustomTilesetDimensions(solid);


                    if (solid.selectedTexture != null)
                    {
                        Renderer r = solid.gameObject.GetComponent<Renderer>();
                        if (r != null)
                        {
                            if (r.sharedMaterials != null)
                            {
                                r.sharedMaterials[solid.selectedMaterial].SetTexture("_MainTex", solid.selectedTexture);
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (solid.selectedTexture != null)
                {
                    // Tile width
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Tile Width ", GUILayout.Width(70));

                    MeshEdit.EditorInfo.CustomTilesetEditorSettings setting = solid.editorInfo.customTilesetSettings[solid.selectedMaterial];
                    
                    int oldTileWidth = setting.tileWidth;

                    setting.tileWidth = EditorGUILayout.IntField(setting.tileWidth, GUILayout.Width(50));
                    GUILayout.Space(40);

                    if (GUILayout.Button("/ 2", GUILayout.Width(40)))
                    {
                        setting.tileWidth /= 2;
                    }
                    if (GUILayout.Button("x 2", GUILayout.Width(40)))
                    {
                        setting.tileWidth *= 2;
                    }
                    if (setting.tileWidth < 1)
                    {
                        setting.tileWidth = 1;
                    }

                    if (setting.tileWidth != oldTileWidth)
                    {
                        updateCustomTilesetDimensions(solid);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();



                    // Tile height
                    EditorGUILayout.BeginHorizontal();

                    int oldTileHeight = setting.tileHeight;
                    EditorGUILayout.LabelField("Tile Height ", GUILayout.Width(70));
                    setting.tileHeight = EditorGUILayout.IntField(setting.tileHeight, GUILayout.Width(50));

                    GUILayout.Space(40);
                    if (GUILayout.Button("/ 2", GUILayout.Width(40)))
                    {
                        setting.tileHeight /= 2;
                    }
                    if (GUILayout.Button("x 2", GUILayout.Width(40)))
                    {
                        setting.tileHeight *= 2;
                    }
                    if (setting.tileHeight < 1)
                    {
                        setting.tileHeight = 1;
                    }
                    if (setting.tileHeight != oldTileHeight)
                    {
                        updateCustomTilesetDimensions(solid);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    // Tile outline

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Spacing ", GUILayout.Width(70));
                    int oldTileOutline = setting.tileOutline;
                    setting.tileOutline = EditorGUILayout.IntField(setting.tileOutline, GUILayout.Width(50));
                    GUILayout.Space(40);
                    if (GUILayout.Button("- 1", GUILayout.Width(40)))
                    {
                        setting.tileOutline -= 1;
                        if (setting.tileOutline < 0)
                        {
                            setting.tileOutline = 0;
                        }
                    }
                    if (GUILayout.Button("+ 1", GUILayout.Width(40)))
                    {
                        setting.tileOutline += 1;
                    }

                    if (setting.tileOutline != oldTileOutline)
                    {
                        updateCustomTilesetDimensions(solid);
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("No texture available in the selected material. It must be named \"_MainTex\" in the shader you're using.");
                }
            }



            #endregion





            GUILayout.Space(6);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            if (solid.selectedTileset >= 0 || (solid.selectedTileset == -1 && solid.selectedTexture != null))
            {
                #region  Tile picker
                Rect clipRect = GUILayoutUtility.GetRect(0, float.MaxValue, 256, 256);

                //Debug.Log("Event" + Event.current.type + ": " + clipRect.ToString());
                if (Event.current.type == EventType.Repaint)
                {
                    if (solid.selectedTileset >= 0)
                    {
                        Tileset ts = tilesetsAvailable[solid.selectedTileset];

                        interfaceTilePicker.texture = ts.textureEditorTileset;
                        interfaceTilePicker.createTextureMaterial();
                    }
                    else if (solid.selectedTexture)
                    {
                        interfaceTilePicker.texture = solid.selectedTexture;
                    }

                    interfaceTilePicker.setViewportSize(clipRect);
                }

                GUI.BeginClip(new Rect(clipRect.x, clipRect.y, clipRect.width, clipRect.height));

                if (Event.current.type == EventType.Repaint)
                {

                    if (solid.selectedTileset >= 0)
                    {
                        Tileset ts = tilesetsAvailable[solid.selectedTileset];
                        interfaceTilePicker.areaWidth = ts.textureEditorTileset.width + (int)interfaceTilePicker.tileSeparation * 2 * solid.editorInfo.tilesPerRow;
                        interfaceTilePicker.areaHeight = ts.textureEditorTileset.height + (int)interfaceTilePicker.tileSeparation * 2 * solid.editorInfo.tilesPerColumn;
                        interfaceTilePicker.gridUnitW = ((float)solid.editorInfo.tileWidth) / ts.textureEditorTileset.width;
                        interfaceTilePicker.gridUnitH = ((float)solid.editorInfo.tileHeight) / ts.textureEditorTileset.height;


                        interfaceTilePicker.tilesPerRow = solid.editorInfo.tilesPerRow;
                        interfaceTilePicker.tilesPerColumn = solid.editorInfo.tilesPerColumn;
                        interfaceTilePicker.tileSeparation = 0;// solid.editorInfo.tileOutline;
                        interfaceTilePicker.tileWidth = ts.tileWidth;
                        interfaceTilePicker.tileHeight = ts.tileHeight;



                        interfaceTilePicker.updateViewMatrix();

                        interfaceTilePicker.glBeginInterface();

                        interfaceTilePicker.glDrawInterface(solid, solid.editorInfo.selectedTile);
                        // glDrawTileset(solid, ts.texturePage, clipRect, ts.tileWidth, ts.tileHeight, ts.tileOutline, ts.pageWidth, ts.pageHeight, solid.editorInfo.selectedTile);

                        interfaceTilePicker.glEndInterface();
                    }
                    else if (solid.selectedTexture)
                    {

                        interfaceTilePicker.areaWidth = solid.selectedTexture.width;// - (solid.editorInfo.tileOutline * 2 - (int)interfaceTilePicker.tileSeparation * 2) * solid.editorInfo.tilesPerRow;
                        interfaceTilePicker.areaHeight = solid.selectedTexture.height;// - (solid.editorInfo.tileOutline * 2 - (int)interfaceTilePicker.tileSeparation * 2) * solid.editorInfo.tilesPerColumn;
                        interfaceTilePicker.gridUnitW = 1.0f / solid.editorInfo.tilesPerRow + (int)interfaceTilePicker.tileSeparation * 2;
                        interfaceTilePicker.gridUnitH = 1.0f / solid.editorInfo.tilesPerColumn + (int)interfaceTilePicker.tileSeparation * 2;

                        interfaceTilePicker.tilesPerRow = solid.editorInfo.tilesPerRow;
                        interfaceTilePicker.tilesPerColumn = solid.editorInfo.tilesPerColumn;
                        interfaceTilePicker.tileSeparation = solid.editorInfo.tileOutline;
                        interfaceTilePicker.tileWidth = solid.editorInfo.tileWidth;
                        interfaceTilePicker.tileHeight = solid.editorInfo.tileHeight;



                        interfaceTilePicker.updateViewMatrix();

                        interfaceTilePicker.glBeginInterface();

                        interfaceTilePicker.glDrawInterface(solid, solid.editorInfo.selectedTile);

                        interfaceTilePicker.glEndInterface();
                    }
                }
                else
                {

                    if (false && solid.selectedTileset >= 0)
                    {
                    }
                }
                GUI.EndClip();

                wantsMouseMove = true;
                interfaceTilePicker.updateEvents(solid);


                #endregion
                GUILayout.Space(6);
                #region  Quad editor
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                Rect quadEditorClipRect = GUILayoutUtility.GetRect(200, 200);//, 256.0f, 256.0f);
                quadEditorClipRect.width = 200;

                //Debug.Log("Event" + Event.current.type + ": " + clipRect.ToString());
                if (Event.current.type == EventType.Repaint)
                {
                    if (solid.selectedTileset >= 0)
                    {
                        Tileset ts = tilesetsAvailable[solid.selectedTileset];

                        interfaceTileUVs.texture = ts.textureEditorTileset;
                        interfaceTileUVs.createTextureMaterial();

                    }
                    else if (solid.selectedTexture)
                    {
                        interfaceTileUVs.texture = solid.selectedTexture;
                    }

                    interfaceTileUVs.tile = tileRectangle(solid, true);

                    interfaceTileUVs.setViewportSize(quadEditorClipRect);
                }

                GUI.BeginClip(new Rect(quadEditorClipRect.xMin, quadEditorClipRect.yMin, quadEditorClipRect.width, quadEditorClipRect.height));

                if (Event.current.type == EventType.Repaint)
                {
                    if (solid.selectedTileset >= 0)
                    {
                        Tileset ts = tilesetsAvailable[solid.selectedTileset];
                        interfaceTileUVs.areaWidth = solid.editorInfo.tileWidth;// *(int)quadEditorClipRect.width;
                        interfaceTileUVs.areaHeight = solid.editorInfo.tileHeight;// * (int)quadEditorClipRect.width;

                        interfaceTileUVs.updateViewMatrix();

                        interfaceTileUVs.glBeginInterface();

                        interfaceTileUVs.glDrawInterface(solid, solid.editorInfo.selectedTile);


                        interfaceTileUVs.glEndInterface();
                    }
                    else if (solid.selectedTexture)
                    {

                        interfaceTileUVs.areaWidth = solid.editorInfo.tileWidth;// * (int)quadEditorClipRect.width;
                        interfaceTileUVs.areaHeight = solid.editorInfo.tileHeight;// * (int)quadEditorClipRect.width;


                        interfaceTileUVs.updateViewMatrix();

                        interfaceTileUVs.glBeginInterface();

                        interfaceTileUVs.glDrawInterface(solid, solid.editorInfo.selectedTile);

                        interfaceTileUVs.glEndInterface();
                    }
                }
                else
                {

                    if (false && solid.selectedTileset >= 0)
                    {
                    }
                }
                GUI.EndClip();

                wantsMouseMove = true;
                interfaceTileUVs.updateEvents(solid, snapUVsToPixel);


                EditorGUILayout.EndVertical();

                GUILayout.Space(6);

                EditorGUILayout.BeginVertical();

                //GUI.skin.box.normal.background = skin.box.normal.background;
                if (GUILayout.Button("Reset", GUI.skin.GetStyle("Button"), GUILayout.Width(100)))
                {
                    interfaceTileUVs.uvCoord = new List<Vector2>(new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f) });
                    solid.editorInfo.uvCoordSnapped = new List<Vector2>(new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f) });
                }
                bool temp = useCustomUVCoords;
                useCustomUVCoords = GUILayout.Toggle(useCustomUVCoords, "Use custom UVs");
                if (temp != useCustomUVCoords)
                {
                    saveSettings(solid, this);
                }
                temp = snapUVsToPixel;
                snapUVsToPixel = GUILayout.Toggle(snapUVsToPixel, "Snap to pixel");
                if (temp != snapUVsToPixel)
                {
                    saveSettings(solid, this);
                }
                GUILayout.Space(8);

                if (GUILayout.Button(tileDirectionTexture[solid.editorInfo.tileDirection], GUILayout.Width(32), GUILayout.Height(32)))
                {
                    solid.editorInfo.tileDirection++;
                }
                GUILayout.Label("Move UVs");
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("-90", GUI.skin.GetStyle("Button"), GUILayout.Width(50)))
                {
                    interfaceTileUVs.rotateUVCoordsCCW();
                    interfaceTileUVs.snapUVCoords(solid, snapUVsToPixel);
                }
                if (GUILayout.Button("+90", GUI.skin.GetStyle("Button"), GUILayout.Width(50)))
                {
                    interfaceTileUVs.rotateUVCoordsCW();
                    interfaceTileUVs.snapUVCoords(solid, snapUVsToPixel);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                #endregion
            }

        }

        if (showShortCuts)
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("W, A, S, D - Select a tile from the tileset");
            EditorGUILayout.LabelField("Q, E - Rotate the selected tile");
            EditorGUILayout.LabelField("Alt+LMB - Copy UVs from selected face");
            EditorGUILayout.LabelField("Ctrl/CMD+LMB - Estimate UVs from selected face");
        }

        EditorGUILayout.EndVertical();
    }


    private void setTextureFromCurrentMaterial(MeshEdit solid)
    {
        try
        {
            solid.selectedTexture = solid.GetComponent<Renderer>().sharedMaterials[solid.selectedMaterial].GetTexture("_MainTex");
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            solid.selectedTexture = null;
        }
    }

    private Rect tileRectangle(MeshEdit solid, bool getDisplayRectangle )
    {
        if (solid.editorInfo.selectedTile < 0 ||
            solid.editorInfo.selectedTile >= solid.editorInfo.tileCount)
        {
            return Rect.zero;
        }

        int tx = solid.editorInfo.selectedTile % solid.editorInfo.tilesPerRow;
        int ty = solid.editorInfo.selectedTile / solid.editorInfo.tilesPerRow;

        if (solid.selectedTileset >= 0)
        {
            Tileset ts = tilesetsAvailable[solid.selectedTileset];

            float xMin = tx * (ts.tileWidth + ts.tileOutline * 2) + ts.tileOutline;
            float width = ts.tileWidth;
            float yMin = ty * (ts.tileHeight + ts.tileOutline * 2) + ts.tileOutline;
            float height = ts.tileHeight;
            xMin /= ts.texturePage.width;
            width /= ts.texturePage.width;
            yMin /= ts.texturePage.height;
            height /= ts.texturePage.height;

            if (getDisplayRectangle)
            {

                 xMin = tx * (ts.tileWidth);
                 width = ts.tileWidth;
                 yMin = ty * (ts.tileHeight);
                 height = ts.tileHeight;
                xMin /= ts.textureEditorTileset.width;
                width /= ts.textureEditorTileset.width;
                yMin /= ts.textureEditorTileset.height;
                height /= ts.textureEditorTileset.height;

            }
            return new Rect(xMin, yMin, width, height);
        }
        else if (solid.selectedTexture )
        {
            float xMin = tx * (solid.editorInfo.tileWidth + solid.editorInfo.tileOutline * 2) + solid.editorInfo.tileOutline;
            float width = solid.editorInfo.tileWidth;
            float yMin = ty * (solid.editorInfo.tileHeight + solid.editorInfo.tileOutline * 2) + solid.editorInfo.tileOutline;
            float height = solid.editorInfo.tileHeight;

            xMin /= solid.selectedTexture.width;
            width /= solid.selectedTexture.width;
            yMin /= solid.selectedTexture.height;
            height /= solid.selectedTexture.height;

            return new Rect(xMin, yMin, width, height);
        }

        return Rect.zero;
    }


    private static void updateCustomTilesetDimensions(MeshEdit solid)
    {
        MeshEdit.EditorInfo.CustomTilesetEditorSettings setting = solid.editorInfo.customTilesetSettings[solid.selectedMaterial];

        solid.editorInfo.tilesPerRow = (solid.selectedTexture.width / (setting.tileWidth + setting.tileOutline * 2));
        solid.editorInfo.tilesPerColumn = (solid.selectedTexture.height / (setting.tileHeight + setting.tileOutline * 2));
        solid.editorInfo.tileCount = solid.editorInfo.tilesPerColumn * solid.editorInfo.tilesPerRow;

        if (solid.editorInfo.tilesPerRow <= 0)
        {
            if (setting.tileWidth > solid.selectedTexture.width)
            {
                setting.tileWidth = solid.selectedTexture.width - setting.tileOutline * 2;
            }
            else if (setting.tileWidth + setting.tileOutline * 2 > solid.selectedTexture.width)
            {
                setting.tileOutline = (solid.selectedTexture.width - setting.tileWidth) / 2;
            }
            solid.editorInfo.tilesPerRow = 1;
        }

        if (solid.editorInfo.tilesPerColumn <= 0)
        {
            if (setting.tileHeight > solid.selectedTexture.height)
            {
                setting.tileHeight = solid.selectedTexture.height - setting.tileOutline * 2;
            }
            else if (setting.tileHeight + setting.tileOutline * 2 > solid.selectedTexture.height)
            {
                setting.tileOutline = (solid.selectedTexture.height - setting.tileHeight) / 2;
            }
            solid.editorInfo.tilesPerColumn = 1;
        }

        solid.editorInfo.tileWidth = setting.tileWidth;
        solid.editorInfo.tileHeight = setting.tileHeight;
        solid.editorInfo.tileOutline = setting.tileOutline;

        int maximumSelection = solid.editorInfo.tilesPerRow * solid.editorInfo.tilesPerColumn;
        if (solid.editorInfo.selectedTile >= maximumSelection)
        {
            solid.editorInfo.selectedTile = maximumSelection - 1;
        }
    }

    private void guiColourEditing(MeshEdit solid, int controlId)
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();

        GUILayout.Space(6);

        int oldPaintMode = solid.paintMode;

        GUILayout.BeginHorizontal();
        solid.paintMode = GUILayout.Toolbar(solid.paintMode, new string[] { "Vert Mode", "Face Mode" }, GUILayout.Height(30)); 
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(3);

        if (oldPaintMode != solid.paintMode)
        {
            saveSettings(solid, this);
        }

        drawSeparator();

        GUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);
        GUILayout.Label("Paint Colour: ", GUILayout.Width(80));


        solid.editorInfo.paintColour = EditorGUILayout.ColorField(solid.editorInfo.paintColour, GUILayout.Width(100));

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);

        if (solid.editorInfo.colourHistory == null)
        {
            solid.editorInfo.colourHistory = new List<Color>();
        }
        skin.button.normal.background = null;
        for (int i = 0; i < solid.editorInfo.maxColours; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            Color c = Color.black;

            int count = 0;
            for (int ii = 0; (ii ) * 32 < position.width && ii + i < solid.editorInfo.maxColours; ii++)
            {
                count++;

                if (i + ii < solid.editorInfo.colourHistory.Count)
                {
                    c = solid.editorInfo.colourHistory[i + ii];
                }
                else
                {
                    c = Color.black;
                }

                GUI.color = c;

                if (GUILayout.Button("",  GUILayout.Width(22)))
                {
                    solid.editorInfo.paintColour = c;
                }
            }

            i += count;

            GUI.color = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        if (showShortCuts)
        {
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(6);
            EditorGUILayout.BeginVertical();
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
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
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

    private void guiDefault(MeshEdit solid, int controlId)
    {
        //Rect editRect = 
        EditorGUILayout.BeginVertical();
        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);

        #region Button - Recenter Pivot
        if (GUILayout.Button("Re-Center pivot", GUILayout.MaxWidth(120)))
        {
            solid.recenterPivot(new Vector3(0.5f, 0.5f, 0.5f));
        }
        #endregion
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);
        drawSeparator();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);
        #region Button - Export to OBJ
        if (GUILayout.Button("Export to .obj", GUILayout.MaxWidth(100)))
        {
            exportObj(solid);
        }
        GUILayout.Space(8);
        //GUILayout.Label("Copy textures on export");
        #endregion
        #region Button - Import from .obj
        if (GUILayout.Button("Import .obj", GUILayout.MaxWidth(100)))
        {
            importObj();
        }
        #endregion
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);

        bool oldCopyTextureOnExport = copyTexturesOnExport;
        copyTexturesOnExport = GUILayout.Toggle(copyTexturesOnExport, "Copy textures on export", GUILayout.Height(24));
        if (oldCopyTextureOnExport != copyTexturesOnExport)
        {
            saveSettings(solid, this);
        }
        EditorGUILayout.EndHorizontal();

        drawSeparator();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(6);
        #region Button - Export to Prefab
        if (GUILayout.Button("Save as a prefab", GUILayout.MaxWidth(140)))
        {
            exportPrefab(solid);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();

        bool oldValue = copyScriptsToPrefab;
        copyScriptsToPrefab = GUILayout.Toggle(copyScriptsToPrefab, "Copy scripts", GUILayout.Height(24));
        if (oldValue != copyScriptsToPrefab)
        {
            saveSettings(solid, this);
        }
        oldValue = copyMeshEditToPrefab;
        copyMeshEditToPrefab = GUILayout.Toggle(copyMeshEditToPrefab, "Copy MeshEdit", GUILayout.Height(24));
        if (oldValue != copyMeshEditToPrefab)
        {
            saveSettings(solid, this);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        /*
        EditorGUILayout.BeginHorizontal();
        oldValue = duplicateAssetsForPrefab;
        duplicateAssetsForPrefab = GUILayout.Toggle(duplicateAssetsForPrefab, "Copy Textures and materials", GUILayout.Height(24));
        if (oldValue != duplicateAssetsForPrefab)
        {
            saveSettings(solid, this);
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();*/
        #endregion


        drawSeparator();

        GUILayout.Space(16);

        if (GUILayout.Button("Show the tutorial", GUILayout.MaxWidth(180)))
        {
            Application.OpenURL("www.jamierollo.com/MeshEdit/Tutorial/index.html");
        }

        bool oldHelpMode = showShortCuts;
        showShortCuts = GUILayout.Toggle(showShortCuts, "Show shortcuts", GUILayout.Height(24));
        if (oldHelpMode != showShortCuts)
        {
            saveSettings(solid, this);
        }


        if (showShortCuts)
        {
            GUILayout.Label("Shortcuts for all edit modes:");
            GUILayout.Label("Tab - Switch edit mode");
            GUILayout.Label("Numpad5 - Toggle ortho perspective");
            GUILayout.Label("Numpad7 - View top");
            GUILayout.Label("Numpad1 - View front");
            GUILayout.Label("Numpad3 - View side");
        }

        GUILayout.Space(6);
        if (solid.debugMode != MeshEdit.DebugShowInfo.Off)
        {
            #region Button - Rebuild verts
            if (GUILayout.Button("Rebuild Connected Verts", GUILayout.Width(180)))
            {
                solid.getConnectedVertsBasedOnPosition();
            }
            if (GUILayout.Button("Rebuild Adjacent Faces", GUILayout.Width(180)))
            {
                solid.rebuildAdjacentFaces();
            }
            if (GUILayout.Button("Rebuild Half Edges", GUILayout.Width(180)))
            {
                solid.defineAllHalfEdges();
            }
            GUILayout.Space(6);
        }
        #endregion
        EditorGUILayout.EndVertical();
        // Catches clicks on box
        // GUI.Button(editRect, "", GUIStyle.none);

    }

    private void drawSeparator()
    {

        Rect rect = EditorGUILayout.GetControlRect(false, 1);

        rect.height = 1;
        rect.x = 0;
        rect.width = position.width;

        float f = 116.0f / 255.0f;

        Color lineColour = new Color(f, f, f, 1);
        EditorGUI.DrawRect(rect, lineColour);
    }


    private void drawSubsectionSeparator()
    {

        Rect rect = EditorGUILayout.GetControlRect(false, 1);

        rect.height = 1;
        rect.x = 0;

        float f = 116.0f / 255.0f;

        Color lineColour = new Color(f, f, f, 1);
        EditorGUI.DrawRect(rect, lineColour);
    }

    private void updateTilesetEvents(MeshEdit solid, Texture texture, Rect clipRect, int tilesPerRow, int tilesPerColumn, float tileWidth, float tileHeight, float tileOutline, float pageWidth, float pageHeight, float areaWidth, float areaHeight, float zoom, int selectedTile, float tileSeparation)
    {

    }

    void drawBox(int w, int h)
    {
        Texture2D pixel = new Texture2D(1, 1);

        pixel.SetPixel(0, 0, Color.black);
        pixel.Apply();

        GUIStyle style = new GUIStyle();
        style.normal.background = pixel;
        //GUI.skin.box.normal.background = pixel;
        GUILayout.Box(GUIContent.none, style, GUILayout.Width(w), GUILayout.Height(h));
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
        {
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
        }

        solid.endTimer();
    }

    private void pin(MeshEdit meshEdit, bool[] selectedUVs)
    {
        if (meshEdit.pinnedUVs == null || meshEdit.pinnedUVs.Length != meshEdit.mesh.uv.Length)
        {
            meshEdit.pinnedUVs = new bool[meshEdit.mesh.uv.Length];
        }

        for (int i = 0; i < selectedUVs.Length; i++)
        {
            if (selectedUVs[i])
            {
                meshEdit.pinnedUVs[i] = true;
            }
        }
    }

    private void unpin(MeshEdit meshEdit, bool[] selectedUVs)
    {
        if (meshEdit.pinnedUVs == null || meshEdit.pinnedUVs.Length != meshEdit.mesh.uv.Length)
        {
            meshEdit.pinnedUVs = new bool[meshEdit.mesh.uv.Length];
        }

        for (int i = 0; i < selectedUVs.Length; i++)
        {
            if (selectedUVs[i])
            {
                meshEdit.pinnedUVs[i] = false;

            }
        }
    }

    public static Vector2 rotate(Vector2 point, float degree, Vector2 origin)
    {
        point -= origin;
        degree = Mathf.Deg2Rad * degree;

        float cos = Mathf.Cos(degree);
        float sin = Mathf.Sin(degree);

        return new Vector2(
            (cos * point.x - sin * point.y),
            (sin * point.x + cos * point.y)) + origin;
    }

    public void setUVs(MeshEdit meshEdit, Vector2[] newUVs)
    {

        if (snapUVsToPixel && backgroundTexture != null)
        {

            // Constrain & snap
            float pixelUnitWidth = 1.0f / backgroundTexture.width;
            float pixelUnitHeight = 1.0f / backgroundTexture.height;

            if (snapUVsToPixel)
            {
                for (int i = 0; i < newUVs.Length; i++)
                {
                    newUVs[i] = new Vector2(
                        Mathf.Round(newUVs[i].x / pixelUnitWidth) * pixelUnitWidth,
                        Mathf.Round(newUVs[i].y / pixelUnitHeight) * pixelUnitHeight);

                }
            }
        }
        /*
        meshEdit.uvMaps[meshEdit.materialUVMap[meshEdit.selectedMaterial]].uvs = newUVs;

        meshEdit.mesh.uv = newUVs;
        */
        // Converted to:
        meshEdit.setUVsThroughUVEditor(newUVs);
        //

        meshEdit.pushLocalMeshToGameObject();
    }

    public Vector2 uvPointToClipSpace(Vector2 point, Vector2 offset, float texWidth, float texHeight, float zoom)
    {
        point.y = 1.0f - point.y;
        point.x *= texWidth;
        point.y *= texHeight;

        point *= zoom;

        point.x += offset.x;
        point.y -= offset.y;

        //point += uvGUIPosition;

        return point;
    }










    private void exportObj(MeshEdit solid)
    {
        // TODO: Allow the user toggle applying modifiers to exported objects
        string path = EditorUtility.SaveFilePanel(
            "Export model as a .obj file",
            "",
            solid.gameObject.name + ".obj",
            "obj");

        if (path.Length > 0)
        {
            createObjFile(solid, path, true, copyTexturesOnExport);
        }


    }

    private void createObjFile(MeshEdit solid, string path, bool createMtlFile, bool copyTextures)
    {
        int slashIndex = path.LastIndexOf('/') + 1;
        string name = path.Remove(0, slashIndex);
        name = name.Remove(name.Length - 4);

        path = path.Remove(slashIndex);

        Debug.Log("Exporting " + solid.gameObject.name + " to " + path + name + ".obj");

        List<string> matNames = new List<string>();
        List<int> matIndexes = new List<int>();

        string data;

        bool fileUsesMaterials = true;
        
        
            #region .mtl
            int validMatCount = 0;
            data = "";
        if (solid.materialUVMap != null)
        {
            for (int i = 0; i < solid.materials.Count; i++)
            {
                Material mat = solid.materials[i];
                string matName = "Material" + validMatCount;
                string texturePath = "";
                string textureName = "";

                bool hasTexture = true;

                if (solid.materialUVMap[i] >= 0)
                {
                    texturePath = Application.dataPath + TilesetManager.locationOfTilesetPagesWindows + "/" + solid.uvMaps[solid.materialUVMap[i]].name + ".png";
                    textureName = solid.uvMaps[solid.materialUVMap[i]].name.Replace(' ', '_');
                }
                else
                {
                    try
                    {
                        Texture t = solid.materials[i].GetTexture("_MainTex");
                        texturePath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/Assets")) + "/" + AssetDatabase.GetAssetPath(t);
                        textureName = t.name.Replace(' ', '_');
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.Message);
                        hasTexture = false;
                    }
                }

                validMatCount++;
                matNames.Add(matName);
                matIndexes.Add(i);

                // TODO Repeat for multiple materials
                data += "newmtl " + matName + Environment.NewLine;


                Color ambientCol = Color.white;
                if (mat.HasProperty("_Color"))
                {
                    ambientCol = mat.GetColor("_Color");
                }

                Color specularCol = Color.white;
                if (mat.HasProperty("_SpecColor"))
                {
                    specularCol = mat.GetColor("_SpecColor");
                }

                float specularExp = 0.0f;
                if (mat.HasProperty("_Shininess"))
                {
                    specularExp = mat.GetFloat("_Shininess");
                }

                data += "Ka " + ambientCol.r + " " + ambientCol.g + " " + ambientCol.b + Environment.NewLine;
                data += "Kd 1.000 1.000 1.000" + Environment.NewLine;
                data += "Ks " + specularCol.r + " " + specularCol.g + " " + specularCol.b + Environment.NewLine;
                data += "Ns " + specularExp + Environment.NewLine;
                data += "d 1.0" + Environment.NewLine;
                data += "Tr 1.0" + Environment.NewLine;
                data += "s 0.0" + Environment.NewLine;
                data += "illum 0" + Environment.NewLine;

                if (hasTexture)
                {
                    data += "map_Kd " + textureName + ".png" + Environment.NewLine + Environment.NewLine;
                }

                #region Textures
                if (hasTexture && copyTextures)
                {
                    File.Copy(
                        texturePath,
                        path + textureName + ".png",
                        true);
                }
                #endregion
            }
        }

        File.WriteAllText(path + name + ".mtl", data);
        
        #endregion




        #region .obj
        data = "# Material" + Environment.NewLine;

        if (fileUsesMaterials)
        {
            data += "mtllib " + name + ".mtl" + Environment.NewLine;
        }

        data += "s " + "off" + Environment.NewLine;

        data += "# Object" + Environment.NewLine;
        data += "o " + name + Environment.NewLine;
        MeshFilter mf = solid.gameObject.GetComponent<MeshFilter>();
        Mesh m = mf.sharedMesh;

        string f6 = "f6";
        string f4 = "f4";
        CultureInfo c = CultureInfo.InvariantCulture;


        // Construct hash table for vertices

        int[] vertMap;
        List<int> fewestVertices;

        vertMap = new int[m.vertices.Length];
        fewestVertices = new List<int>();
        
        bool[] mappedVerts = new bool[solid.verts.Count];
        int vertIndex = 0;
        for (int i = 0; i < solid.connectedVerts.Count; i++)
        {
            if (!mappedVerts[i])
            {
                List<int> added = new List<int>();
                // "Added" is just for redundancy, in case a connected vert is counted twice by mistake, or in case the format changes.
                added.Add(i);
                
                // Get the full list of connected verts and the average normal of one of the main vertices
                for (int j = 0; j < solid.connectedVerts[i].Count; j++)
                {
                    if (!added.Contains(solid.connectedVerts[i].list[j]))
                    {
                        added.Add(solid.connectedVerts[i].list[j]);
                    }
                }

                bool isValid = true;
                for (int j = 0; j < added.Count; j++)
                {
                    if (mappedVerts[added[j]])
                    {
                        isValid = false;

                        // Fill in all spots that have connected vertices
                        for (int jj = 0; jj < added.Count; jj++)
                        {
                            mappedVerts[added[jj]] = true;
                            vertMap[added[jj]] = vertIndex;
                        }

                        // Add this to the list in the order it was found
                        vertIndex++;

                        break;
                    }
                }

                if (isValid)
                {
                    fewestVertices.Add(i);

                    for (int j = 0; j < added.Count; j++)
                    {
                        mappedVerts[added[j]] = true;
                        vertMap[added[j]] = vertIndex;
                    }

                    vertIndex++;
                }
            }
        }

        #region Create map of normals
        List<Vector3> fewestNormals = new List<Vector3>();
        int[] normMap = new int[m.normals.Length];

        for (int i = 0; i < m.normals.Length; i++)
        {
            normMap[i] = -1;
        }

        Vector3 n = Vector3.zero;
        for (int i = 0; i < m.normals.Length; i++)
        {

            n = m.normals[i];
            int indexOfCopy = -1;

            for (int j = 0; j < fewestNormals.Count; j++)
            {
                if ((fewestNormals[j] - n).sqrMagnitude < 0.000001f)
                {
                    indexOfCopy = j;
                    break;
                }
            }

            if (indexOfCopy >= 0)
            {
                normMap[i] = indexOfCopy;
            }
            else
            {
                normMap[i] = fewestNormals.Count;
                fewestNormals.Add(n);
            }
        }
        #endregion
        #region Create map of UVs
        List<Vector2> fewestUVs = new List<Vector2>();
        int[] uvMap = new int[m.normals.Length];

        for (int i = 0; i < m.uv.Length; i++)
        {
            uvMap[i] = -1;
        }
        Vector2 uv = Vector3.zero;
        for (int i = 0; i < m.uv.Length; i++)
        {

            uv = m.uv[i];
            int indexOfCopy = -1;

            for (int j = 0; j < fewestUVs.Count; j++)
            {
                if ((fewestUVs[j] - uv).sqrMagnitude < 0.000001f)
                {
                    indexOfCopy = j;
                    break;
                }
            }

            if (indexOfCopy >= 0)
            {
                uvMap[i] = indexOfCopy;
            }
            else
            {
                uvMap[i] = fewestUVs.Count;
                fewestUVs.Add(uv);
            }
        }
        #endregion

        data += "# Vertices" + Environment.NewLine;
        for (int i = 0; i < fewestVertices.Count; i++)
        {
            data += "v ";
            data += m.vertices[fewestVertices[i]].x.ToString(f6, c) + " ";
            data += m.vertices[fewestVertices[i]].y.ToString(f6, c) + " ";
            data += m.vertices[fewestVertices[i]].z.ToString(f6, c);
            data += Environment.NewLine;
        }

        data += "# UV Coordinates" + Environment.NewLine;

        for (int i = 0; i < fewestUVs.Count; i++)
        {
            data += "vt ";
            data += fewestUVs[i].x.ToString(f6, c) + " ";
            data += fewestUVs[i].y.ToString(f6, c);
            data += Environment.NewLine;
        }

        data += "# Vertex Normals" + Environment.NewLine;

        for (int i = 0; i < fewestNormals.Count; i++)
        {
            Vector3 normal = fewestNormals[i];

            data += "vn ";
            data += normal.x.ToString(f4, c) + " ";
            data += normal.y.ToString(f4, c) + " ";
            data += normal.z.ToString(f4, c);
            data += Environment.NewLine;
        }

        data += "# Faces" + Environment.NewLine;
        int[] compoundedVertIndexes = new int[solid.connectedVerts.Count];

        for (int i = 0; i < solid.connectedVerts.Count; i++)
        {
            List<int> choices = new List<int>();
            choices.Add(i);
            for (int j = 0; j < solid.connectedVerts[i].Count; j++)
            {
                choices.Add(solid.connectedVerts[i].list[j]);
            }

            bool doesVertExist = false;

            for (int j = 0; j < i; j++)
            {
                if (choices.Contains(compoundedVertIndexes[j]))
                {
                    doesVertExist = true;
                    compoundedVertIndexes[i] = compoundedVertIndexes[j];
                    break;
                }
            }

            if (!doesVertExist)
            {
                compoundedVertIndexes[i] = choices[0];
            }
        }

        for (int ii = 0; ii < matNames.Count; ii++)
        {
            int f = 0;
            if (fileUsesMaterials)
            {
                data += "usemtl " + matNames[ii] + Environment.NewLine;
            }
            
            for (int i = 0; i < solid.quads.Count; i += 4)
            {
                if (!fileUsesMaterials || solid.quadMaterial[f] == matIndexes[ii])
                {
                    Vector3 faceNormal =
                        (fewestNormals[normMap[solid.quads[i + 0]]] +
                        fewestNormals[normMap[solid.quads[i + 1]]] +
                        fewestNormals[normMap[solid.quads[i + 2]]] +
                        fewestNormals[normMap[solid.quads[i + 3]]]) / 4.0f;

                    Vector3 ta = m.vertices[fewestVertices[vertMap[solid.quads[i + 1]]]];
                    Vector3 tb = m.vertices[fewestVertices[vertMap[solid.quads[i + 0]]]];
                    Vector3 tc = m.vertices[fewestVertices[vertMap[solid.quads[i + 2]]]];

                    Vector3 ab = tb - ta;
                    Vector3 ac = tc - ta;
                    Vector3 proofNormal = Vector3.Cross(ab, ac);

                    data += "f ";

                    // Obj file format gets the face direction based on the standard cross product of it's triangles.
                    // Because of this it expects the face to list the points in a clockwise/counterclockwise fashion.
                    if (Vector3.Dot(faceNormal, proofNormal) > 0)
                    {
                        data += (vertMap[solid.quads[i + 1]] + 1) + "/" + (uvMap[solid.quads[i + 1]] + 1) + "/" + (normMap[solid.quads[i + 1]] + 1) + " ";
                        data += (vertMap[solid.quads[i + 0]] + 1) + "/" + (uvMap[solid.quads[i + 0]] + 1) + "/" + (normMap[solid.quads[i + 0]] + 1) + " ";
                        data += (vertMap[solid.quads[i + 2]] + 1) + "/" + (uvMap[solid.quads[i + 2]] + 1) + "/" + (normMap[solid.quads[i + 2]] + 1) + " ";
                        data += (vertMap[solid.quads[i + 3]] + 1) + "/" + (uvMap[solid.quads[i + 3]] + 1) + "/" + (normMap[solid.quads[i + 3]] + 1);
                    }
                    else
                    {
                        // Flipped face
                        data += (vertMap[solid.quads[i + 2]] + 1) + "/" + (uvMap[solid.quads[i + 2]] + 1) + "/" + (normMap[solid.quads[i + 2]] + 1) + " ";
                        data += (vertMap[solid.quads[i + 0]] + 1) + "/" + (uvMap[solid.quads[i + 0]] + 1) + "/" + (normMap[solid.quads[i + 0]] + 1) + " ";
                        data += (vertMap[solid.quads[i + 1]] + 1) + "/" + (uvMap[solid.quads[i + 1]] + 1) + "/" + (normMap[solid.quads[i + 1]] + 1) + " ";
                        data += (vertMap[solid.quads[i + 3]] + 1) + "/" + (uvMap[solid.quads[i + 3]] + 1) + "/" + (normMap[solid.quads[i + 3]] + 1);
                    }

                    data += Environment.NewLine;
                }
                f++;
            }
        }

        File.WriteAllText(path + name + ".obj", data);
        #endregion
    }
    // Stub class, used to block the other references when MeshEdit is exported
    private class MeshMod : MonoBehaviour
    {
        public void applyAllMods()
        {

        }
    }
    private void exportPrefab(MeshEdit solid)
    {
        // TODO: Allow the user toggle applying modifiers to exported objects
        string fullPath = EditorUtility.SaveFilePanel(
            "Save mesh as a prefab",
            Application.dataPath + solid.lastPrefabPath,
            solid.gameObject.name,
            "prefab");


        if (fullPath.Length > 0)
        {

            string localPath = "Assets" + fullPath.Replace(Application.dataPath, "");

            if (localPath.EndsWith(".prefab"))
            {
                localPath = localPath.Replace(".prefab", "");
            }

            string path = fullPath;
            int slashIndex = path.LastIndexOf('/') + 1;
            string name = path.Remove(0, slashIndex);
            name = name.Remove(name.Length - 4);

            path = path.Remove(slashIndex);

            string assetName = fullPath.Remove(fullPath.LastIndexOf('.')).Remove(0, fullPath.LastIndexOf('/') + 1);

            // Create necessary assets
            /*
            string objPath = fullPath.Remove(fullPath.LastIndexOf('.')) + ".obj";
            string objPathLocal = "Assets" + objPath.Replace(Application.dataPath, "");
            createObjFile(solid, objPath, false, false);
            
            string matPath = localPath.Remove(localPath.LastIndexOf('.')) + ".mat";
            Material prefabMaterial = new Material(solid.GetComponent<Renderer>().sharedMaterial);
            AssetDatabase.CreateAsset(prefabMaterial, matPath);
            */

            string prefabPath = localPath + ".prefab";
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                
                prefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
            }

            GameObject prefabObject = new GameObject(name);
            prefabObject.transform.position = solid.transform.position;
            prefabObject.transform.rotation = solid.transform.rotation;
            prefabObject.transform.localScale = solid.transform.localScale;

            MeshFilter mf = prefabObject.AddComponent<MeshFilter>();
            Renderer r = prefabObject.AddComponent<MeshRenderer>();

            Mesh meshCopy = Mesh.Instantiate(solid.GetComponent<MeshFilter>().sharedMesh) as Mesh;
            AssetDatabase.CreateAsset(meshCopy, localPath + "-Mesh");

            Renderer solidMeshRenderer = solid.GetComponent<Renderer>();



            Material[] materials = null;

            if (solidMeshRenderer != null && solidMeshRenderer.sharedMaterials != null)
            {
                materials = new Material[solidMeshRenderer.sharedMaterials.Length];

                
                for (int i = 0; i < solidMeshRenderer.sharedMaterials.Length; i++)
                {
                    Material mat = Instantiate(solidMeshRenderer.sharedMaterials[i]);
                    AssetDatabase.CreateAsset(mat, localPath + "-Material" + i + ".mat");

                    materials[i] = mat;
                }
                
                    /*
                //if (duplicateAssetsForPrefab)
                else  TURNS OUT THIS DOESN'T WORK!!
                {
                    for (int i = 0; i < solidMeshRenderer.sharedMaterials.Length; i++)
                    {
                        materials[i] = Instantiate(solidMeshRenderer.sharedMaterials[i]);
                    }
                }*/

                r.sharedMaterials = materials;

            }


            mf.mesh = meshCopy;
            r = solid.GetComponent<Renderer>();

            MonoBehaviour[] scripts = solid.GetComponents<MonoBehaviour>();
            if (scripts != null)
            {
                for (int i = 0; i < scripts.Length; i++)
                {
                    if (copyScriptsToPrefab && (!(scripts[i] is MeshMod) && !(scripts[i] is MeshEdit)) ||
                        copyMeshEditToPrefab && ((scripts[i] is MeshMod) || (scripts[i] is MeshEdit)))
                    {
                        // Get the new materials for the prefab
                        MeshEdit meshEdit = scripts[i] as MeshEdit;
                        if (meshEdit != null)
                        {
                            MeshEdit newMeshEditObject = prefabObject.AddComponent<MeshEdit>();
                            meshEdit.deepCopyMeshEditScript(newMeshEditObject);

                            newMeshEditObject.setToEditorPosition();

                            newMeshEditObject.materials = materials.ToList<Material>();
                            
                        }
                        else
                        {
                            Type t = scripts[i].GetType();
                            var script = prefabObject.AddComponent(t);
                            script = Instantiate(scripts[i]);
                        }

                    }
                }
            }

            // Replace the prefab to update existing prefabs without break instancing
            PrefabUtility.ReplacePrefab(prefabObject, prefab, ReplacePrefabOptions.ReplaceNameBased);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DestroyImmediate(prefabObject);

            // If no errors have been hit, save the new local path
            solid.lastPrefabPath = localPath;
        }


    }

    public static void importObj()
    {
        string path = EditorUtility.OpenFilePanel(
            "Export model as a .obj file",
            Application.dataPath + "/Resources",
            "obj");

        if (path != null && path.Length > 0)
        {
            Debug.Log("Importing " + path);

            string data;

            using (StreamReader reader = new StreamReader(path))
            {
                data = reader.ReadToEnd();
            }

            // Repeat for each "o" object in the file that corresponds to a mesh and create a separate mesh edit object for each.
            // TODO: Set up texturing tab to show the loaded texture, or a warning before initialising a new tiled texture.

            string[] lines = data.Split('\r', '\n');

            string materialPath = "";
            string materialName = "";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> vertNormals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            for (int o = 0; o < lines.Length; o++)
            {
                if (lines[o].Trim(' ').StartsWith("mtllib"))
                {
                    materialPath = lines[o].Trim(' ').Remove(0, 6).Trim(' ');
                }
                if (lines[o].Trim(' ').StartsWith("usemtl"))
                {
                    materialName = lines[o].Trim(' ').Remove(0, 6).Trim(' ');
                }

                if (lines[o].Trim(' ').StartsWith("o"))
                {
                    #region Load Mesh
                    List<List<int>> faceVertices = new List<List<int>>();
                    List<List<int>> faceUvs = new List<List<int>>();
                    List<List<int>> faceNormals = new List<List<int>>();
                    string texturePath = "";
                    string name = "";

                    name = lines[o].Trim(' ').Remove(0, 1).Trim(' ');

                    Debug.Log("Loading new mesh: " + name);

                    for (int i = o; i < lines.Length; i++)
                    {
                        // Empty
                        if (lines[i].Length == 0)
                        {
                            continue;
                        }

                        // Comment
                        if (lines[i].StartsWith("#"))
                        {
                            continue;
                        }

                        // New object, break from current object.
                        if (lines[i].Trim(' ').StartsWith("o") && i != o)
                        {
                            o = i - 1;
                            break;
                        }

                        if (lines[i].Trim(' ').StartsWith("mtllib"))
                        {
                            materialPath = lines[i].Trim(' ').Remove(0, 6).Trim(' ');
                        }
                        if (lines[i].Trim(' ').StartsWith("usemtl"))
                        {
                            materialName = lines[i].Trim(' ').Remove(0, 6).Trim(' ');
                        }

                        // Vert
                        Vector3 vert;
                        Vector2 tex;
                        if (parseVector3(lines[i], out vert, "v# # #"))
                        {
                            vertices.Add(vert);
                        }
                        // Normal
                        else if (parseVector3(lines[i], out vert, "vn# # #"))
                        {
                            vertNormals.Add(vert);
                        }
                        // Uv
                        else if (parseVector2(lines[i], out tex, "vt# #"))
                        {
                            uvs.Add(tex);
                        }
                        // Face
                        else if (lines[i].TrimStart(' ').StartsWith("f"))
                        {
                            string face = lines[i].TrimStart(' ').Remove(0, 1);

                            string[] elements = face.Split(' ');

                            List<int> newFaceVertices = new List<int>();
                            List<int> newFaceUvs = new List<int>();
                            List<int> newFaceNormals = new List<int>();

                            int vertsAdded = 0;
                            for (int j = 0; j < elements.Length; j++)
                            {
                                string faceElement = elements[j].Trim(' ');
                                if (faceElement.Length > 0)
                                {
                                    string[] arguments = faceElement.Split('/');
                                    if (arguments.Length == 1)
                                    {
                                        int vertex;
                                        if (int.TryParse(arguments[0], out vertex))
                                        {
                                            vertsAdded++;
                                            newFaceVertices.Add(vertex - 1);
                                        }
                                    }
                                    else if (arguments.Length == 2)
                                    {
                                        int vertex;
                                        if (int.TryParse(arguments[0], out vertex))
                                        {
                                            vertsAdded++;
                                            newFaceVertices.Add(vertex - 1);
                                        }
                                        int uv;
                                        if (int.TryParse(arguments[1], out uv))
                                        {
                                            newFaceUvs.Add(uv - 1);
                                        }
                                    }
                                    else if (arguments.Length == 3)
                                    {
                                        int vertex;
                                        if (int.TryParse(arguments[0], out vertex))
                                        {
                                            vertsAdded++;
                                            newFaceVertices.Add(vertex - 1);
                                        }
                                        int uv;
                                        if (int.TryParse(arguments[1], out uv))
                                        {
                                            newFaceUvs.Add(uv - 1);
                                        }
                                        int normal;
                                        if (int.TryParse(arguments[2], out normal))
                                        {
                                            newFaceNormals.Add(normal - 1);
                                        }
                                    }
                                }

                            }
                            // Only add if there are exactly four elements in the face. 
                            // Quads only topology in the current Mesh Edit system
                            if (Mathf.Max(newFaceVertices.Count, newFaceUvs.Count, newFaceNormals.Count) == 4)
                            {
                                faceVertices.Add(newFaceVertices);
                                faceUvs.Add(newFaceUvs);
                                faceNormals.Add(newFaceNormals);
                            }

                        }
                    }


                    int fV = faceVertices.Count;
                    int fU = faceUvs.Count;
                    int fN = faceNormals.Count;

                    if (fU < fV)
                    {
                        fU = 0;
                    }
                    if (fN < fV)
                    {
                        fN = 0;
                    }

                    Mesh mesh = new Mesh();
                    List<int> quads = new List<int>();
                    List<int> tris = new List<int>();

                    List<Vector3> meshVertices = new List<Vector3>();
                    List<Vector3> meshVertNormals = new List<Vector3>();
                    List<Vector3> meshMeshNormals = new List<Vector3>();


                    MeshEdit.ListWrapper[] connectedVerts = new MeshEdit.ListWrapper[faceVertices.Count * 4];

                    for (int i = 0; i < connectedVerts.Length; i++)
                    {
                        connectedVerts[i] = new MeshEdit.ListWrapper();
                    }

                    // The remap is intented to counteract the way the verts are shuffled around in the mesh-building section
                    int[] remap = { 1, 3, 2, 0 };
                    for (int i = 0; i < faceVertices.Count; i++)
                    {

                        for (int ii = 0; ii < faceVertices[i].Count; ii++)
                        {

                            for (int j = 0; j < faceVertices.Count; j++)
                            {
                                // As long as they're not the same face
                                if (i != j)
                                {

                                    for (int jj = 0; jj < faceVertices[j].Count; jj++)
                                    {
                                        if (faceVertices[j][jj] == faceVertices[i][ii])
                                        {
                                            connectedVerts[j * 4 + remap[jj]].Add(i * 4 + remap[ii]);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Mesh building
                    List<Vector2> meshUvs = new List<Vector2>();

                    for (int i = 0; i < faceVertices.Count; i++)
                    {
                        meshVertices.Add(vertices[faceVertices[i][3]]);
                        meshVertices.Add(vertices[faceVertices[i][0]]);
                        meshVertices.Add(vertices[faceVertices[i][2]]);
                        meshVertices.Add(vertices[faceVertices[i][1]]);

                        Vector3 ab = vertices[faceVertices[i][1]] - vertices[faceVertices[i][0]];
                        Vector3 ac = vertices[faceVertices[i][2]] - vertices[faceVertices[i][0]];
                        Vector3 defaultNormal = Vector3.Cross(ab, ac);

                        if (fN > 0)
                        {
                            if (faceNormals != null && faceNormals.Count > i && faceNormals[i].Count == 4)
                            {
                                meshVertNormals.Add(vertNormals[faceNormals[i][3]]);
                                meshVertNormals.Add(vertNormals[faceNormals[i][0]]);
                                meshVertNormals.Add(vertNormals[faceNormals[i][2]]);
                                meshVertNormals.Add(vertNormals[faceNormals[i][1]]);

                                meshMeshNormals.Add(vertNormals[faceNormals[i][3]]);
                                meshMeshNormals.Add(vertNormals[faceNormals[i][0]]);
                                meshMeshNormals.Add(vertNormals[faceNormals[i][2]]);
                                meshMeshNormals.Add(vertNormals[faceNormals[i][1]]);
                            }
                            else
                            {
                                meshVertNormals.Add(defaultNormal);
                                meshVertNormals.Add(defaultNormal);
                                meshVertNormals.Add(defaultNormal);
                                meshVertNormals.Add(defaultNormal);

                                meshMeshNormals.Add(defaultNormal);
                                meshMeshNormals.Add(defaultNormal);
                                meshMeshNormals.Add(defaultNormal);
                                meshMeshNormals.Add(defaultNormal);
                            }
                        }
                        else
                        {
                            meshVertNormals.Add(defaultNormal);
                            meshVertNormals.Add(defaultNormal);
                            meshVertNormals.Add(defaultNormal);
                            meshVertNormals.Add(defaultNormal);

                            meshMeshNormals.Add(defaultNormal);
                            meshMeshNormals.Add(defaultNormal);
                            meshMeshNormals.Add(defaultNormal);
                            meshMeshNormals.Add(defaultNormal);
                        }
                        if (fU > 0)
                        {
                            if (faceUvs != null && faceUvs.Count > i && faceUvs[i].Count == 4)
                            {
                                meshUvs.Add(uvs[faceUvs[i][3]]);
                                meshUvs.Add(uvs[faceUvs[i][0]]);
                                meshUvs.Add(uvs[faceUvs[i][2]]);
                                meshUvs.Add(uvs[faceUvs[i][1]]);
                            }
                            else
                            {
                                meshUvs.Add(Vector2.zero);
                                meshUvs.Add(Vector2.zero);
                                meshUvs.Add(Vector2.zero);
                                meshUvs.Add(Vector2.zero);
                            }
                        }
                        else
                        {
                            meshUvs.Add(Vector2.zero);
                            meshUvs.Add(Vector2.zero);
                            meshUvs.Add(Vector2.zero);
                            meshUvs.Add(Vector2.zero);
                        }

                        tris.Add(i * 4 + 0);
                        tris.Add(i * 4 + 1);
                        tris.Add(i * 4 + 2);

                        tris.Add(i * 4 + 3);
                        tris.Add(i * 4 + 2);
                        tris.Add(i * 4 + 1);

                        quads.Add(i * 4 + 0);
                        quads.Add(i * 4 + 1);
                        quads.Add(i * 4 + 2);
                        quads.Add(i * 4 + 3);
                    }

                    GameObject go = new GameObject(name);
                    go.transform.position = SceneView.lastActiveSceneView.pivot;

                    Color[] colours = new Color[meshVertices.Count];
                    for (int i = 0; i < colours.Length; i++)
                    {
                        colours[i] = Color.white;
                    }

                    mesh.vertices = meshVertices.ToArray();
                    mesh.triangles = tris.ToArray();
                    mesh.colors = colours;
                    if (fU > 0)
                    {
                        mesh.uv = meshUvs.ToArray();
                    }
                    else
                    {
                        mesh.uv = new Vector2[meshVertices.Count];
                    }

                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();

                    // Add MeshFilter
                    MeshFilter mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = mesh;

                    // Add MeshRenderer
                    MeshRenderer mr = go.AddComponent<MeshRenderer>();
                    mr.material = new Material(Shader.Find("MeshEdit/ModellingShader"));

                    // Add MeshEdit script
                    MeshEdit me = go.AddComponent<MeshEdit>();

                    // Full MeshEdit initialisation
                    me.deepCopyMesh(ref mesh, ref me._mesh);

                    me.verts = meshVertices;
                    me.vertNormals = meshVertNormals;
                    me.meshNormals = meshMeshNormals;
                    me.colours = colours.ToList();
                    me.quads = quads;
                    me.tris = tris;

                    me.connectedVerts = connectedVerts.ToList();
                    me.rebuildAdjacentFaces();

                    //
                    me.getVerts(false);

                    me.tris = new List<int>();
                    for (int i = 0; i < me.mesh.triangles.Length; i++)
                    {
                        me.tris.Add(me.mesh.triangles[i]);
                    }

                    me.getQuads(false);

                    me.getConnectedVertsBasedOnPosition();

                    me.rebuildAdjacentFaces();

                    me.checkMeshValidity();
                    //

                    me.recalculateNormals(me._mesh);
                    for (int i = 0; i < meshVertices.Count; i++)
                    {
                        meshVertices[i] += go.transform.position;
                    }

                    #endregion

                    #region Load textures and materials
                    string matPath = path.Remove(path.LastIndexOf('/')) + "/" + materialPath;

                    using (StreamReader stream = new StreamReader(matPath))
                    {
                        data = stream.ReadToEnd();
                    }

                    string[] mtlLines = data.Split('\r', '\n');

                    bool isReadingCorrectMaterial = false;

                    for (int i = 0; i < mtlLines.Length; i++)
                    {
                        if (mtlLines[i].Trim(' ').StartsWith("newmtl"))
                        {
                            string currentName = mtlLines[i].Trim(' ').Remove(0, 6).Trim(' ');

                            isReadingCorrectMaterial = (currentName == materialName);

                        }

                        if (isReadingCorrectMaterial &&
                            mtlLines[i].Trim(' ').ToLower().StartsWith("map_kd"))
                        {
                            string textureName = mtlLines[i].Trim(' ').Remove(0, 6).Trim(' ');
                            texturePath = path.Remove(path.LastIndexOf('/')) + "/" + textureName;


                            // Import the texture and apply it to the mesh
                            string importPath = Application.dataPath + "/Resources/" + textureName;

                            File.Copy(
                                texturePath,
                                importPath,
                                true);

                            AssetDatabase.ImportAsset("Assets/Resources/" + textureName);
                            AssetDatabase.Refresh();

                            string loadName = textureName.Remove(textureName.LastIndexOf('.'));
                            Texture tex = Resources.Load<Texture>(loadName);
                            Renderer r = go.GetComponent<Renderer>();
                            r.sharedMaterial.SetTexture("_MainTex", tex);

                            me.customTextureUVMap = meshUvs;
                            me.hasDefaultUVs = true;
                            me.defaultUVs = meshUvs.ToArray();
                            me.uvMaps = new List<MeshEdit.UVData>();
                            //me.uvMaps.Add(new MeshEdit.UVBasic(uvs, tex));


                        }
                    }

                    #endregion

                    GameObject[] objects = new GameObject[1];
                    objects[0] = me.gameObject;
                    Selection.objects = objects;
                    Selection.activeGameObject = me.gameObject;

                    SceneView sceneView = (SceneView)SceneView.sceneViews[0];
                    sceneView.Focus();

                }
            }
        }
    }

    public void readUVsOfSelectedQuads(MeshEdit solid, int selectedQuad)
    { 
        if (interfaceTileUVs != null)
        {
            interfaceTileUVs.readUVsOfSelectedQuads(solid, selectedQuad, snapUVsToPixel);
        }
    }

    public static void saveSettings(MeshEdit meshEdit, MeshEditWindow window)
    {
        MeshEditWindow.checkEditorFoldersExist();

        string settings = "";
        settings += "CSTP=" + window.copyScriptsToPrefab.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "CMTP=" + window.copyMeshEditToPrefab.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "CATP=" + window.duplicateAssetsForPrefab.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "CTOE=" + window.copyTexturesOnExport.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "Help=" + window.showShortCuts.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "RNOT=" + window.recalculateNormalsOnTransform.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "SnapUV=" + window.snapUVsToPixel.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "SOSV=" + window.showOnlySelectedVerts.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "UseUVs=" + window.useCustomUVCoords.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "VMode=" + meshEdit.vertMode.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "PMode=" + meshEdit.paintMode.ToString(CultureInfo.InvariantCulture) + '\n';
        settings += "PCol=" +
            meshEdit.editorInfo.paintColour.r.ToString(CultureInfo.InvariantCulture) + "," +
            meshEdit.editorInfo.paintColour.g.ToString(CultureInfo.InvariantCulture) + "," +
            meshEdit.editorInfo.paintColour.b.ToString(CultureInfo.InvariantCulture) + "," +
            meshEdit.editorInfo.paintColour.a.ToString(CultureInfo.InvariantCulture) + '\n';
        if (meshEdit.editorInfo.colourHistory != null)
        {
            for (int i = 0; i < meshEdit.editorInfo.colourHistory.Count; i++)
            {
                settings += "HCol" + i + "=" +
                    meshEdit.editorInfo.colourHistory[i].r.ToString(CultureInfo.InvariantCulture) + "," +
                    meshEdit.editorInfo.colourHistory[i].g.ToString(CultureInfo.InvariantCulture) + "," +
                    meshEdit.editorInfo.colourHistory[i].b.ToString(CultureInfo.InvariantCulture) + "," +
                    meshEdit.editorInfo.colourHistory[i].a.ToString(CultureInfo.InvariantCulture) + '\n';
            }
        }
        
        File.WriteAllText(Application.dataPath + MeshEditWindow.settingsPath, settings);
    }

    public static void loadSettings(MeshEdit meshEdit, MeshEditWindow window)
    {
        MeshEditWindow.checkEditorFoldersExist();

        meshEdit.editorInfo.colourHistory = new List<Color>();
        
        if (File.Exists(Application.dataPath + MeshEditWindow.settingsPath))
        {
            string settings = File.ReadAllText(Application.dataPath + MeshEditWindow.settingsPath);

            string[] args = settings.Split('\n');
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("CTOE"))
                {
                    bool.TryParse(args[i].Remove(0, 5), out window.copyTexturesOnExport);
                }
                else if (args[i].StartsWith("CMTP"))
                {
                    bool.TryParse(args[i].Remove(0, 5), out window.copyMeshEditToPrefab);
                }
                else if (args[i].StartsWith("CSTP"))
                {
                    bool.TryParse(args[i].Remove(0, 5), out window.copyScriptsToPrefab);
                }
                else if (args[i].StartsWith("CATP"))
                {
                    bool.TryParse(args[i].Remove(0, 5), out window.duplicateAssetsForPrefab);
                }
                else if (args[i].StartsWith("Help"))
                {
                    bool.TryParse(args[i].Remove(0, 5), out window.showShortCuts);
                }
                else if (args[i].StartsWith("SOSV"))
                {
                    bool.TryParse(args[i].Remove(0, 5), out window.showOnlySelectedVerts);
                }
                else if (args[i].StartsWith("RNOT"))
                {
                    bool.TryParse(args[i].Remove(0, 5), out window.recalculateNormalsOnTransform);
                }
                else if (args[i].StartsWith("SnapUV"))
                {
                    bool.TryParse(args[i].Remove(0, 7), out window.snapUVsToPixel);
                }
                else if (args[i].StartsWith("UseUVs"))
                {
                    bool.TryParse(args[i].Remove(0, 7), out window.useCustomUVCoords);
                }
                else if (args[i].StartsWith("VMode"))
                {
                    int.TryParse(args[i].Remove(0, 6), out meshEdit.vertMode);
                }
                else if (args[i].StartsWith("PMode"))
                {
                    int.TryParse(args[i].Remove(0, 6), out meshEdit.paintMode);
                }
                else if (args[i].StartsWith("PCol"))
                {
                    string colourString = args[i].Remove(0, 5);

                    string[] values = colourString.Split(',');
                    float r, g, b, a;
                    float.TryParse(values[0], out r);
                    float.TryParse(values[1], out g);
                    float.TryParse(values[2], out b);
                    float.TryParse(values[3], out a);

                    meshEdit.editorInfo.paintColour = new Color(r, g, b, a);
                }
                else if (args[i].StartsWith("HCol"))
                {
                    string colourString = args[i].Remove(0, args[i].IndexOf('=') + 1);

                    string[] values = colourString.Split(',');
                    float r, g, b, a;
                    float.TryParse(values[0], out r);
                    float.TryParse(values[1], out g);
                    float.TryParse(values[2], out b);
                    float.TryParse(values[3], out a);
                    Color colour = new Color(r, g, b, a);
                    meshEdit.editorInfo.colourHistory.Add(colour);
                }
            }

        }
    }

    public static bool parseVector3(string text, out Vector3 vector, string format = "# # #")
    {
        int formatPosition = 0;

        string[] vectorElements = new string[3];
        int vectorElement = 0;

        for (int i = 0; i < text.Length; i++)
        {

            // Extract all characters until the next character in the format is found
            // # represents a number
            if (format[formatPosition] == '#')
            {
                vectorElements[vectorElement] += text[i];

                if (text.Length > i + 1 &&
                    format.Length > formatPosition + 1 &&
                    text[i + 1] == format[formatPosition + 1])
                {
                    vectorElement++;
                    formatPosition++;
                }
            }
            else if (format[formatPosition] == text[i])
            {
                formatPosition++;
            }
            else
            {
                continue;
            }
        }

        float a, b, c;

        if (float.TryParse(vectorElements[0], NumberStyles.Any, CultureInfo.InvariantCulture, out a) &&
            float.TryParse(vectorElements[1], NumberStyles.Any, CultureInfo.InvariantCulture, out b) &&
            float.TryParse(vectorElements[2], NumberStyles.Any, CultureInfo.InvariantCulture, out c))
        {

            vector = new Vector3(a, b, c);

            return true;
        }

        vector = Vector3.zero;

        return false;
    }

    public static bool parseVector2(string text, out Vector2 vector, string format = "# #")
    {
        int formatPosition = 0;

        string[] vectorElements = new string[2];
        int vectorElement = 0;

        for (int i = 0; i < text.Length; i++)
        {

            // Extract all characters until the next character in the format is found
            // # represents a number
            if (format[formatPosition] == '#')
            {
                vectorElements[vectorElement] += text[i];

                if (text.Length > i + 1 &&
                    format.Length > formatPosition + 1 &&
                    text[i + 1] == format[formatPosition + 1])
                {
                    vectorElement++;
                    formatPosition++;
                }
            }
            else if (format[formatPosition] == text[i])
            {
                formatPosition++;
            }
            else
            {
                continue;
            }
        }

        float a, b;

        if (float.TryParse(vectorElements[0], NumberStyles.Any, CultureInfo.InvariantCulture, out a) &&
            float.TryParse(vectorElements[1], NumberStyles.Any, CultureInfo.InvariantCulture, out b))
        {

            vector = new Vector2(a, b);

            return true;
        }

        vector = Vector2.zero;

        return false;
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


    bool shift = false;
    bool ctrl = false;
    bool alt = false;
    
    private void setLatestColour(MeshEdit meshEdit)
    {
        if (meshEdit.editorInfo.colourHistory == null)
        {
            meshEdit.editorInfo.colourHistory = new List<Color>();
        }

        meshEdit.editorInfo.colourHistory.Insert(0, meshEdit.editorInfo.paintColour);

        for (int i = 1; i < meshEdit.editorInfo.colourHistory.Count; i++)
        {
            if (meshEdit.editorInfo.paintColour == meshEdit.editorInfo.colourHistory[i] ||
                i >= meshEdit.editorInfo.maxColours)
            {
                meshEdit.editorInfo.colourHistory.RemoveAt(i);
                i--;
            }
        }

        saveSettings(meshEdit, this);
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

        Vector2[] newUVs = new Vector2[solid.mesh.uv.Length];

        for (int i = 0; i < solid.quadMaterial.Count; i++)
        {
            if (solid.materialUVMap[solid.quadMaterial[i]] >= 0 &&
                solid.uvMaps[solid.materialUVMap[solid.quadMaterial[i]]].uvs != null &&
                solid.uvMaps[solid.materialUVMap[solid.quadMaterial[i]]].uvs.Length == meshEdit.mesh.uv.Length)
            {
                newUVs[solid.quads[i * 4 + 0]] = solid.uvMaps[solid.materialUVMap[solid.quadMaterial[i]]].uvs[solid.quads[i * 4 + 0]];
                newUVs[solid.quads[i * 4 + 1]] = solid.uvMaps[solid.materialUVMap[solid.quadMaterial[i]]].uvs[solid.quads[i * 4 + 1]];
                newUVs[solid.quads[i * 4 + 2]] = solid.uvMaps[solid.materialUVMap[solid.quadMaterial[i]]].uvs[solid.quads[i * 4 + 2]];
                newUVs[solid.quads[i * 4 + 3]] = solid.uvMaps[solid.materialUVMap[solid.quadMaterial[i]]].uvs[solid.quads[i * 4 + 3]];
            }
            else if (meshEdit.mesh.uv.Length == solid.quads.Count)
            {
                newUVs[solid.quads[i * 4 + 0]] = meshEdit.mesh.uv[solid.quads[i * 4 + 0]];
                newUVs[solid.quads[i * 4 + 1]] = meshEdit.mesh.uv[solid.quads[i * 4 + 1]];
                newUVs[solid.quads[i * 4 + 2]] = meshEdit.mesh.uv[solid.quads[i * 4 + 2]];
                newUVs[solid.quads[i * 4 + 3]] = meshEdit.mesh.uv[solid.quads[i * 4 + 3]];
            }
        }

        meshEdit.mesh.uv = newUVs;

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
    public static bool rayIntersectsTriangle(Vector3 rayOrigin,
                          Vector3 rayVector,
                          MeshEdit.Triangle inTriangle, ref Vector3 collisionPoint)
    {
        const float EPSILON = 0.0000001f;
        Vector3 vertex0 = inTriangle.a;
        Vector3 vertex1 = inTriangle.b;
        Vector3 vertex2 = inTriangle.c;
        Vector3 edge1, edge2, h, s, q;
        float a, f, u, v;
        edge1 = vertex1 - vertex0;
        edge2 = vertex2 - vertex0;
        h = Vector3.Cross(rayVector, edge2);
        a = Vector3.Dot(edge1, h);

        if (a > -EPSILON && a < EPSILON)
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
        v = f * Vector3.Dot(rayVector, q);
        if (v < 0.0 || u + v > 1.0)
        {
            return false;
        }
        // At this stage we can compute t to find out where the intersection point is on the line.
        float t = f * Vector3.Dot(edge2, q);
        if (t > EPSILON) // ray intersection
        {
            collisionPoint = rayOrigin + rayVector * t;
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

    public static void checkEditorFoldersExist()
    {
        /*
        if (!AssetDatabase.IsValidFolder("Assets/Editor/EditorResources"))
        {
            AssetDatabase.CreateFolder("Assets/Editor", "EditorResources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Editor/EditorResources/Tilesets"))
        {
            AssetDatabase.CreateFolder("Assets/Editor/EditorResources", "Tilesets");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Tilesets"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Tilesets");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Tilesets/Constructed"))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Tilesets", "Constructed");
        }
        */
    }

    private void loadTilesFromTileset(MeshEdit solid, int index)
    {
        MeshEditWindow.checkEditorFoldersExist();

        if (tilesetsAvailable != null && tilesetsAvailable.Count > 0)
        {
            tileset = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets" + TilesetManager.locationOfTilesetInfoWindows + "/" + tilesetsAvailable[index].tilesetName + ".png");

            

            solid.editorInfo.tileWidth = tilesetsAvailable[index].tileWidth;
            solid.editorInfo.tileHeight = tilesetsAvailable[index].tileHeight;
            solid.editorInfo.tileOutline = tilesetsAvailable[index].tileOutline;
            
            solid.editorInfo.texturePage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets" + TilesetManager.locationOfTilesetPagesWindows + "/" + tilesetsAvailable[index].tilesetName + ".png");
            
            solid.editorInfo.tilesPerRow = tileset.width / solid.editorInfo.tileWidth;
            solid.editorInfo.tilesPerColumn = tileset.height / solid.editorInfo.tileHeight;


            tiles = new Texture2D[solid.editorInfo.tilesPerRow, solid.editorInfo.tilesPerColumn];
            for (int y = 0; y < (tileset.height / solid.editorInfo.tileHeight) * solid.editorInfo.tileHeight; y += solid.editorInfo.tileHeight)
            {
                for (int x = 0; x < (tileset.width / solid.editorInfo.tileWidth) * solid.editorInfo.tileWidth; x += solid.editorInfo.tileWidth)
                {
                    Texture2D newTile = new Texture2D(solid.editorInfo.tileWidth, solid.editorInfo.tileHeight, TextureFormat.RGB24, false);

                    for (int cy = 0; cy < solid.editorInfo.tileHeight; cy++)
                    {
                        for (int cx = 0; cx < solid.editorInfo.tileWidth; cx++)
                        {

                            Color pixel = tileset.GetPixel(cx + x, cy + y);
                            if (pixel.a < 0.1f)
                            {
                                pixel = Color.black;
                            }
                            newTile.SetPixel(cx, cy, pixel);
                        }
                    }

                    newTile.filterMode = FilterMode.Point;
                    newTile.Apply(false);
                    tiles[x / solid.editorInfo.tileWidth, (solid.editorInfo.tilesPerColumn - 1) - y / solid.editorInfo.tileHeight] = newTile;
                }
            }

            solid.editorInfo.tileCount = tiles.Length;
        }
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

    public static void loadTilesets()
    {
        tilesetsAvailable = new List<Tileset>();
        tilesetTexturesAvailable = new List<string>();

        MeshEditWindow.checkEditorFoldersExist();

        DirectoryInfo d = new DirectoryInfo(Application.dataPath + TilesetManager.locationOfTilesetInfoWindows);
        FileInfo[] files = d.GetFiles();

        foreach (FileInfo xmlFile in files)
        {
            if (xmlFile.Extension.EndsWith("xml"))
            {
                Tileset tileset = TilesetManager.DeSerializeObject<Tileset>(xmlFile.FullName);
                tileset.loadTexturesFromAssets();
                //tileset.packTextures();

                // If either file has been manually renamed then make sure no asset is loaded, since it would cause a FileNotFound error when loading the texture page.

                tilesetsAvailable.Add(tileset);
                tilesetTexturesAvailable.Add(tileset.tilesetName);

            }
        }
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


    public Material lineMat;
    public void createLineMaterial()
    {
        if (lineMat == null)
        {
            Shader shader = Shader.Find("MeshEdit/GUI2");
            lineMat = new Material(shader);
            lineMat.hideFlags = HideFlags.HideAndDontSave;
        }
    }



    protected Texture backgroundTexture;

    protected Vector2 uvGUIPosition;

    protected Rect viewportRectangle;
    protected Rect oldViewportRectangle;

    protected Rect textureRectangle;

    protected Matrix4x4 viewMatrix;

    protected Vector2 uvViewPos;
    protected float zoom = 1;

    protected void glDrawGrid(float w, float h, float zoom, float offsetX, float offsetY)
    {
        float left = 0 - offsetX;
        float right = left + w ;
        float bottom = 0 - offsetY;
        float top = bottom + h;



        lineMat.SetPass(0);

        GL.Begin(GL.QUADS);
        float g = 80.0f / 256.0f;
        float lineLight = 88.0f / 256.0f;
        float lineHeavy = 108.0f / 256.0f;
        GL.Color(new Color(g, g, g, 1));
         
        GL.Vertex3(right, bottom, 0);
        GL.Vertex3(left, bottom, 0);
        GL.Vertex3(left, top, 0);
        GL.Vertex3(right, top, 0);
        
        float gridUnitW = 0.1f;
        float gridUnitH = 0.1f;
        

        float startY = Mathf.Floor(bottom / gridUnitH) * gridUnitH;

        float startX = Mathf.Floor(left / gridUnitW) * gridUnitW;

        for (float y = startY; y < top; y += gridUnitW)
        {
            GL.Begin(GL.LINES);
            GL.Color(new Color(lineLight, lineLight, lineLight, 1));

            GL.Vertex3(left, y, 0);
            GL.Vertex3(right, y, 0);
            GL.End();
        }
        for (float x = startX; x < right; x += gridUnitH)
        {
            GL.Begin(GL.LINES);
            GL.Color(new Color(lineLight, lineLight, lineLight, 1));

            GL.Vertex3(x, top, 0);
            GL.Vertex3(x, bottom, 0);
            GL.End();
        }

        gridUnitW = 1f;
        gridUnitH = 1f;


        startY = Mathf.Floor(bottom / gridUnitH) * gridUnitH;

        startX = Mathf.Floor(left / gridUnitW) * gridUnitW;

        for (float y = startY; y < top; y += gridUnitW)
        {
            GL.Begin(GL.LINES);
            GL.Color(new Color(lineHeavy, lineHeavy, lineHeavy, 1));

            GL.Vertex3(left, y, 0);
            GL.Vertex3(right, y, 0);
            GL.End();
        }
        for (float x = startX; x < right; x += gridUnitH)
        {
            GL.Begin(GL.LINES);
            GL.Color(new Color(lineHeavy, lineHeavy, lineHeavy, 1));

            GL.Vertex3(x, top, 0);
            GL.Vertex3(x, bottom, 0);
            GL.End();
        }

    }

    public void refreshMaterials(MeshEdit solid)
    {
        textureMat = null;
        lineMat = null;

        createTextureMat(solid);
        createLineMaterial();
    }
    
    public void setMaterialClipRectangle(int left, int right, int top, int bottom)
    {
        if (lineMat != null)
        {
            lineMat.SetInt("_ClipLeft", left);
            lineMat.SetInt("_ClipTop", top);
            lineMat.SetInt("_ClipRight", right);
            lineMat.SetInt("_ClipBottom", bottom);
        }
        if (textureMat != null)
        {
            textureMat.SetInt("_ClipLeft", left);
            textureMat.SetInt("_ClipTop", top);
            textureMat.SetInt("_ClipRight", right);
            textureMat.SetInt("_ClipBottom", bottom);
        }
    }

    public Material textureMat;

    [SerializeField, HideInInspector]
    private int uvEditorSelectedMaterialTexture = 0;

    public void createTextureMat(MeshEdit solid)
    {
        if (backgroundTexture == null)
        {
            if (uvEditorSelectedMaterialTexture >= 0)
            {
                Renderer r = solid.GetComponent<Renderer>();
                if (r)
                {
                    Material sharedMaterial = r.sharedMaterial;
                    if (sharedMaterial)
                    {
                        backgroundTexture = r.sharedMaterial.GetTexture("_MainTex");
                    }
                }
            }
        }
        if (backgroundTexture != null && textureMat == null)
        {
            Shader shader = Shader.Find("MeshEdit/GUI2");
            textureMat = new Material(shader);

            textureMat.SetTexture("_MainTex", backgroundTexture);
        }
    }

    public void resetView(MeshEdit solid)
    {
        uvViewPos.x = (viewportRectangle.width - textureRectangle.width) / 2;
        uvViewPos.y = (viewportRectangle.height - textureRectangle.height) / 2;
        zoom = 1;


        refreshMaterials(solid);
        setMaterialClipRectangle(0, (int)viewportRectangle.width, 0, (int)viewportRectangle.height);
    }
    protected int transformMode = 0;

    Vector2[] oldUVs;
    Vector2 anchorCenter;
    Vector2 moveAnchor;

    Vector2 transformDimensions;

    bool haveUVsChanged;

    bool canDrag = false;


    public void selectionConvertToVertsUV(MeshEdit meshEdit)
    {
        // Convert faces into verts
        for (int i = 0; i < meshEdit.selectedUVFaces.Length; i++)
        {
            for (int ii = 0; ii < 4; ii++)
            {
            }
        }
    }

    public void selectionConvertToFacesUV(MeshEdit meshEdit)
    {
        // Convert verts into faces
        for (int i = 0; i < meshEdit.selectedUVFaces.Length; i++)
        {
            if (meshEdit.selectedUVs[meshEdit.quads[i * 4 + 0]] &&
                 meshEdit.selectedUVs[meshEdit.quads[i * 4 + 1]] &&
                 meshEdit.selectedUVs[meshEdit.quads[i * 4 + 2]] &&
                 meshEdit.selectedUVs[meshEdit.quads[i * 4 + 3]])
            {
                meshEdit.selectedUVFaces[i] = true;
            }
        }
    }

    private void updateUVEvents(MeshEdit meshEdit)
    {
        wantsMouseMove = true;


        bool shift = Event.current.shift;
        bool ctrl = Event.current.control || Event.current.command;
        bool alt = Event.current.alt;

        float vertClickDistance = 14;

        verifySelectionArray(meshEdit);

        Vector2 mousePos = Event.current.mousePosition;
        Vector2 localMousePosition = mousePos - new Vector2(viewportRectangle.xMin, viewportRectangle.yMin);


        if (Event.current.type == EventType.MouseUp)
        {
            canDrag = false;
        }

        if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout &&
            !viewportRectangle.Contains(mousePos) && !(Event.current.type == EventType.MouseDrag && canDrag))
        {
            canDrag = false;
            //return;
        }

        if (Event.current.type == EventType.MouseDown &&
            Event.current.button == 2 &&
            viewportRectangle.Contains(mousePos))
        {
            canDrag = true;
        }

        if (Event.current.type == EventType.MouseMove)
        {
            if (transformMode == 1)
            {
                Vector2[] newUVs = new Vector2[meshEdit.mesh.uv.Length];
                Vector2 delta = moveAnchor - localMousePosition;

                delta.x = delta.x / (textureRectangle.width * zoom);
                delta.y = -delta.y / (textureRectangle.height * zoom);

                if (transformDimensions.sqrMagnitude > 0)
                {
                    delta.x *= transformDimensions.x;
                    delta.y *= transformDimensions.y;
                }

                if (ctrl)
                {
                    float scale = 10f * zoom;

                    if (shift)
                    {
                        scale = 50 * zoom;
                    }

                    delta.x = Mathf.Round(delta.x * scale) / scale;

                    delta.y = Mathf.Round(delta.y * scale) / scale;
                }

                int f = 0;

                float pixelUnitWidth = 1.0f;
                float pixelUnitHeight = 1.0f;
                if (snapUVsToPixel)
                {
                    pixelUnitWidth = 1.0f / backgroundTexture.width;
                    pixelUnitHeight = 1.0f / backgroundTexture.height;
                }

                for (int i = 0; i < meshEdit.quads.Count; i += 4)
                {
                    for (int ii = 0; ii < 4; ii++)
                    {
                        int uv = meshEdit.quads[i + ii];

                        newUVs[uv] = meshEdit.mesh.uv[uv];

                        if (meshEdit.selectedUVs[uv])
                        {
                            newUVs[uv] = oldUVs[uv] - delta;

                            if (snapUVsToPixel)
                            {
                                newUVs[uv] = new Vector2(
                                    Mathf.Round(newUVs[uv].x / pixelUnitWidth) * pixelUnitWidth,
                                    Mathf.Round(newUVs[uv].y / pixelUnitHeight) * pixelUnitHeight);

                            }

                            // Update the source lists
                            if (meshEdit.materialUVMap[meshEdit.quadMaterial[f]] == -1)
                            {
                                meshEdit.customTextureUVMap[uv] = newUVs[uv];
                            }
                            else
                            {
                                MeshEdit.UVData uvData = meshEdit.uvMaps[meshEdit.materialUVMap[meshEdit.quadMaterial[f]]];
                                uvData._newUvs[uv] = newUVs[uv];
                                uvData._uvs[uv] = newUVs[uv];
                            }
                        }
                    }
                    f++;
                }
                meshEdit.mesh.uv = newUVs;
                meshEdit.pushLocalMeshToGameObject();

            }
            else if (transformMode == 2)
            {
                Vector2[] newUVs = new Vector2[meshEdit.mesh.uv.Length];

                Vector2 anchorCenterClipSpace = viewMatrix.MultiplyPoint3x4(new Vector3(anchorCenter.x, 1.0f - anchorCenter.y, 0));

                float angle = Vector2.SignedAngle(localMousePosition - anchorCenterClipSpace, moveAnchor - anchorCenterClipSpace);

                if (ctrl)
                {
                    float scale = 10.0f / 180.0f;

                    if (shift)
                    {
                        scale *= 2.0f;
                    }

                    angle = Mathf.Round(angle * scale) / scale;
                }

                // Since the UV space can be rectangular, we have to make sure the transformations aren't stuffed up;
                float cosFactor = 1.0f;
                if (backgroundTexture != null)
                {
                    cosFactor = (float)backgroundTexture.height / (float)backgroundTexture.width;
                }

                int f = 0;

                float pixelUnitWidth = 1.0f;
                float pixelUnitHeight = 1.0f;
                if (snapUVsToPixel)
                {
                    pixelUnitWidth = 1.0f / backgroundTexture.width;
                    pixelUnitHeight = 1.0f / backgroundTexture.height;
                }

                for (int i = 0; i < meshEdit.quads.Count; i += 4)
                {
                    for (int ii = 0; ii < 4; ii++)
                    {
                        int uv = meshEdit.quads[i + ii];

                        newUVs[uv] = meshEdit.mesh.uv[uv];

                        if (meshEdit.selectedUVs[uv])
                        {
                            newUVs[uv] = oldUVs[uv];
                            newUVs[uv] -= anchorCenter;
                            newUVs[uv].y *= cosFactor;
                            newUVs[uv] = MeshEditWindow.rotate(newUVs[uv], angle, Vector2.zero);
                            newUVs[uv].y /= cosFactor;
                            newUVs[uv] += anchorCenter;

                            //  newUVs[uv] = nUV;

                            if (snapUVsToPixel)
                            {
                                newUVs[uv] = new Vector2(
                                    Mathf.Round(newUVs[uv].x / pixelUnitWidth) * pixelUnitWidth,
                                    Mathf.Round(newUVs[uv].y / pixelUnitHeight) * pixelUnitHeight);

                            }

                            // Update the source lists
                            if (meshEdit.materialUVMap[meshEdit.quadMaterial[f]] == -1)
                            {
                                meshEdit.customTextureUVMap[uv] = newUVs[uv];
                            }
                            else
                            {
                                MeshEdit.UVData uvData = meshEdit.uvMaps[meshEdit.materialUVMap[meshEdit.quadMaterial[f]]];
                                uvData._newUvs[uv] = newUVs[uv];
                                uvData._uvs[uv] = newUVs[uv];
                            }
                        }
                    }
                    f++;
                }
                meshEdit.mesh.uv = newUVs;
                meshEdit.pushLocalMeshToGameObject();
            }
            else if (transformMode == 3)
            {
                Vector2[] newUVs = new Vector2[meshEdit.mesh.uv.Length];

                Vector2 anchorCenterClipSpace = viewMatrix.MultiplyPoint3x4(new Vector3(anchorCenter.x, 1.0f - anchorCenter.y, 0));
                Vector2 lastMousePos = localMousePosition - Event.current.delta;

                Vector2 p1 = moveAnchor - anchorCenterClipSpace;
                Vector2 p2 = localMousePosition - anchorCenterClipSpace;
                float direction = Mathf.Sign(Vector2.Dot(moveAnchor - anchorCenterClipSpace, p2));
                float d1 = p1.magnitude;
                float d2 = p2.magnitude * direction;

                float scaleX = 1;
                float scaleY = 1;

                if (d1 > 0)
                {
                    scaleX = d2 / d1;
                    scaleY = d2 / d1;
                }

                if (transformDimensions.sqrMagnitude == 1)
                {
                    scaleX = Mathf.Pow(scaleX, transformDimensions.x);
                    scaleY = Mathf.Pow(scaleY, transformDimensions.y);
                }



                if (ctrl)
                {
                    float scale = 5;

                    if (shift)
                    {
                        scale = 10;
                    }

                    scaleX = Mathf.Round(scaleX * scale) / scale;
                    scaleY = Mathf.Round(scaleY * scale) / scale;
                }

                int f = 0;

                float pixelUnitWidth = 1.0f;
                float pixelUnitHeight = 1.0f;
                if (snapUVsToPixel)
                {
                    pixelUnitWidth = 1.0f / backgroundTexture.width;
                    pixelUnitHeight = 1.0f / backgroundTexture.height;
                }

                for (int i = 0; i < meshEdit.quads.Count; i += 4)
                {
                    for (int ii = 0; ii < 4; ii++)
                    {
                        int uv = meshEdit.quads[i + ii];

                        newUVs[uv] = meshEdit.mesh.uv[uv];

                        if (meshEdit.selectedUVs[uv])
                        {
                            newUVs[uv] = oldUVs[uv] - anchorCenter;
                            newUVs[uv].x *= scaleX;
                            newUVs[uv].y *= scaleY;
                            newUVs[uv] += anchorCenter;

                            if (snapUVsToPixel)
                            {
                                newUVs[uv] = new Vector2(
                                    Mathf.Round(newUVs[uv].x / pixelUnitWidth) * pixelUnitWidth,
                                    Mathf.Round(newUVs[uv].y / pixelUnitHeight) * pixelUnitHeight);

                            }

                            // Update the source lists
                            if (meshEdit.materialUVMap[meshEdit.quadMaterial[f]] == -1)
                            {
                                meshEdit.customTextureUVMap[uv] = newUVs[uv];
                            }
                            else
                            {
                                MeshEdit.UVData uvData = meshEdit.uvMaps[meshEdit.materialUVMap[meshEdit.quadMaterial[f]]];
                                uvData._newUvs[uv] = newUVs[uv];
                                uvData._uvs[uv] = newUVs[uv];
                            }
                        }
                    }
                    f++;
                }

                meshEdit.mesh.uv = newUVs;
                meshEdit.pushLocalMeshToGameObject();
            }
        }
        else if (Event.current.type == EventType.ScrollWheel)
        {
            if (viewportRectangle.Contains(mousePos))
            {
                if (Event.current.delta.y > 0)
                {
                    if (zoom > 0.1f)
                    {
                        uvViewPos -= (mousePos - new Vector2(viewportRectangle.xMin, viewportRectangle.yMin));
                        zoom /= 1.25f;
                        uvViewPos /= 1.25f;
                        uvViewPos += (mousePos - new Vector2(viewportRectangle.xMin, viewportRectangle.yMin));
                    }
                    Event.current.Use();
                }
                else if (Event.current.delta.y < 0)
                {
                    if (zoom < 100.0f)
                    {
                        uvViewPos -= (mousePos - new Vector2(viewportRectangle.xMin, viewportRectangle.yMin)); ;
                        zoom *= 1.25f;
                        uvViewPos *= 1.25f;
                        uvViewPos += (mousePos - new Vector2(viewportRectangle.xMin, viewportRectangle.yMin)); ;
                    }
                    Event.current.Use();
                }
            }
        }
        else if (Event.current.isMouse)
        {
            if (Event.current.type == EventType.MouseDrag && canDrag)
            {
                if (Event.current.button == 2)
                {
                    uvViewPos += Event.current.delta;
                }
            }
            else if (Event.current.type == EventType.MouseDown &&
                viewportRectangle.Contains(mousePos))
            {
                if (Event.current.button == 0)
                {
                    if (transformMode != 0)
                    {
                        transformMode = 0;
                    }
                }
                else if (Event.current.button == 1)
                {
                    if (transformMode != 0)
                    {
                        activateTransformMode(meshEdit, 0);
                        transformDimensions.x = 0;
                        transformDimensions.y = 0;
                    }
                    else
                    {
                        float d = vertClickDistance * vertClickDistance;

                        if (meshEdit.vertMode == 0)
                        {
                            if (Event.current.modifiers == (EventModifiers.Alt | EventModifiers.Shift) ||
                                Event.current.modifiers == EventModifiers.Alt)
                            {
                                #region Find the closest vert edge
                                int closestEdgeFace = -1;
                                int closestEdgeSide = -1;

                                for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                                {
                                    if (isUVFaceAvailableForSelection(meshEdit, i))
                                    {
                                        for (int q = 0; q < 4; q++)
                                        {
                                            int v0 = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[q * 2 + 0]];
                                            int v1 = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[q * 2 + 1]];

                                            // This edge is open for business!!
                                            Vector2 uv0 = viewMatrix.MultiplyPoint3x4(new Vector3(meshEdit.mesh.uv[v0].x, 1.0f - meshEdit.mesh.uv[v0].y, 0));
                                            Vector2 uv1 = viewMatrix.MultiplyPoint3x4(new Vector3(meshEdit.mesh.uv[v1].x, 1.0f - meshEdit.mesh.uv[v1].y, 0));
                                            Vector2 point = closestPoint(uv0, uv1, localMousePosition);
                                            float dd = (point - (localMousePosition)).sqrMagnitude;

                                            if (dd < d)
                                            {
                                                d = dd;
                                                closestEdgeFace = i;
                                                closestEdgeSide = q;
                                            }
                                        }
                                    }
                                }
                                //Debug.Log("Closest Edge face: " + closestEdgeFace);
                                // Debug.Log("Closest Edge face side: " + closestEdgeSide);
                                if (closestEdgeFace >= 0)
                                {

                                    // Select all available uvs on the continuous edge that contains the selected face-edge :(
                                    //meshEdit.loop
                                    // 1. Get the opposite face to the selected one
                                    // 2. In both directions, follow the next adjacent faces of both the selected and opposite face
                                    // 3. If there are any breaks in the next faces not being adjacent anymore (in the selected group) then stop adding verts to the selection
                                    // 4. If you come across a face that has already been checked, stop immediately.
                                    int oppositeEdgeFace = meshEdit.adjacentFaces[closestEdgeFace].list[closestEdgeSide];

                                    if (oppositeEdgeFace >= 0 && (!isUVFaceAvailableForSelection(meshEdit, oppositeEdgeFace) || !areFacesTrulyAdjacentByUvs(meshEdit, closestEdgeFace, oppositeEdgeFace)))
                                    {
                                        oppositeEdgeFace = -1;
                                    }
                                    // Debug.Log("Opposite face: " + oppositeEdgeFace);

                                    meshEdit.selectionConvertToFaces();

                                    List<int> vertsToAdd = new List<int>();

                                    List<int> loopA = getFaceLoop(meshEdit, closestEdgeFace, (closestEdgeSide + 1) % 4);

                                    if (loopA != null && loopA.Count > 0)
                                    {
                                        int loopAStartIndex = loopA.IndexOf(closestEdgeFace);

                                        //  Debug.Log("StartIndex = " + loopAStartIndex);

                                        if (oppositeEdgeFace == -1)
                                        {
                                            #region Edge select for lone edges
                                            int nextSide = closestEdgeSide;

                                            int a = meshEdit.quads[closestEdgeFace * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 0]];
                                            int b = meshEdit.quads[closestEdgeFace * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 1]];

                                            vertsToAdd.Add(a);
                                            vertsToAdd.Add(b);

                                            // Start forward from the selected face
                                            for (int i = loopAStartIndex + 1; i < loopA.Count; i++)
                                            {
                                                nextSide = meshEdit.getSideOfFaceBThatContinuesSideFromFaceA(loopA[i - 1], nextSide, loopA[i]);
                                                int adjacentFace = meshEdit.adjacentFaces[loopA[i]].list[nextSide];


                                                bool isAdjacentSideDisconnected = !isUVFaceAvailableForSelection(meshEdit, adjacentFace);

                                                if (!isAdjacentSideDisconnected &&
                                                    !areFacesTrulyAdjacentByUvs(meshEdit, loopA[i], adjacentFace))
                                                {
                                                    isAdjacentSideDisconnected = true;
                                                }


                                                if (nextSide >= 0 && isAdjacentSideDisconnected)
                                                {
                                                    a = meshEdit.quads[loopA[i] * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 0]];
                                                    b = meshEdit.quads[loopA[i] * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 1]];

                                                    vertsToAdd.Add(a);
                                                    vertsToAdd.Add(b);
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }

                                            // Repeat in reverse
                                            nextSide = closestEdgeSide;

                                            for (int i = loopAStartIndex - 1; i >= 0; i--)
                                            {
                                                nextSide = meshEdit.getSideOfFaceBThatContinuesSideFromFaceA(loopA[i + 1], nextSide, loopA[i]);
                                                int adjacentFace = meshEdit.adjacentFaces[loopA[i]].list[nextSide];

                                                bool isAdjacentSideDisconnected = !isUVFaceAvailableForSelection(meshEdit, adjacentFace);

                                                if (!isAdjacentSideDisconnected &&
                                                    !areFacesTrulyAdjacentByUvs(meshEdit, loopA[i], adjacentFace))
                                                {
                                                    isAdjacentSideDisconnected = true;
                                                }


                                                if (nextSide >= 0 && isAdjacentSideDisconnected)
                                                {
                                                    a = meshEdit.quads[loopA[i] * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 0]];
                                                    b = meshEdit.quads[loopA[i] * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 1]];

                                                    vertsToAdd.Add(a);
                                                    vertsToAdd.Add(b);
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Edge select for middle edges
                                            // From the starting points on each list, the faces at that index must be adjacent to one another, if either end early or stop being adjacent, then the edge stops
                                            int opposideEdgeSide = meshEdit.adjacentFaces[oppositeEdgeFace].list.IndexOf(closestEdgeFace);

                                            List<int> loopB = null;

                                            loopB = getFaceLoop(meshEdit, oppositeEdgeFace, (opposideEdgeSide + 1) % 4);

                                            int loopBStartIndex = loopB.IndexOf(oppositeEdgeFace);

                                            int loopBDirection = 1;




                                            int nextSide = closestEdgeSide;
                                            int nextSideOpposite = opposideEdgeSide;

                                            int a = meshEdit.quads[closestEdgeFace * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 0]];
                                            int b = meshEdit.quads[closestEdgeFace * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 1]];
                                            int x = meshEdit.quads[oppositeEdgeFace * 4 + MeshEdit.quadEdgePatternClockwise[nextSideOpposite * 2 + 0]];
                                            int y = meshEdit.quads[oppositeEdgeFace * 4 + MeshEdit.quadEdgePatternClockwise[nextSideOpposite * 2 + 1]];

                                            vertsToAdd.Add(a);
                                            vertsToAdd.Add(b);
                                            vertsToAdd.Add(x);
                                            vertsToAdd.Add(y);

                                            // Debug.Log("LoopA.Count: " + loopA.Count + " loopAStartIndex: " + loopAStartIndex + " loopA[loopAStartIndex] " + loopA[loopAStartIndex] + " closestEdgeSide: " + closestEdgeSide);
                                            // Debug.Log("LoopB.Count: " + loopB.Count + " loopBStartIndex: " + loopBStartIndex + "loopB[loopBStartIndex] " + loopB[loopBStartIndex] + " opposideEdgeSide: " + opposideEdgeSide);

                                            if (loopA.Count > 1 && loopB.Count > 1)
                                            {
                                                #region Test loopB against loopA to see if they travel in the same direction
                                                int loopAInitialDirection = 1;
                                                int loopBInitialDirection = 1;
                                                if (loopAStartIndex + 1 >= loopA.Count)
                                                {
                                                    loopAInitialDirection *= -1;
                                                    loopBInitialDirection *= -1;
                                                }

                                                if (loopBStartIndex + loopBInitialDirection >= loopB.Count || loopBStartIndex + loopBInitialDirection < 0)
                                                {
                                                    loopBInitialDirection *= -1;
                                                }

                                                if (meshEdit.areFacesAdjacent(loopB[loopBStartIndex + loopBInitialDirection], loopA[loopAStartIndex + loopAInitialDirection]))
                                                {
                                                    loopBDirection = loopAInitialDirection * loopBInitialDirection;
                                                }
                                                else
                                                {
                                                    loopBDirection = -loopAInitialDirection * loopBInitialDirection;
                                                }

                                                //loopBDirection *= -1;
                                                // Debug.Log("Direction: " + loopBDirection);
                                                #endregion
                                                // Start forward from the selected face
                                                int c = loopBDirection;
                                                for (int i = loopAStartIndex + 1; i < loopA.Count; i++)
                                                {

                                                    int iOpposite = loopBStartIndex + c;
                                                    c += loopBDirection;
                                                    if (iOpposite >= loopB.Count || iOpposite < 0)
                                                    {
                                                        iOpposite = (iOpposite + loopB.Count) % loopB.Count;
                                                    }

                                                    // Debug.Log("LoopA[" + i + "]: " + loopA[i] + ", LoopB[" + iOpposite + "]: " + loopB[iOpposite]);



                                                    nextSide = meshEdit.getSideOfFaceBThatContinuesSideFromFaceA(loopA[i - 1], nextSide, loopA[i]);
                                                    nextSideOpposite = meshEdit.getSideOfFaceBThatContinuesSideFromFaceA(loopB[(iOpposite + loopB.Count - loopBDirection) % loopB.Count], nextSideOpposite, loopB[iOpposite]);

                                                    if (nextSide >= 0 &&
                                                        nextSideOpposite >= 0)
                                                    {
                                                        int adjacentFaceOpposite = loopB[iOpposite];
                                                        int adjacentFace = loopA[i];

                                                        // Debug.Log("Next face link at adjacentFace: " + adjacentFace + " on side nextSide: " + nextSide);
                                                        // Debug.Log("Next opposite link adjacentFaceOpposite: " + adjacentFaceOpposite + " on side nextSideOpposite: " + nextSideOpposite);

                                                        if (meshEdit.areFacesAdjacent(adjacentFace, adjacentFaceOpposite) &&
                                                            areFacesTrulyAdjacentByUvs(meshEdit, adjacentFace, adjacentFaceOpposite) &&
                                                            isUVFaceAvailableForSelection(meshEdit, adjacentFace) &&
                                                            isUVFaceAvailableForSelection(meshEdit, adjacentFaceOpposite))
                                                        {
                                                            a = meshEdit.quads[loopA[i] * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 0]];
                                                            b = meshEdit.quads[loopA[i] * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 1]];
                                                            x = meshEdit.quads[loopB[iOpposite] * 4 + MeshEdit.quadEdgePatternClockwise[nextSideOpposite * 2 + 0]];
                                                            y = meshEdit.quads[loopB[iOpposite] * 4 + MeshEdit.quadEdgePatternClockwise[nextSideOpposite * 2 + 1]];

                                                            vertsToAdd.Add(a);
                                                            vertsToAdd.Add(b);
                                                            vertsToAdd.Add(x);
                                                            vertsToAdd.Add(y);
                                                        }
                                                        else
                                                        {
                                                            //  Debug.Log("Failed adjacency check: " + (meshEdit.areFacesAdjacent(adjacentFace,  adjacentFaceOpposite))); 
                                                            //  Debug.Log("Failed adjacency check: " + (areFacesTrulyAdjacentByUvs(meshEdit, adjacentFace, adjacentFaceOpposite))); 
                                                            //  Debug.Log("Failed adjacency check: " + (isUVFaceAvailableForSelection(meshEdit, adjacentFace))); 
                                                            // Debug.Log("Failed adjacency check: " + (isUVFaceAvailableForSelection(meshEdit, adjacentFaceOpposite)));
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //Debug.Log("One loop cannot find a side");
                                                        break;
                                                    }
                                                }

                                                // Repeat in reverse
                                                nextSide = closestEdgeSide;
                                                nextSideOpposite = opposideEdgeSide;

                                                c = -loopBDirection;
                                                for (int i = loopAStartIndex - 1; i >= 0; i--)
                                                {

                                                    int iOpposite = loopBStartIndex + c;
                                                    c -= loopBDirection;

                                                    if (iOpposite >= loopB.Count || iOpposite < 0)
                                                    {
                                                        iOpposite = (iOpposite + loopB.Count) % loopB.Count;
                                                    }

                                                    // Debug.Log("LoopA[" + i + "], LoopB[" + iOpposite + "]");



                                                    nextSide = meshEdit.getSideOfFaceBThatContinuesSideFromFaceA(loopA[i + 1], nextSide, loopA[i]);
                                                    nextSideOpposite = meshEdit.getSideOfFaceBThatContinuesSideFromFaceA(loopB[(iOpposite + loopB.Count + loopBDirection) % loopB.Count], nextSideOpposite, loopB[iOpposite]);

                                                    if (nextSide >= 0 &&
                                                        nextSideOpposite >= 0)
                                                    {
                                                        int adjacentFace = meshEdit.adjacentFaces[loopA[i]].list[nextSide];
                                                        int adjacentFaceOpposite = meshEdit.adjacentFaces[loopB[iOpposite]].list[nextSideOpposite];

                                                        if (meshEdit.areFacesAdjacent(adjacentFace, adjacentFaceOpposite) &&
                                                            areFacesTrulyAdjacentByUvs(meshEdit, adjacentFace, adjacentFaceOpposite) &&
                                                            isUVFaceAvailableForSelection(meshEdit, adjacentFace) &&
                                                            isUVFaceAvailableForSelection(meshEdit, adjacentFaceOpposite))
                                                        {


                                                            a = meshEdit.quads[loopA[i] * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 0]];
                                                            b = meshEdit.quads[loopA[i] * 4 + MeshEdit.quadEdgePatternClockwise[nextSide * 2 + 1]];
                                                            x = meshEdit.quads[loopB[iOpposite] * 4 + MeshEdit.quadEdgePatternClockwise[nextSideOpposite * 2 + 0]];
                                                            y = meshEdit.quads[loopB[iOpposite] * 4 + MeshEdit.quadEdgePatternClockwise[nextSideOpposite * 2 + 1]];

                                                            vertsToAdd.Add(a);
                                                            vertsToAdd.Add(b);
                                                            vertsToAdd.Add(x);
                                                            vertsToAdd.Add(y);

                                                        }
                                                        else
                                                        {
                                                            // Debug.Log("Failed adjacency check");
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // Debug.Log("One loop cannot find a side");
                                                        break;
                                                    }
                                                }
                                            }
                                            #endregion
                                        }

                                        // Select the found faces
                                        bool value = true;

                                        if (vertsToAdd.Count > 0)
                                        {
                                            Undo.RegisterCompleteObjectUndo(meshEdit, "Select UV Loop");
                                        }

                                        if (!Event.current.shift)
                                        {
                                            meshEdit.selectedUVs = new bool[meshEdit.selectedVerts.Length];
                                            meshEdit.selectedUVFaces = new bool[meshEdit.selectedFaces.Length];
                                        }
                                        else
                                        {
                                            value = !(meshEdit.selectedUVs[meshEdit.quads[closestEdgeFace * 4 + MeshEdit.quadEdgePatternClockwise[closestEdgeSide * 2 + 0]]] &&
                                                    meshEdit.selectedUVs[meshEdit.quads[closestEdgeFace * 4 + MeshEdit.quadEdgePatternClockwise[closestEdgeSide * 2 + 1]]]);
                                        }


                                        for (int i = 0; i < vertsToAdd.Count; i++)
                                        {
                                            meshEdit.selectedUVs[vertsToAdd[i]] = value;
                                        }
                                    }
                                }
                                #endregion
                            }
                            else
                            {
                                #region Find the closest vert
                                int closestUV = -1;

                                //Debug.Log("MousePos: " + mousePos);
                                for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                                {
                                    if (isUVFaceAvailableForSelection(meshEdit, i))
                                    {
                                        for (int j = 0; j < 4; j++)
                                        {
                                            int v = meshEdit.quads[i * 4 + j];

                                            Vector2 uv = viewMatrix.MultiplyPoint3x4(new Vector3(meshEdit.mesh.uv[v].x, 1.0f - meshEdit.mesh.uv[v].y, 0));
                                            // Debug.Log("uv[" + v + "]: " + uv);

                                            float dd = (uv - localMousePosition).sqrMagnitude;
                                            if (dd < d)
                                            {
                                                d = dd;
                                                closestUV = v;
                                            }
                                        }
                                    }
                                }

                                if (closestUV >= 0)
                                {
                                    bool value = true;

                                    Undo.RegisterCompleteObjectUndo(meshEdit, "Select UV");

                                    if (!Event.current.shift)
                                    {
                                        meshEdit.selectedUVs = new bool[meshEdit.selectedVerts.Length];
                                        meshEdit.selectedUVFaces = new bool[meshEdit.selectedFaces.Length];
                                    }
                                    else
                                    {
                                        value = !meshEdit.selectedUVs[closestUV];
                                    }

                                    meshEdit.selectedUVs[closestUV] = value;

                                    for (int i = 0; i < meshEdit.connectedVerts[closestUV].list.Count; i++)
                                    {
                                        int v = meshEdit.connectedVerts[closestUV].list[i];

                                        int f = meshEdit.quads.IndexOf(v) / 4;

                                        if (isUVFaceAvailableForSelection(meshEdit, f))
                                        {
                                            if ((meshEdit.mesh.uv[v] - meshEdit.mesh.uv[closestUV]).sqrMagnitude < float.Epsilon)
                                            {
                                                meshEdit.selectedUVs[v] = value;
                                            }
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                        else
                        {
                            if (Event.current.modifiers == (EventModifiers.Alt | EventModifiers.Shift) ||
                                Event.current.modifiers == EventModifiers.Alt)
                            {
                                #region Find the closest face and the appropriate side to start the loop from

                                d = 10000 * 10000;

                                int closestEdgeFace = -1;
                                int closestEdgeSide = -1;

                                for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                                {
                                    if (isUVFaceAvailableForSelection(meshEdit, i) &&
                                        mouseOverFace(meshEdit, i, localMousePosition))
                                    {
                                        for (int q = 0; q < 4; q++)
                                        {
                                            int v0 = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[q * 2 + 0]];
                                            int v1 = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[q * 2 + 1]];

                                            {
                                                Vector2 uv0 = viewMatrix.MultiplyPoint3x4(new Vector3(meshEdit.mesh.uv[v0].x, 1.0f - meshEdit.mesh.uv[v0].y, 0));
                                                Vector2 uv1 = viewMatrix.MultiplyPoint3x4(new Vector3(meshEdit.mesh.uv[v1].x, 1.0f - meshEdit.mesh.uv[v1].y, 0));
                                                Vector2 point = closestPoint(uv0, uv1, localMousePosition);
                                                float dd = (point - localMousePosition).sqrMagnitude;

                                                if (dd < d)
                                                {
                                                    d = dd;
                                                    closestEdgeFace = i;
                                                    closestEdgeSide = q;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (closestEdgeFace >= 0)
                                {
                                    List<int> addedFaces = getFaceLoop(meshEdit, closestEdgeFace, closestEdgeSide);

                                    // Select the found faces
                                    bool value = true;

                                    if (addedFaces.Count > 0)
                                    {
                                        Undo.RegisterCompleteObjectUndo(meshEdit, "Select UV Loop");
                                    }

                                    if (!Event.current.shift)
                                    {
                                        meshEdit.selectedUVs = new bool[meshEdit.selectedVerts.Length];
                                        meshEdit.selectedUVFaces = new bool[meshEdit.selectedFaces.Length];
                                    }
                                    else
                                    {
                                        value = !meshEdit.selectedUVFaces[closestEdgeFace];
                                    }



                                    for (int i = 0; i < addedFaces.Count; i++)
                                    {
                                        int f = addedFaces[i];

                                        meshEdit.selectedUVFaces[f] = value;
                                    }
                                }

                                #endregion
                            }
                            else
                            {
                                #region Find the closest face
                                int closestFace = getClosestFaceToMousePos(meshEdit, localMousePosition);

                                if (closestFace >= 0)
                                {
                                    Undo.RegisterCompleteObjectUndo(meshEdit, "Select UV Face");
                                    bool value = true;
                                    if (!Event.current.shift)
                                    {
                                        meshEdit.selectedUVs = new bool[meshEdit.selectedVerts.Length];
                                        meshEdit.selectedUVFaces = new bool[meshEdit.selectedFaces.Length];
                                    }
                                    else
                                    {
                                        value = !meshEdit.selectedUVFaces[closestFace];
                                    }

                                    meshEdit.selectedUVFaces[closestFace] = value;
                                }
                                #endregion
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (transformMode == 1 || transformMode == 3)
                {
                    if (Event.current.keyCode == KeyCode.X ||
                        (Event.current.keyCode == KeyCode.Y && Event.current.shift))
                    {
                        if (transformDimensions.x == 1)
                        {
                            transformDimensions.x = 0;
                            transformDimensions.y = 0;
                        }
                        else
                        {
                            transformDimensions.x = 1;
                            transformDimensions.y = 0;
                        }
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.Y ||
                        (Event.current.keyCode == KeyCode.X && Event.current.shift))
                    {
                        if (transformDimensions.y == 1)
                        {
                            transformDimensions.x = 0;
                            transformDimensions.y = 0;
                        }
                        else
                        {
                            transformDimensions.x = 0;
                            transformDimensions.y = 1;
                        }
                        Event.current.Use();
                    }
                }

                if (Event.current.keyCode == KeyCode.G)
                {
                    moveAnchor = localMousePosition;
                    activateTransformMode(meshEdit, 1);
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.R)
                {
                    moveAnchor = localMousePosition;
                    activateTransformMode(meshEdit, 2);
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.S)
                {
                    moveAnchor = localMousePosition;
                    activateTransformMode(meshEdit, 3);
                    Event.current.Use();
                }
                if (transformMode == 0)
                {
                    if (Event.current.keyCode == KeyCode.A)
                    {
                        Undo.RegisterCompleteObjectUndo(meshEdit, "Select all UVs");
                        selectAllUVs(meshEdit);
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.L)
                    {
                        selectIsland(meshEdit, mousePos - new Vector2(viewportRectangle.xMin, viewportRectangle.yMin));
                        Event.current.Use();
                    }
                    else if (Event.current.keyCode == KeyCode.C)
                    {
                        Event.current.Use();
                    }
                }
            }
        }

    }

    private List<int> getFaceLoop(MeshEdit meshEdit, int closestEdgeFace, int closestEdgeSide)
    {

        // Select all available uvs on the continuous edge that contains the selected face-edge :(
        //meshEdit.loop
        // 1. Get the opposite face to the selected one
        // 2. In both directions, follow the next adjacent faces of both the selected and opposite face
        // 3. If there are any breaks in the next faces not being adjacent anymore (in the selected group) then stop adding verts to the selection
        // 4. If you come across a face that has already been checked, stop immediately.

        List<int> addedFaces = new List<int>();
        addedFaces.Add(closestEdgeFace);

        int originalFace = closestEdgeFace;
        int originalSide = closestEdgeSide;

        int cc = 0;
        int nextFace = meshEdit.adjacentFaces[closestEdgeFace].list[closestEdgeSide];
        while (nextFace != -1 && isUVFaceAvailableForSelection(meshEdit, nextFace) && !addedFaces.Contains(nextFace))
        {
            cc++;
            if (cc > 10000)
            {
                Debug.LogError("While loop too large!!!");
                break;
            }

            int oppositeSide = meshEdit.adjacentFaces[nextFace].list.IndexOf(closestEdgeFace);

            if (!areFacesTrulyAdjacentByUvs(meshEdit, nextFace, closestEdgeFace))
            {
                break;
            }

            addedFaces.Add(nextFace);

            closestEdgeSide = (oppositeSide + 2) % 4;
            closestEdgeFace = nextFace;

            nextFace = meshEdit.adjacentFaces[closestEdgeFace].list[closestEdgeSide];
        }

        // Try again in the other direction
        closestEdgeFace = originalFace;
        closestEdgeSide = (originalSide + 2) % 4;

        cc = 0;
        nextFace = meshEdit.adjacentFaces[closestEdgeFace].list[closestEdgeSide];
        while (nextFace != -1 && isUVFaceAvailableForSelection(meshEdit, nextFace) && !addedFaces.Contains(nextFace))
        {
            cc++;
            if (cc > 10000)
            {
                Debug.LogError("While loop too large!!!");
                break;
            }




            int oppositeSide = meshEdit.adjacentFaces[nextFace].list.IndexOf(closestEdgeFace);

            if (!areFacesTrulyAdjacentByUvs(meshEdit, nextFace, closestEdgeFace))
            {
                break;
            }

            addedFaces.Insert(0, nextFace);

            closestEdgeSide = (oppositeSide + 2) % 4;
            closestEdgeFace = nextFace;

            nextFace = meshEdit.adjacentFaces[closestEdgeFace].list[closestEdgeSide];
        }

        return addedFaces;
    }

    private void verifySelectionArray(MeshEdit meshEdit)
    {
        if (meshEdit.selectedVerts != null)
        {
            if (meshEdit.selectedUVs == null || meshEdit.selectedUVs.Length != meshEdit.selectedVerts.Length)
            {
                meshEdit.selectedUVs = new bool[meshEdit.selectedVerts.Length];
            }
        }
        if (meshEdit.selectedFaces != null)
        {
            if (meshEdit.selectedUVFaces == null || meshEdit.selectedUVFaces.Length != meshEdit.selectedFaces.Length)
            {
                meshEdit.selectedUVFaces = new bool[meshEdit.selectedFaces.Length];
            }
        }
    }

    void OnLostFocus()
    {
        transformMode = 0;
        transformDimensions.x = 0;
        transformDimensions.y = 0;
    }

    private bool areFacesTrulyAdjacentByUvs(MeshEdit meshEdit, int faceA, int faceB)
    {
        // Check if the face is connected on that side by proximity
        int sideA = meshEdit.adjacentFaces[faceA].list.IndexOf(faceB);
        int sideB = meshEdit.adjacentFaces[faceB].list.IndexOf(faceA);

        int vA = meshEdit.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[sideA * 2 + 0]];
        int vB = meshEdit.quads[faceA * 4 + MeshEdit.quadEdgePatternClockwise[sideA * 2 + 1]];
        int vAOpposite = meshEdit.quads[faceB * 4 + MeshEdit.quadEdgePatternClockwise[sideB * 2 + 0]];
        int vBOpposite = meshEdit.quads[faceB * 4 + MeshEdit.quadEdgePatternClockwise[sideB * 2 + 1]];

        if (!meshEdit.isShared(vA, vAOpposite))
        {
            int temp = vA;
            vA = vB;
            vB = temp;
        }

        if ((meshEdit.mesh.uv[vA] - meshEdit.mesh.uv[vAOpposite]).sqrMagnitude > float.Epsilon ||
            (meshEdit.mesh.uv[vB] - meshEdit.mesh.uv[vBOpposite]).sqrMagnitude > float.Epsilon)
        {
            return false;
        }
        return true;
    }

    private int getClosestFaceToMousePos(MeshEdit meshEdit, Vector2 mousePos)
    {
        int closestFace = -1;

        float d = 10000 * 10000;

        //Debug.Log("MousePos: " + mousePos);
        for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
        {
            if (isUVFaceAvailableForSelection(meshEdit, i) &&
                mouseOverFace(meshEdit, i, mousePos))
            {
                Vector2 center =
                new Vector2(meshEdit.mesh.uv[meshEdit.quads[i * 4 + 0]].x, meshEdit.mesh.uv[meshEdit.quads[i * 4 + 0]].y) +
                new Vector2(meshEdit.mesh.uv[meshEdit.quads[i * 4 + 1]].x, meshEdit.mesh.uv[meshEdit.quads[i * 4 + 1]].y) +
                new Vector2(meshEdit.mesh.uv[meshEdit.quads[i * 4 + 2]].x, meshEdit.mesh.uv[meshEdit.quads[i * 4 + 2]].y) +
                new Vector2(meshEdit.mesh.uv[meshEdit.quads[i * 4 + 3]].x, meshEdit.mesh.uv[meshEdit.quads[i * 4 + 3]].y);

                center /= 4;

                center = viewMatrix.MultiplyPoint3x4(new Vector3(center.x, 1.0f - center.y, 0.0f));
                // Debug.Log("uv[" + v + "]: " + uv);

                float dd = (center - mousePos).sqrMagnitude;
                if (dd < d)
                {
                    d = dd;
                    closestFace = i;
                }
            }
        }

        return closestFace;
    }

    private int getClosestVertToMousePos(MeshEdit meshEdit, Vector2 mousePos, out int face)
    {
        int closestVert = -1;
        int closestFace = -1;

        float d = 10000 * 10000;

        //Debug.Log("MousePos: " + mousePos);
        for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
        {
            for (int ii = 0; ii < 4; ii++)
            {
                int iii = meshEdit.quads[i * 4 + ii];
                if (meshEdit.selectedVerts[iii])
                {
                    Vector2 center = meshEdit.mesh.uv[iii];
                    
                    center = viewMatrix.MultiplyPoint3x4(new Vector3(center.x, 1.0f - center.y, 0.0f));
                    // Debug.Log("uv[" + v + "]: " + uv);

                    float dd = (center - mousePos).sqrMagnitude;
                    if (dd < d)
                    {
                        d = dd;
                        closestFace = i;
                        closestVert = iii;
                    }
                }
            }
        }

        face = closestFace;
        return closestVert;
    }

    private void selectIsland(MeshEdit meshEdit, Vector2 mousePos)
    {
        if (!Event.current.shift)
        {
            meshEdit.selectedUVs = new bool[meshEdit.selectedVerts.Length];
            meshEdit.selectedUVFaces = new bool[meshEdit.selectedFaces.Length];
        }

        int selectedFace = 0;
        if (meshEdit.vertMode == 0)
        {
            meshEdit.selectionConvertToFaces();
        }

        selectedFace = getClosestFaceToMousePos(meshEdit, mousePos);

        if (selectedFace >= 0)
        {
            Stack<int> facesToCheck = new Stack<int>();
            facesToCheck.Push(selectedFace);
            
            bool[] checkedFaces = new bool[meshEdit.selectedUVFaces.Length];

            int cc = 0;
            while (facesToCheck.Count > 0)
            {
                cc++;
                if (cc > 10000)
                {
                    Debug.LogError("Too many face checks, loop broken.");
                    break;
                }

                int currentFace = facesToCheck.Pop();

                meshEdit.selectedUVFaces[currentFace] = true;
                meshEdit.selectedUVs[meshEdit.quads[currentFace * 4 + 0]] = true;
                meshEdit.selectedUVs[meshEdit.quads[currentFace * 4 + 1]] = true;
                meshEdit.selectedUVs[meshEdit.quads[currentFace * 4 + 2]] = true;
                meshEdit.selectedUVs[meshEdit.quads[currentFace * 4 + 3]] = true;
                checkedFaces[currentFace] = true;

                for (int i = 0; i < meshEdit.adjacentFaces[currentFace].list.Count; i++)
                {
                    int adjacentFace = meshEdit.adjacentFaces[currentFace].list[i];
                    if (isUVFaceAvailableForSelection(meshEdit, adjacentFace))
                    {
                        if (!checkedFaces[adjacentFace])
                        {
                            if (areFacesTrulyAdjacentByUvs(meshEdit, adjacentFace, currentFace))
                            {
                                facesToCheck.Push(adjacentFace);
                            }
                        }
                    }
                }
            }
        }
    }

    public static float cross(Vector2 a, Vector2 b) 
    {
        return a.x * b.y - a.y * b.x;
    }

    private bool isPointOnSameSideOfTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float crossA = cross(b - a, point - a);
        float crossB = cross(b - a, c - a);
        if (Mathf.Sign(crossA) == Mathf.Sign(crossB))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    private bool mouseOverFace(MeshEdit meshEdit, int i, Vector2 mousePos)
    {
        Vector2 a = meshEdit.mesh.uv[meshEdit.quads[i * 4 + 0]];
        Vector2 b = meshEdit.mesh.uv[meshEdit.quads[i * 4 + 1]];
        Vector2 c = meshEdit.mesh.uv[meshEdit.quads[i * 4 + 2]];
        Vector2 d = meshEdit.mesh.uv[meshEdit.quads[i * 4 + 3]];
        a = viewMatrix.MultiplyPoint3x4(new Vector3(a.x, 1.0f - a.y, 0.0f));
        b = viewMatrix.MultiplyPoint3x4(new Vector3(b.x, 1.0f - b.y, 0.0f));
        c = viewMatrix.MultiplyPoint3x4(new Vector3(c.x, 1.0f - c.y, 0.0f));
        d = viewMatrix.MultiplyPoint3x4(new Vector3(d.x, 1.0f - d.y, 0.0f));

        // Evaluate Triangle A
        if (isPointOnSameSideOfTriangle(mousePos, a, b, c) &&
            isPointOnSameSideOfTriangle(mousePos, b, a, c) &&
            isPointOnSameSideOfTriangle(mousePos, c, a, b))
        {
            return true;
        }
        // Evaluate Triangle B
        if (isPointOnSameSideOfTriangle(mousePos, d, c, b) &&
            isPointOnSameSideOfTriangle(mousePos, c, d, b) &&
            isPointOnSameSideOfTriangle(mousePos, b, d, c))
        {
            return true;
        }

        return false;
    }

    public bool isUVFaceAvailableForSelection(MeshEdit meshEdit, int face)
    {
        if (face == -1)
        {
            return false;
        }

        if (!showOnlySelectedVerts)
        {
            return true;
        }

        if (meshEdit.vertMode == 0)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!meshEdit.selectedVerts[meshEdit.quads[face * 4 + i]])
                {
                    return false;
                }
            }

            return true;
        }
        else
        {
            return meshEdit.selectedFaces[face];
        }
    }

    public void glDrawUVArea(Rect rect, MeshEdit meshEdit)
    {
        verifySelectionArray(meshEdit);

        uvGUIPosition = new Vector2(rect.xMin, rect.yMin);

        float size = Mathf.Min(rect.width, rect.height);

        textureRectangle = new Rect(0, 0, size, size);
        if (backgroundTexture != null)
        {
            textureRectangle = new Rect(0, 0, backgroundTexture.width, backgroundTexture.height);
        }

        oldViewportRectangle = viewportRectangle;
        viewportRectangle = rect;

        if (viewportRectangle != oldViewportRectangle)
        {
            refreshMaterials(meshEdit);
            //setMaterialClipRectangle((int)viewportRectangle.xMin, (int)viewportRectangle.xMax, (int)viewportRectangle.yMin, (int)viewportRectangle.yMax);

            setMaterialClipRectangle(0, (int)viewportRectangle.width, 0, (int)viewportRectangle.height);

            Repaint();
        }

        viewMatrix = Matrix4x4.TRS(new Vector3(uvViewPos.x, uvViewPos.y, 0.0f), Quaternion.identity, new Vector3(textureRectangle.width * zoom, textureRectangle.height * zoom, 1));

        //GUI.BeginClip(new Rect(0.0f, 0.0f, viewportRectangle.width, viewportRectangle.height));


        // THIS IS THE CORRECT WAY TO DRAW A CLIP       
        // A clip that's outside of the window viewport (scrolled away from) will not be drawn, the space it takes up is based on THIS RECTANGLE.
        GUI.BeginClip(new Rect(viewportRectangle.xMin,viewportRectangle.yMin, viewportRectangle.width, viewportRectangle.height));
        if (Event.current.type == EventType.Repaint)
        {
            if (meshEdit &&
            meshEdit.selectedVerts != null &&
            meshEdit.selectedFaces != null)
            {
                Renderer r = meshEdit.GetComponent<Renderer>();
                if (r)
                {
                    Material m = r.sharedMaterial;

                    if (m)
                    {
                        Texture tex = m.GetTexture("_MainTex");

                        if (tex)
                        {
                            // GUI.DrawTexture(uvRectangle, texture);
                        }
                    }

                }


                createLineMaterial();
                createTextureMat(meshEdit);


                float wRatio = (viewportRectangle.width / textureRectangle.width ) / zoom;
                float hRatio = (viewportRectangle.height / textureRectangle.height) / zoom;

                float offsetXRatio = (uvViewPos.x / viewportRectangle.width) * wRatio;
                float offsetYRatio = (uvViewPos.y / viewportRectangle.height) * hRatio;

                GL.PushMatrix();
                GL.MultMatrix(viewMatrix);
                GL.Clear(true, false, Color.black);
                glDrawGrid(wRatio, hRatio, 1, offsetXRatio , offsetYRatio);
                /* draw pip 2
                 * *?
                 */


                GL.PushMatrix();
                if (textureMat != null)
                {
                    textureMat.SetPass(0);

                    //GL.MultMatrix(Matrix4x4.Scale(new Vector3(1.0f / wRatio, 1.0f / hRatio, 1.0f)));

                    GL.Begin(GL.QUADS);
                    GL.TexCoord(new Vector3(1, 0, 0));
                    GL.Vertex3(1, 1, 0);
                    GL.TexCoord(new Vector3(0, 0, 0));
                    GL.Vertex3(0, 1, 0);
                    GL.TexCoord(new Vector3(0, 1, 0));
                    GL.Vertex3(0, 0, 0);
                    GL.TexCoord(new Vector3(1, 1, 0));
                    GL.Vertex3(1, 0, 0);
                    GL.End();
                }

                lineMat.SetPass(0);

                Vector2[] uvs = meshEdit.mesh.uv;

                if (meshEdit.vertMode == 0)
                {
                    // Lines
                    GL.Begin(GL.LINES);
                    GL.Color(Color.black);
                    for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                    {
                        if (isUVFaceAvailableForSelection(meshEdit, i))
                        {
                            for (int q = 0; q < 4; q++)
                            {
                                int v0 = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[q * 2 + 0]];
                                int v1 = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[q * 2 + 1]];
                                
                                    if (meshEdit.selectedUVs[v0])
                                    {
                                        GL.Color(Color.cyan);
                                    }
                                    else
                                    {
                                        GL.Color(Color.black);
                                    }

                                    GL.Vertex3(uvs[v0].x, 1.0f - uvs[v0].y, 0);

                                    if (meshEdit.selectedUVs[v1])
                                    {
                                        GL.Color(Color.cyan);
                                    }
                                    else
                                    {
                                        GL.Color(Color.black);
                                    }

                                    GL.Vertex3(uvs[v1].x, 1.0f - uvs[v1].y, 0);
                                
                            }
                        }
                    }
                    // Selected Lines
                    for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                    {
                        if (isUVFaceAvailableForSelection(meshEdit, i))
                        {
                            for (int q = 0; q < 4; q++)
                            {
                                int v0 = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[q * 2 + 0]];
                                int v1 = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[q * 2 + 1]];

                                if (meshEdit.selectedUVs[v0] && meshEdit.selectedUVs[v1])
                                {
                                    GL.Color(Color.cyan);

                                    GL.Vertex3(uvs[v0].x, 1.0f - uvs[v0].y, 0);
                                    GL.Vertex3(uvs[v1].x, 1.0f - uvs[v1].y, 0);
                                }
                            }
                        }
                    }
                    GL.End();
                    // Pips
                    float zX = 1.0f / (textureRectangle.width * zoom) * 2f;
                    float zY = 1.0f / (textureRectangle.height * zoom)  * 2f;
                    GL.Begin(GL.QUADS);
                    GL.Color(Color.black);
                    for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                    {
                        if (isUVFaceAvailableForSelection(meshEdit, i))
                        {
                            for (int q = 0; q < 4; q++)
                            {
                                int v = meshEdit.quads[i * 4 + q];
                                
                                GL.Color(Color.black);
                              
                                GL.Vertex3(uvs[v].x - zX, 1.0f - uvs[v].y - zY, 0);
                                GL.Vertex3(uvs[v].x + zX, 1.0f - uvs[v].y - zY, 0);
                                GL.Vertex3(uvs[v].x + zX, 1.0f - uvs[v].y + zY, 0);
                                GL.Vertex3(uvs[v].x - zX, 1.0f - uvs[v].y + zY, 0);
                                
                            }
                        }
                    }
                    // Selected Pips & Pins
                    for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                    {
                        if (isUVFaceAvailableForSelection(meshEdit, i))
                        {
                            for (int q = 0; q < 4; q++)
                            {
                                int v = meshEdit.quads[i * 4 + q];

                                if (meshEdit.selectedUVs[v])
                                {
                                    GL.Color(Color.cyan);

                                    GL.Vertex3(uvs[v].x - zX, 1.0f - uvs[v].y - zY, 0);
                                    GL.Vertex3(uvs[v].x + zX, 1.0f - uvs[v].y - zY, 0);
                                    GL.Vertex3(uvs[v].x + zX, 1.0f - uvs[v].y + zY, 0);
                                    GL.Vertex3(uvs[v].x - zX, 1.0f - uvs[v].y + zY, 0);
                                }

                                if (meshEdit.pinnedUVs != null && meshEdit.pinnedUVs.Length > v && meshEdit.pinnedUVs[v])
                                {
                                    GL.Color(Color.black);
                                    GL.Vertex3(uvs[v].x, 1.0f - uvs[v].y - zY * 1f, 0);
                                    GL.Vertex3(uvs[v].x + zX * 2, 1.0f - uvs[v].y - zY * 5f, 0);
                                    GL.Vertex3(uvs[v].x - zX * 2, 1.0f - uvs[v].y - zY * 5f, 0);
                                    GL.Vertex3(uvs[v].x, 1.0f - uvs[v].y - zY * 1f, 0);
                                    GL.Color(Color.red);
                                    GL.Vertex3(uvs[v].x, 1.0f - uvs[v].y - zY * 2f, 0);
                                    GL.Vertex3(uvs[v].x + zX, 1.0f - uvs[v].y - zY * 4f, 0);
                                    GL.Vertex3(uvs[v].x - zX, 1.0f - uvs[v].y - zY * 4f, 0);
                                    GL.Vertex3(uvs[v].x, 1.0f - uvs[v].y - zY * 2f, 0);
                                }
                            }
                        }
                    }
                        GL.End();
                }
                else
                {
                    // Lines

                    // Non-selected
                    for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                    {
                        if (isUVFaceAvailableForSelection(meshEdit, i) &&
                            !meshEdit.selectedUVFaces[i])
                        {
                            GL.Begin(GL.LINE_STRIP);
                            
                            GL.Color(Color.black);
                            

                            for (int q = 0; q < 5; q++)
                            {
                                int v = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[(q % 4) * 2]];

                                GL.Vertex3(uvs[v].x, 1.0f - uvs[v].y, 0);
                            }
                            GL.End();
                        }
                    }
                    // Selected
                    for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                    {
                        if (isUVFaceAvailableForSelection(meshEdit, i) &&
                            meshEdit.selectedUVFaces[i])
                        {
                            GL.Begin(GL.LINE_STRIP);
                            
                            GL.Color(Color.cyan);

                            for (int q = 0; q < 5; q++)
                            {
                                int v = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[(q % 4) * 2]];

                                GL.Vertex3(uvs[v].x, 1.0f - uvs[v].y, 0);
                            }
                            GL.End();
                        }
                    }
                    // Pips
                    float zX = 1.0f / (textureRectangle.width * zoom) * 2f;
                    float zY = 1.0f / (textureRectangle.height * zoom) * 2f;
                    GL.Begin(GL.QUADS);
                    GL.Color(Color.black);
                    for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                    {
                        if (isUVFaceAvailableForSelection(meshEdit, i))
                        {
                            Vector2 quadCenter = Vector2.zero;

                            for (int q = 0; q < 4; q++)
                            {
                                int v = meshEdit.quads[i * 4 + MeshEdit.quadEdgePatternClockwise[(q % 4) * 2]];

                                quadCenter += uvs[v];

                                if (meshEdit.pinnedUVs != null && meshEdit.pinnedUVs.Length > v && meshEdit.pinnedUVs[v])
                                {
                                    GL.Color(Color.black);
                                    GL.Vertex3(uvs[v].x, 1.0f - uvs[v].y - zY * 1f, 0);
                                    GL.Vertex3(uvs[v].x + zX * 2, 1.0f - uvs[v].y - zY * 5f, 0);
                                    GL.Vertex3(uvs[v].x - zX * 2, 1.0f - uvs[v].y - zY * 5f, 0);
                                    GL.Vertex3(uvs[v].x, 1.0f - uvs[v].y - zY * 1f, 0);
                                    GL.Color(Color.red);
                                    GL.Vertex3(uvs[v].x, 1.0f - uvs[v].y - zY * 2f, 0);
                                    GL.Vertex3(uvs[v].x + zX, 1.0f - uvs[v].y - zY * 4f, 0);
                                    GL.Vertex3(uvs[v].x - zX, 1.0f - uvs[v].y - zY * 4f, 0);
                                    GL.Vertex3(uvs[v].x, 1.0f - uvs[v].y - zY * 2f, 0);

                                }
                            }

                            quadCenter /= 4;

                            if (meshEdit.selectedUVFaces[i])
                            {
                                GL.Color(Color.cyan);
                            }
                            else
                            {
                                GL.Color(Color.black);
                            }
                            GL.Vertex3(quadCenter.x - zX, 1.0f - quadCenter.y - zY, 0);
                            GL.Vertex3(quadCenter.x + zX, 1.0f - quadCenter.y - zY, 0);
                            GL.Vertex3(quadCenter.x + zX, 1.0f - quadCenter.y + zY, 0);
                            GL.Vertex3(quadCenter.x - zX, 1.0f - quadCenter.y + zY, 0);

                        }
                    }
                    GL.End();
                }
                // Transform guides
                if (transformMode == 2)
                {
                    /* rotation center pip
                    GL.Begin(GL.QUADS);
                    GL.Color(Color.red);
                    GL.Vertex3(anchorCenter.x - z, anchorCenter.y - z, 0);
                    GL.Vertex3(anchorCenter.x + z,   anchorCenter.y - z, 0);
                    GL.Vertex3(anchorCenter.x + z,  anchorCenter.y + z, 0);
                    GL.Vertex3(anchorCenter.x - z,   anchorCenter.y + z, 0);
                    GL.End();
                    */
                }
                GL.PopMatrix();

                GL.PopMatrix();
            }
        }
        GUI.EndClip();
    }

    private void cleanSelectedUVs(MeshEdit meshEdit)
    {
        if (meshEdit.vertMode == 0)
        {
            for (int f = 0; f < meshEdit.selectedFaces.Length; f++)
            {
                for (int ii = 0; ii < 4; ii++)
                {
                    int i = meshEdit.quads[f * 4 + ii];

                    if (meshEdit.selectedUVs[i])
                    {
                        // Check for surrounding verts that are both connected on the main mesh, and very close in proximity on the UV editor
                        for (int j = 0; j < meshEdit.connectedVerts[i].list.Count; j++)
                        {
                            int v = meshEdit.connectedVerts[i].list[j];

                            if (isUVFaceAvailableForSelection(meshEdit, f))
                            {
                                if ((meshEdit.mesh.uv[i] - meshEdit.mesh.uv[v]).sqrMagnitude < float.Epsilon)
                                {
                                    meshEdit.selectedUVs[v] = true;
                                }
                            }
                        }
                    }
                }
            }

            for (int f = 0; f < meshEdit.selectedFaces.Length; f++)
            {
                for (int ii = 0; ii < 4; ii++)
                {
                    int i = meshEdit.quads[f * 4 + ii];

                    // Deselect any verts that are not selected on the mesh
                    if (!isUVFaceAvailableForSelection(meshEdit, f))
                    {
                        meshEdit.selectedUVs[i] = false;
                    }
                }
            }
        }
        else
        {
            meshEdit.selectedUVs = new bool[meshEdit.selectedUVs.Length];
            
            for (int i = 0; i < meshEdit.selectedUVFaces.Length; i++)
            {
                if (!isUVFaceAvailableForSelection(meshEdit, i))
                {
                    meshEdit.selectedUVFaces[i] = false;
                }

                if (isUVFaceAvailableForSelection(meshEdit, i))
                {
                    for (int q = 0; q < 4; q++)
                    {
                        if (meshEdit.selectedUVFaces[i])
                        {
                            int v = meshEdit.quads[i * 4 + q];

                            meshEdit.selectedUVs[v] = true;

                            for (int j = 0; j < meshEdit.connectedVerts[v].list.Count; j++)
                            {
                                int vv = meshEdit.connectedVerts[v].list[j];
                                int touchingFace = meshEdit.quads.IndexOf(vv) / 4;

                                if (isUVFaceAvailableForSelection(meshEdit, touchingFace) &&
                                    (meshEdit.mesh.uv[vv] - meshEdit.mesh.uv[v]).sqrMagnitude < float.Epsilon)
                                {
                                    meshEdit.selectedUVs[vv] = true;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void activateTransformMode(MeshEdit meshEdit, int transformMode)
    {
        cleanSelectedUVs(meshEdit);

        if (this.transformMode == 0)
        {
            Undo.RegisterCompleteObjectUndo(meshEdit, "UV Transformation");

            transformDimensions = Vector2.zero;
        }
        else if (this.transformMode != transformMode && this.transformMode != 0)
        {
            setUVs(meshEdit, oldUVs);
        }

        this.transformMode = transformMode;

        oldUVs = new Vector2[meshEdit.mesh.uv.Length];

        int c = 0;
        anchorCenter = Vector2.zero;
        for (int i = 0; i < oldUVs.Length; i++)
        {
            if (meshEdit.selectedUVs[i])
            {
                anchorCenter += meshEdit.mesh.uv[i];
                c++;
            }
            oldUVs[i] = meshEdit.mesh.uv[i];
        }
        anchorCenter /= c;
        /*anchorCenter = viewMatrix.MultiplyPoint3x4(new Vector3(
             anchorCenter.x, 
             (1.0f - anchorCenter.y), 
             0)); 
         Debug.Log("Activating Mode Mode: " + this.transformMode + " with center " + anchorCenter.ToString());*/
    }

    public void selectAllUVs(MeshEdit meshEdit)
    {
        bool selectAll = false;

        if (meshEdit.vertMode == 0)
        {
            for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
            {
                for (int q = 0; q < 4; q++)
                {
                    int v = meshEdit.quads[i * 4 + q];

                    if (isUVFaceAvailableForSelection(meshEdit, i))
                    {
                        if (!meshEdit.selectedUVs[v])
                        {
                            selectAll = true;
                            break;
                        }
                    }
                }
            }
            if (selectAll)
            {
                for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                {
                    for (int q = 0; q < 4; q++)
                    {
                        int v = meshEdit.quads[i * 4 + q];
                        if (isUVFaceAvailableForSelection(meshEdit, i))
                        {
                            meshEdit.selectedUVs[v] = true;
                        }
                    }
                }
            }
            else
            {
                meshEdit.selectedUVs = new bool[meshEdit.selectedUVs.Length];
            }
        }
        else
        {
            for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
            {
                if (meshEdit.selectedFaces[i])
                {
                    if (!meshEdit.selectedUVFaces[i])
                    {
                        selectAll = true;
                        break;
                    }
                }

            }
            if (selectAll)
            {
                for (int i = 0; i < meshEdit.selectedFaces.Length; i++)
                {
                    if (meshEdit.selectedFaces[i])
                    {
                        meshEdit.selectedUVFaces[i] = true;
                    }
                }

            }
            else
            {
                meshEdit.selectedUVFaces = new bool[meshEdit.selectedUVFaces.Length];
            }
        }
    }


    public class GLTilesetInterface
    {
        Rect viewportRectangle;
        Vector2 viewPosition;
        Vector2 windowMousePosition;

        Matrix4x4 viewMatrix;
        public float zoom = 1;

        public int areaWidth = 1000;
        public int areaHeight = 1000;
        public float gridUnitW = 100;
        public float gridUnitH = 100;

        private bool canDrag;

        public bool constrainView = true;

        public Texture texture;
        public Material textureMat;
        
        public void createTextureMaterial()
        {
            if (textureMat == null)
            {
                Shader shader = Shader.Find("MeshEdit/GUI2");
                textureMat = new Material(shader);
                textureMat.hideFlags = HideFlags.HideAndDontSave;

                if (texture != null)
                {
                    textureMat.SetTexture("_MainTex", texture);
                }
            }
        }

        public void updateEvents(MeshEdit solid)
        {
            bool shift = Event.current.shift;
            bool ctrl = Event.current.control || Event.current.command;
            bool alt = Event.current.alt;
            windowMousePosition = Event.current.mousePosition;
            Vector2 localMousePosition = windowMousePosition - new Vector2(viewportRectangle.xMin, viewportRectangle.yMin);

            if (Event.current.type == EventType.MouseUp)
            {
                canDrag = false;
            }
            
            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint &&
                !viewportRectangle.Contains(windowMousePosition) && !(Event.current.type == EventType.MouseDrag && canDrag))
            {
                canDrag = false;
                return;
            }

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 2)
            {
                canDrag = true;
            }

            if (Event.current.type == EventType.ScrollWheel)
            {
                if (Event.current.delta.y > 0)
                {
                    if (zoom > 0.1f)
                    {
                        viewPosition -= localMousePosition;
                        zoom /= 1.25f;
                        viewPosition /= 1.25f;
                        viewPosition += localMousePosition;

                        if (constrainView)
                        {
                            constrainViewposition();
                        }
                    }
                    Event.current.Use();
                }
                else if (Event.current.delta.y < 0)
                {
                    if (zoom < 100.0f)
                    {
                        viewPosition -= localMousePosition;
                        zoom *= 1.25f;
                        viewPosition *= 1.25f;
                        viewPosition += localMousePosition;

                        if (constrainView)
                        {
                            constrainViewposition();
                        }
                    }
                    Event.current.Use();
                }
            }
            else if (Event.current.isMouse)
            {
                if (Event.current.type == EventType.MouseDrag && canDrag)
                {
                    if (Event.current.button == 2)
                    {
                        viewPosition += Event.current.delta;

                        if (constrainView)
                        {
                            constrainViewposition();
                        }
                    }
                }
                // Tileset events
                updateTilesetEvents(solid);
            }

            constrainViewposition();
        }

        public void constrainViewposition()
        {
            float pixelMargin = 32.0f;
            if (viewPosition.x > pixelMargin)
            {
                viewPosition.x = pixelMargin;
            }
            if (viewPosition.y > pixelMargin)
            {
                viewPosition.y = pixelMargin;
            }
            float xLimit = -areaWidth * zoom + viewportRectangle.width - pixelMargin;

            if (viewPosition.x < xLimit)
            {
                viewPosition.x = xLimit;
            }
            float yLimit = -areaHeight * zoom + viewportRectangle.height - pixelMargin;

            if (viewPosition.y < yLimit)
            {
                viewPosition.y = yLimit;
            }

            if (areaWidth * zoom + pixelMargin * 2 < viewportRectangle.width)
            {
                viewPosition.x = (viewportRectangle.width - (areaWidth * zoom)) / 2;
            }
            if (areaHeight * zoom + pixelMargin * 2 < viewportRectangle.height )
            {
                viewPosition.y = (viewportRectangle.height - (areaHeight * zoom)) / 2;
            }
        }

        public void setViewportSize(Rect rect)
        {
            createLineMaterial();
            createAlphaMaterial();

            if (rect != viewportRectangle)
            {
                viewportRectangle = rect;

            }

            if (lineMat != null)
            {
                lineMat.SetInt("_ClipLeft", 0);
                lineMat.SetInt("_ClipTop", 0);
                lineMat.SetInt("_ClipRight", (int)viewportRectangle.width);
                lineMat.SetInt("_ClipBottom", (int)viewportRectangle.height);
            }
            if (textureMat != null)
            {

                textureMat.SetInt("_ClipLeft", 0);
                textureMat.SetInt("_ClipTop", 0);
                textureMat.SetInt("_ClipRight", (int)viewportRectangle.width);
                textureMat.SetInt("_ClipBottom", (int)viewportRectangle.height);
            }
            if (alphaMaterial != null)
            {

                alphaMaterial.SetInt("_ClipLeft", 0);
                alphaMaterial.SetInt("_ClipTop", 0);
                alphaMaterial.SetInt("_ClipRight", (int)viewportRectangle.width);
                alphaMaterial.SetInt("_ClipBottom", (int)viewportRectangle.height);
            }
        }

        public void updateViewMatrix()
        {
            viewMatrix = Matrix4x4.TRS(new Vector3(viewPosition.x, viewPosition.y, 0.0f), Quaternion.identity, new Vector3((areaWidth) * zoom, (areaHeight) * zoom, 1));
        }

        public void glBeginInterface()
        {

            GL.PushMatrix();
            GL.MultMatrix(viewMatrix);
        }

        public void glEndInterface()
        {
            GL.PopMatrix();
        }
        
        public void glDrawInterface(MeshEdit solid, int selectedTile)
        {
            if (lineMat == null)
            {
                createLineMaterial();

            }
            float wRatio = (viewportRectangle.width / areaWidth) / zoom;
            float hRatio = (viewportRectangle.height / areaHeight) / zoom;

            float offsetXRatio = (viewPosition.x / viewportRectangle.width) * wRatio;
            float offsetYRatio = (viewPosition.y / viewportRectangle.height) * hRatio;

            glDrawGrid(wRatio, hRatio, 1, offsetXRatio, offsetYRatio);

            if (texture != null)
            {
                glDrawTileset(solid, 1, selectedTile);
            }
        }

        private void glDrawGrid(float w, float h, float zoom, float offsetX, float offsetY)
        {
            float left = 0 - offsetX;
            float right = left + w;
            float bottom = 0 - offsetY;
            float top = bottom + h;

            lineMat.SetPass(0);

            GL.Begin(GL.QUADS);
            GL.Color(new Color(0.125f, 0.125f, 0.125f, 1.0f));
            GL.Vertex3(left, top, 0.0f);
            GL.Vertex3(left, bottom, 0.0f);
            GL.Vertex3(right, bottom, 0.0f);
            GL.Vertex3(right, top, 0.0f);
            GL.End();

            left = 0;
            right = 1.0f;
            top = 0;
            bottom = 1.0f;

            GL.Begin(GL.QUADS);
            float g = 80.0f / 256.0f;
            GL.Color(new Color(g, g, g, 1));

            GL.Vertex3(right, bottom, 0);
            GL.Vertex3(left, bottom, 0);
            GL.Vertex3(left, top, 0);
            GL.Vertex3(right, top, 0);

            GL.End();
            /*
            float startY = Mathf.Floor(bottom / gridUnitH) * gridUnitH;

            float startX = Mathf.Floor(left / gridUnitW) * gridUnitW;

            for (float y = startY; y < top; y += gridUnitW)
            {
                GL.Begin(GL.LINES);
                GL.Color(new Color(lineLight, lineLight, lineLight, 1));

                GL.Vertex3(left, y, 0);
                GL.Vertex3(right, y, 0);
                GL.End();
            }
            for (float x = startX; x < right; x += gridUnitH)
            {
                GL.Begin(GL.LINES);
                GL.Color(new Color(lineLight, lineLight, lineLight, 1));

                GL.Vertex3(x, top, 0);
                GL.Vertex3(x, bottom, 0);
                GL.End();
            }

            if (drawHeavyGrid)
            {
                startY = Mathf.Floor(bottom / gridUnitH) * gridUnitH;

                startX = Mathf.Floor(left / gridUnitW) * gridUnitW;

                for (float y = startY; y < top; y += gridUnitW)
                {
                    GL.Begin(GL.LINES);
                    GL.Color(new Color(lineHeavy, lineHeavy, lineHeavy, 1));

                    GL.Vertex3(left, y, 0);
                    GL.Vertex3(right, y, 0);
                    GL.End();
                }
                for (float x = startX; x < right; x += gridUnitH)
                {
                    GL.Begin(GL.LINES);
                    GL.Color(new Color(lineHeavy, lineHeavy, lineHeavy, 1));

                    GL.Vertex3(x, top, 0);
                    GL.Vertex3(x, bottom, 0);
                    GL.End();
                }

            }*/
        }
        public int tilesPerRow;
        public int tilesPerColumn;
        public float tileWidth;
        public float tileHeight; 
        public float tileSeparation;

        private void glDrawTileset(MeshEdit solid, float tileOutline, int selectedTile)
        {
            createLineMaterial();

            createTextureMaterial();

            createAlphaMaterial();

            float fullTileWidth = tileWidth + tileOutline * 2;
            float fullTileHeight = tileHeight + tileOutline * 2;

            // float tilesPerRow = Mathf.Floor(pageWidth / fullTileWidth);
            // float tilesPerColumn = Mathf.Floor(pageHeight / fullTileHeight);


            float tileUnitX = tileWidth / texture.width;
            float tileUnitY = tileHeight / texture.height;

            float outlineUnitX = tileSeparation / texture.width;
            float outlineUnitY = tileSeparation / texture.height;

            float separationUnitX = (tileOutline / areaWidth) / zoom;
            float separationUnitY = (tileOutline / areaHeight) / zoom;

            float tileDrawUnitX = 1.0f / tilesPerRow - separationUnitX * 2;
            float tileDrawUnitY = 1.0f / tilesPerColumn - separationUnitY * 2;

            float txx = 0;
            float tyy = 0;
            
            if (solid.selectedTileset >= 0)
            {
                GL.Begin(GL.QUADS);

                textureMat.SetTexture("_MainTex", texture);

                textureMat.SetPass(0);

                for (float ty = 0; ty < tilesPerColumn; ty += 1)
                {
                    float tyUV = outlineUnitY + (tileUnitY + outlineUnitY * 2) * ty;

                    tyy += separationUnitY;
                    txx = 0;
                    for (float tx = 0; tx < tilesPerRow; tx += 1)
                    {
                        float txUV = outlineUnitX + (tileUnitX + outlineUnitX * 2) * tx;
                        txx += separationUnitX;

                        GL.TexCoord(new Vector3(txUV, 1.0f - (tyUV), 0.0f));
                        GL.Vertex3(txx, tyy, 0.0f);
                        GL.TexCoord(new Vector3(txUV + tileUnitX, 1.0f - (tyUV), 0.0f));
                        GL.Vertex3(txx + tileDrawUnitX, tyy, 0.0f);
                        GL.TexCoord(new Vector3(txUV + tileUnitX, 1.0f - (tyUV + tileUnitY), 0.0f));
                        GL.Vertex3(txx + tileDrawUnitX, tyy + tileDrawUnitY, 0.0f);
                        GL.TexCoord(new Vector3(txUV, 1.0f - (tyUV + tileUnitY), 0.0f));
                        GL.Vertex3(txx, tyy + tileDrawUnitY, 0.0f);

                        txx += tileDrawUnitX + separationUnitX;

                    }

                    tyy += tileDrawUnitY;
                    tyy += separationUnitY;

                }

                GL.End();

                if (selectedTile >= 0)
                {
                    
                    GL.Begin(GL.QUADS);

                    lineMat.SetPass(0);


                    float tx = selectedTile % tilesPerRow;
                    float ty = Mathf.Floor(selectedTile / tilesPerRow);



                    float left = tx * (tileDrawUnitX + separationUnitX * 2);
                    float right = left + tileDrawUnitX + separationUnitX * 2;
                    float top = ty * (tileDrawUnitY + separationUnitY * 2);
                    float bottom = top + tileDrawUnitY + separationUnitY * 2;

                    glDrawFrame(left, right, top, bottom);
                    GL.End();
                }
            }
            else
            {
                if (tilesPerRow > 0)
                {
                    GL.Begin(GL.QUADS);

                    textureMat.SetTexture("_MainTex", texture);

                    textureMat.SetPass(0);

                    GL.TexCoord(new Vector3(0.0f, 1.0f, 0.0f));
                    GL.Vertex3(0.0f, 0.0f, 0.0f);
                    GL.TexCoord(new Vector3(1.0f, 1.0f, 0.0f));
                    GL.Vertex3(1.0f, 0.0f, 0.0f);
                    GL.TexCoord(new Vector3(1.0f, 0.0f, 0.0f));
                    GL.Vertex3(1.0f, 1.0f, 0.0f);
                    GL.TexCoord(new Vector3(0.0f, 0.0f, 0.0f));
                    GL.Vertex3(0.0f, 1.0f, 0.0f);

                    GL.End();

                    Color gray = new Color(0.2f, 0.2f, 0.2f);

                    GL.Begin(GL.QUADS);

                    alphaMaterial.SetPass(0);
                    GL.Color(gray);

                    for (float ty = 0; ty < tilesPerColumn; ty += 1)
                    {
                        float yMinOuter = ty * (tileUnitY + outlineUnitY * 2);
                        float yMinInner = yMinOuter + outlineUnitY;
                        float yMaxOuter = (ty + 1) * (tileUnitY + outlineUnitY * 2);
                        float yMaxInner = yMaxOuter - outlineUnitY;

                        for (float tx = 0; tx < tilesPerRow; tx += 1)
                        {
                            float xMinOuter = tx * (tileUnitX + outlineUnitX * 2);
                            float xMinInner = xMinOuter + outlineUnitX;
                            float xMaxOuter = (tx + 1) * (tileUnitX + outlineUnitX * 2);
                            float xMaxInner = xMaxOuter - outlineUnitX;


                            // Left
                            GL.Vertex3(xMinOuter, yMinOuter, 0.0f);
                            GL.Vertex3(xMinInner, yMinInner, 0.0f);
                            GL.Vertex3(xMinInner, yMaxInner, 0.0f);
                            GL.Vertex3(xMinOuter, yMaxOuter, 0.0f);

                            // Right
                            GL.Vertex3(xMaxOuter, yMinOuter, 0.0f);
                            GL.Vertex3(xMaxInner, yMinInner, 0.0f);
                            GL.Vertex3(xMaxInner, yMaxInner, 0.0f);
                            GL.Vertex3(xMaxOuter, yMaxOuter, 0.0f);



                            // Top
                            GL.Vertex3(xMinOuter, yMinOuter, 0.0f);
                            GL.Vertex3(xMinInner, yMinInner, 0.0f);
                            GL.Vertex3(xMaxInner, yMinInner, 0.0f);
                            GL.Vertex3(xMaxOuter, yMinOuter, 0.0f);
                            // Bottom
                            GL.Vertex3(xMinOuter, yMaxOuter, 0.0f);
                            GL.Vertex3(xMinInner, yMaxInner, 0.0f);
                            GL.Vertex3(xMaxInner, yMaxInner, 0.0f);
                            GL.Vertex3(xMaxOuter, yMaxOuter, 0.0f);
                        }


                    }
                    // Extrude to cover the edges, Y
                    float tileBorderY = tilesPerColumn * (tileUnitY + outlineUnitY * 2);

                    GL.Vertex3(0.0f, tileBorderY, 0.0f);
                    GL.Vertex3(1.0f, tileBorderY, 0.0f);
                    GL.Vertex3(1.0f, 1.0f, 0.0f);
                    GL.Vertex3(0.0f, 1.0f, 0.0f);

                    // Extrude to cover the edge, X
                    float tileBorderX = tilesPerRow * (tileUnitX + outlineUnitX * 2);

                    GL.Vertex3(tileBorderX, 0.0f, 0.0f);
                    GL.Vertex3(tileBorderX, tileBorderY, 0.0f);
                    GL.Vertex3(1.0f, tileBorderY, 0.0f);
                    GL.Vertex3(1.0f, 0.0f, 0.0f);

                    GL.End();


                    // The tile outlines
                    for (float ty = 0; ty < tilesPerColumn; ty += 1)
                    {
                        for (float tx = 0; tx < tilesPerRow; tx += 1)
                        {
                            float x = tx * (tileUnitX + outlineUnitX * 2) + outlineUnitX;
                            float y = ty * (tileUnitY + outlineUnitY * 2) + outlineUnitY;
                            GL.Begin(GL.LINE_STRIP);
                            lineMat.SetPass(0);
                            GL.Color(gray);
                            GL.Vertex3(x, y, 0.0f);

                            GL.Vertex3(x + tileUnitX, y, 0.0f);

                            GL.Vertex3(x + tileUnitX, y + tileUnitY, 0.0f);

                            GL.Vertex3(x, y + tileUnitY, 0.0f);

                            GL.Vertex3(x, y, 0.0f);
                            GL.End();
                        }
                    }

                    if (selectedTile >= 0)
                    {
                        GL.Begin(GL.QUADS);

                        lineMat.SetPass(0);

                        float tix = selectedTile % tilesPerRow;
                        float tiy = Mathf.Floor(selectedTile / tilesPerRow);

                        float left = tix * (tileUnitX + outlineUnitX * 2) + outlineUnitX;
                        float right = left + tileUnitX;
                        float top = tiy * (tileUnitY + outlineUnitY * 2) + outlineUnitY;
                        float bottom = top + tileUnitY;

                        glDrawFrame(left, right, top, bottom);
                        GL.End();
                    }
                }
            }
        }

        private void glDrawFrame(float left, float right, float top, float bottom)
        {
            float pixelX = (1.0f / areaWidth) / zoom;
            float pixelY = (1.0f / areaHeight) / zoom;

            float pixelX2 = pixelX * 1;
            float pixelY2 = pixelY * 1;
            float pixelX3 = pixelX * 2;
            float pixelY3 = pixelY * 2;

            GL.Color(Color.black);
            GL.Vertex3(left - pixelX3, top - pixelY3, 0.0f);

            GL.Vertex3(left + pixelX3, top + pixelY3, 0.0f);

            GL.Vertex3(right - pixelX3, top + pixelY3, 0.0f);

            GL.Vertex3(right + pixelX3, top - pixelY3, 0.0f);
            GL.Color(Color.white);
            GL.Vertex3(left - pixelX2, top - pixelY2, 0.0f);

            GL.Vertex3(left + pixelX2, top + pixelY2, 0.0f);

            GL.Vertex3(right - pixelX2, top + pixelY2, 0.0f);

            GL.Vertex3(right + pixelX2, top - pixelY2, 0.0f);




            GL.Color(Color.black);
            GL.Vertex3(left - pixelX3, bottom + pixelY3, 0.0f);

            GL.Vertex3(left + pixelX3, bottom - pixelY3, 0.0f);

            GL.Vertex3(right - pixelX3, bottom - pixelY3, 0.0f);

            GL.Vertex3(right + pixelX3, bottom + pixelY3, 0.0f);
            GL.Color(Color.white);
            GL.Vertex3(left - pixelX2, bottom + pixelY2, 0.0f);

            GL.Vertex3(left + pixelX2, bottom - pixelY2, 0.0f);

            GL.Vertex3(right - pixelX2, bottom - pixelY2, 0.0f);

            GL.Vertex3(right + pixelX2, bottom + pixelY2, 0.0f);


            GL.Color(Color.black);
            GL.Vertex3(left - pixelX3, top - pixelY3, 0.0f);

            GL.Vertex3(left + pixelX3, top + pixelY3, 0.0f);

            GL.Vertex3(left + pixelX3, bottom - pixelY3, 0.0f);

            GL.Vertex3(left - pixelX3, bottom + pixelY3, 0.0f);

            GL.Color(Color.white);
            GL.Vertex3(left - pixelX2, top - pixelY2, 0.0f);

            GL.Vertex3(left + pixelX2, top + pixelY2, 0.0f);

            GL.Vertex3(left + pixelX2, bottom - pixelY2, 0.0f);

            GL.Vertex3(left - pixelX2, bottom + pixelY2, 0.0f);


            GL.Color(Color.black);
            GL.Vertex3(right + pixelX3, top - pixelY3, 0.0f);

            GL.Vertex3(right - pixelX3, top + pixelY3, 0.0f);

            GL.Vertex3(right - pixelX3, bottom - pixelY3, 0.0f);

            GL.Vertex3(right + pixelX3, bottom + pixelY3, 0.0f);

            GL.Color(Color.white);
            GL.Vertex3(right + pixelX2, top - pixelY2, 0.0f);

            GL.Vertex3(right - pixelX2, top + pixelY2, 0.0f);

            GL.Vertex3(right - pixelX2, bottom - pixelY2, 0.0f);

            GL.Vertex3(right + pixelX2, bottom + pixelY2, 0.0f);

        }

        private void updateTilesetEvents(MeshEdit solid)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (solid.selectedTileset >= 0)
                {
                    for (int i = 0; i < solid.editorInfo.tileCount; i++)
                    {
                        Vector3 topLeft = new Vector3(
                            (i % tilesPerRow) / (float)tilesPerRow,
                            (i / tilesPerRow) / (float)tilesPerColumn,
                            0.0f);
                        Vector3 widthHeight = new Vector3(
                            1.0f / tilesPerRow,
                            1.0f / tilesPerColumn,
                            0.0f);

                        topLeft = viewMatrix.MultiplyPoint3x4(topLeft);
                        widthHeight.x *= areaWidth * zoom;
                        widthHeight.y *= areaHeight * zoom;


                        Rect tileRectangle = new Rect(topLeft, widthHeight);

                        if (tileRectangle.Contains(windowMousePosition - new Vector2(viewportRectangle.xMin, viewportRectangle.yMin)))
                        {
                            solid.editorInfo.selectedTile = i;
                            break;
                        }
                    }
                }
                else
                {
                    // Just calculate it based on the mouse position, since it's a grid and not a limited number of tiles
                    Vector2 miniMousePos = Matrix4x4.Inverse(viewMatrix).MultiplyPoint3x4((windowMousePosition - new Vector2(viewportRectangle.xMin, viewportRectangle.yMin)));
                    float widthRatio = solid.selectedTexture.width / (float)(solid.editorInfo.tilesPerRow * (solid.editorInfo.tileWidth + solid.editorInfo.tileOutline * 2));
                    miniMousePos.x *= widthRatio;
                    float heightRatio = solid.selectedTexture.height / (float)(solid.editorInfo.tilesPerColumn * (solid.editorInfo.tileHeight + solid.editorInfo.tileOutline * 2));
                    miniMousePos.y *= heightRatio;
                    if (miniMousePos.x >= 0 && miniMousePos.x <= 1 &&
                        miniMousePos.y >= 0 && miniMousePos.y <= 1)
                    {
                       
                        int tx = (int)(miniMousePos.x * solid.editorInfo.tilesPerRow);
                        int ty = (int)(miniMousePos.y * solid.editorInfo.tilesPerColumn);
                        
                        solid.editorInfo.selectedTile = ty * solid.editorInfo.tilesPerRow + tx;
                    }
                }
            }
        }

        public Material lineMat;

        public void createLineMaterial()
        {
            if (lineMat == null)
            {
                Shader shader = Shader.Find("MeshEdit/GUI2");
                lineMat = new Material(shader);
                lineMat.hideFlags = HideFlags.HideAndDontSave;
            }
        }
        public Material alphaMaterial;

        public void createAlphaMaterial()
        {
            if (alphaMaterial == null)
            {
                Shader shader = Shader.Find("MeshEdit/GUI2");
                alphaMaterial = new Material(shader);
                alphaMaterial.hideFlags = HideFlags.HideAndDontSave;
                alphaMaterial.SetFloat("_Alpha", 0.8f);
            }
        }
    }

    public class GLTileUVInterface
    {
        Rect viewportRectangle;
        Vector2 viewPosition;
        Vector2 windowMousePosition;

        Matrix4x4 viewMatrix;
        public float zoom = 1;

        public int areaWidth = 1000;
        public int areaHeight = 1000;
        public float gridUnitW = 100;
        public float gridUnitH = 100;

        private bool canDrag;

        public bool constrainView = true;

        public List<Vector2> uvCoord = new List<Vector2>(new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f) });
        int uvCoordSelected = -1;
        int uvCoordEdgeSelected = -1;
        bool uvCoordBodySelected = false;

        Vector2 uvRectLastMouseClickPosition;
        float uvEdgeSelectionDistance = 12.0f;


        public Texture texture;
        public Rect tile;
        public Material textureMat;
        public void createTextureMaterial()
        {
            if (textureMat == null)
            {
                Shader shader = Shader.Find("MeshEdit/GUI2");
                textureMat = new Material(shader);
                textureMat.hideFlags = HideFlags.HideAndDontSave;

                if (texture != null)
                {
                    textureMat.SetTexture("_MainTex", texture);
                }
            }
        }

        public void updateEvents(MeshEdit solid, bool snapCoords)
        {
            bool shift = Event.current.shift;
            bool ctrl = Event.current.control || Event.current.command;
            bool alt = Event.current.alt;

            windowMousePosition = Event.current.mousePosition;
            Vector2 localMousePosition = windowMousePosition - new Vector2(viewportRectangle.xMin, viewportRectangle.yMin);

            if (Event.current.type == EventType.MouseUp)
            {
                canDrag = false;
            }

            
            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint &&
                !viewportRectangle.Contains(windowMousePosition) && !(Event.current.type == EventType.MouseDrag && canDrag))
            {
                canDrag = false;
            }

            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 2 &&
                viewportRectangle.Contains(windowMousePosition))
            {
                canDrag = true;
            }

            if (Event.current.type == EventType.ScrollWheel)
            {
                if (viewportRectangle.Contains(windowMousePosition))
                {
                    if (Event.current.delta.y > 0)
                    {
                        if (zoom > 0.1f)
                        {
                            viewPosition -= localMousePosition;
                            zoom /= 1.25f;
                            viewPosition /= 1.25f;
                            viewPosition += localMousePosition;

                            if (constrainView)
                            {
                                constrainViewposition();
                            }
                        }
                        Event.current.Use();
                    }
                    else if (Event.current.delta.y < 0)
                    {
                        if (zoom < 100.0f)
                        {
                            viewPosition -= localMousePosition;
                            zoom *= 1.25f;
                            viewPosition *= 1.25f;
                            viewPosition += localMousePosition;

                            if (constrainView)
                            {
                                constrainViewposition();
                            }
                        }
                        Event.current.Use();
                    }
                }
            }
            else if (Event.current.type == EventType.MouseDrag && canDrag)
            {
                if (Event.current.button == 2)
                {
                    viewPosition += Event.current.delta;

                    if (constrainView)
                    {
                        constrainViewposition();
                    }
                }
            }



            // Coord events

            if (Event.current.type == EventType.MouseMove)
            {
                uvCoordBodySelected = false;
                uvCoordEdgeSelected = -1;
                uvCoordSelected = -1;
            }

            if (solid.editorInfo.selectedTile >= 0 && solid.editorInfo.selectedTile < solid.editorInfo.tileCount)
            {
                //skin.box.normal.background = tiles[selectedTile % tilesPerRow, selectedTile / tilesPerRow];
                
                Rect activeRect = viewportRectangle;
                //if (activeRect.Contains(Event.current.mousePosition))
                {
                    if (uvCoordEdgeSelected >= 0 || uvCoordSelected >= 0 || uvCoordBodySelected)
                    {
                        Vector2 difference = (localMousePosition - uvRectLastMouseClickPosition);

                        // Move
                        difference.x = difference.x / areaWidth / zoom;
                        difference.y = difference.y / areaHeight / zoom;

                        if (uvCoordSelected >= 0)
                        {
                            uvCoord[uvCoordSelected] += difference;
                        }
                        else if (uvCoordEdgeSelected >= 0)
                        {
                            uvCoord[uvCoordEdgeSelected] += difference ;
                            uvCoord[(uvCoordEdgeSelected + 1) % uvCoord.Count] += difference ;
                        }
                        else if (uvCoordBodySelected)
                        {
                            for (int i = 0; i < uvCoord.Count; i++)
                            {
                                uvCoord[i] += difference;
                            }
                        }

                        snapUVCoords(solid, snapCoords);

                        uvRectLastMouseClickPosition = localMousePosition;

                    }
                    else
                    {
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                            (activeRect.Contains(Event.current.mousePosition)))
                        {
                            uvRectLastMouseClickPosition = localMousePosition;
                            
                            float sqrDist = uvEdgeSelectionDistance * uvEdgeSelectionDistance;

                            int closestUVCoord = -1;
                            float distToClosestUVCoord = float.MaxValue;
                            for (int i = 0; i < uvCoord.Count; i++)
                            {
                                Vector2 uvCoordPixelPosition = viewMatrix.MultiplyPoint3x4(uvCoord[i]);
                                   // new Vector2(uvCoord[i].x * areaWidth, uvCoord[i].y * areaHeight) + viewportRectangle.position;
                                float d = (uvCoordPixelPosition - localMousePosition).sqrMagnitude;
                                if (d <= sqrDist && d < distToClosestUVCoord)
                                {
                                    closestUVCoord = i;
                                    distToClosestUVCoord = d;
                                }
                            }
                            if (closestUVCoord != -1)
                            {
                                uvCoordBodySelected = false;
                                uvCoordEdgeSelected = -1;
                                uvCoordSelected = closestUVCoord;
                            }
                            else
                            {
                                for (int i = 0; i < uvCoord.Count; i++)
                                {
                                    Vector2 uvCoordPixelPositionA = viewMatrix.MultiplyPoint3x4(uvCoord[i]);
                                    //Vector2 uvCoordPixelPositionA = new Vector2(uvCoord[i].x * areaWidth, uvCoord[i].y * areaHeight) + viewportRectangle.position;
                                    Vector2 uvCoordPixelPositionB = viewMatrix.MultiplyPoint3x4(uvCoord[(i + 1) % uvCoord.Count]);
                                    //Vector2 uvCoordPixelPositionB = new Vector2(uvCoord[(i + 1) % uvCoord.Count].x * areaWidth, uvCoord[(i + 1) % uvCoord.Count].y * areaHeight) + viewportRectangle.position;
                                    float d = (closestPoint(uvCoordPixelPositionA, uvCoordPixelPositionB, localMousePosition) - localMousePosition).sqrMagnitude;
                                    if (d <= sqrDist && d < distToClosestUVCoord)
                                    {
                                        closestUVCoord = i;
                                        distToClosestUVCoord = d;
                                    }
                                }
                                if (closestUVCoord != -1)
                                {
                                    uvCoordBodySelected = false;
                                    uvCoordEdgeSelected = closestUVCoord;
                                    uvCoordSelected = -1;
                                }
                                else
                                {
                                    uvCoordBodySelected = true;
                                }
                            }

                            // Push snapped verts into main vert list

                            for (int i = 0; i < uvCoord.Count; i++)
                            {
                                uvCoord[i] = solid.editorInfo.uvCoordSnapped[i];
                            }
                        }
                    }
                }

                if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseLeaveWindow)
                {
                    uvCoordBodySelected = false;
                    uvCoordEdgeSelected = -1;
                    uvCoordSelected = -1;
                }


            }
            constrainViewposition();
        }

        public void rotateUVCoordsCCW()
        {
            Vector2 origin = new Vector2(0.5f, 0.5f);
            Vector2 tempVector = rotate(uvCoord[0], -90, origin);
            uvCoord[0] = rotate(uvCoord[1], -90, origin);
            uvCoord[1] = rotate(uvCoord[2], -90, origin);
            uvCoord[2] = rotate(uvCoord[3], -90, origin);
            uvCoord[3] = tempVector;
        }

        public void rotateUVCoordsCW()
        {
            Vector2 origin = new Vector2(0.5f, 0.5f);

            Vector2 tempVector = rotate(uvCoord[3], 90, origin);
            uvCoord[3] = rotate(uvCoord[2], 90, origin);
            uvCoord[2] = rotate(uvCoord[1], 90, origin);
            uvCoord[1] = rotate(uvCoord[0], 90, origin);
            uvCoord[0] = tempVector;
        }

        public void snapUVCoords(MeshEdit meshEdit, bool snapUVs)
        {

            // Constrain & snap
            float pixelUnitWidth = 1.0f / meshEdit.editorInfo.tileWidth;
            float pixelUnitHeight = 1.0f / meshEdit.editorInfo.tileHeight;

            if (meshEdit.selectedTileset >= 0)
            {
                pixelUnitWidth = 1.0f / tilesetsAvailable[meshEdit.selectedTileset].tileWidth;
                pixelUnitHeight = 1.0f / tilesetsAvailable[meshEdit.selectedTileset].tileHeight;
            }

            for (int i = 0; i < uvCoord.Count; i++)
            {
                uvCoord[i] = new Vector2(
                    Mathf.Clamp(uvCoord[i].x, 0.0f, 1.0f),
                    Mathf.Clamp(uvCoord[i].y, 0.0f, 1.0f));

                Vector2 snappedCoord = uvCoord[i];

                if (snapUVs)
                {
                    snappedCoord = new Vector2(
                        Mathf.Round(uvCoord[i].x / pixelUnitWidth) * pixelUnitWidth,
                        Mathf.Round(uvCoord[i].y / pixelUnitHeight) * pixelUnitHeight);

                }
                meshEdit.editorInfo.uvCoordSnapped[i] = snappedCoord;
            }
        }

        public void readUVsOfSelectedQuads(MeshEdit solid, int selectedQuad, bool snapUVs)
        {
            // This function doesn't take into account quads that aren't planar, and won't deform the uv to accurately map the space a saddled quad will occupy
            // This isn't the hardest thing to fix but I'm lazy :)

            Vector2 origin = new Vector2(0.5f, 0.5f);
            
            if (Event.current.modifiers == EventModifiers.Command || Event.current.modifiers == EventModifiers.Control)
            {
                Vector3 up = -Vector3.up;
                Vector3 planeNormal = solid.faceNormals[selectedQuad];

                if (Vector3.Cross(up, planeNormal).sqrMagnitude < 0.000001f)
                {

                    up = SceneView.lastActiveSceneView.camera.transform.up; //

                    up.y = 0;
                    up = up.normalized;
                    up = roundNormalToAxis(up);

                    if (Vector3.Cross(up, planeNormal).sqrMagnitude < 0.000001f)
                    {
                        new Vector3(0.0f, 0.0f, 1.0f);
                    }
                }
                uvCoord[0] = easyProjection(solid.verts[solid.quads[selectedQuad * 4 + 0]], planeNormal, solid.quadCenter(selectedQuad), up) + origin;
                uvCoord[1] = easyProjection(solid.verts[solid.quads[selectedQuad * 4 + 1]], planeNormal, solid.quadCenter(selectedQuad), up) + origin;
                uvCoord[2] = easyProjection(solid.verts[solid.quads[selectedQuad * 4 + 3]], planeNormal, solid.quadCenter(selectedQuad), up) + origin;
                uvCoord[3] = easyProjection(solid.verts[solid.quads[selectedQuad * 4 + 2]], planeNormal, solid.quadCenter(selectedQuad), up) + origin;

            }
            else if (solid.selectedMaterial >= 0)
            {
                if ( solid.materialUVMap[solid.selectedMaterial] >= 0)
                {
                    
                        uvCoord[0] = solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvs[solid.quads[selectedQuad * 4 + 0]];
                        uvCoord[1] = solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvs[solid.quads[selectedQuad * 4 + 1]];
                        uvCoord[2] = solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvs[solid.quads[selectedQuad * 4 + 3]];
                        uvCoord[3] = solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].uvs[solid.quads[selectedQuad * 4 + 2]];
                        float w = solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].texWidth;
                        float h = solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].texHeight;
                        float tw = solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].tileWidth;
                        float th = solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].tileHeight;
                        float outline = solid.uvMaps[solid.materialUVMap[solid.selectedMaterial]].tileOutline;

                        for (int i = 0; i < uvCoord.Count; i++)
                        {
                            float x = uvCoord[i].x * w;
                            float y = uvCoord[i].y * h;

                            x = x % (tw + outline * 2) - outline;
                            y = y % (th + outline * 2) - outline;
                            uvCoord[i] = new Vector2(
                                x / tw,
                                1.0f - y / th);
                        }
                    
                }
                else if (solid.selectedTexture != null)
                {
                    uvCoord[0] = solid.mesh.uv[solid.quads[selectedQuad * 4 + 0]];
                    uvCoord[1] = solid.mesh.uv[solid.quads[selectedQuad * 4 + 1]];
                    uvCoord[2] = solid.mesh.uv[solid.quads[selectedQuad * 4 + 3]];
                    uvCoord[3] = solid.mesh.uv[solid.quads[selectedQuad * 4 + 2]];
                    float w = solid.selectedTexture.width;
                    float h = solid.selectedTexture.height;
                    float tw = solid.editorInfo.customTilesetSettings[solid.selectedMaterial].tileWidth;
                    float th = solid.editorInfo.customTilesetSettings[solid.selectedMaterial].tileHeight;
                    float outline = solid.editorInfo.customTilesetSettings[solid.selectedMaterial].tileOutline;

                    float minX = float.MaxValue;
                    float maxX = float.MinValue;
                    float minY = float.MaxValue;
                    float maxY = float.MinValue;

                    for (int i = 0; i < uvCoord.Count; i++)
                    {
                        float x = uvCoord[i].x * w;
                        float y = uvCoord[i].y * h;

                        x = x % (tw + outline * 2) - outline;
                        y = y % (th + outline * 2) - outline;
                        uvCoord[i] = new Vector2(
                            x / tw,
                            1.0f - y / th);

                        if (uvCoord[i].x < minX)
                        {
                            minX = uvCoord[i].x;

                        }
                        if (uvCoord[i].y < minY)
                        {
                            minY = uvCoord[i].y;

                        }
                        if (uvCoord[i].x > maxX)
                        {
                            maxX = uvCoord[i].x;

                        }
                        if (uvCoord[i].y > minY)
                        {
                            maxY = uvCoord[i].y;

                        }
                    }

                    float widthOfSelection = maxX - minX;
                    float heightOfSelection = maxY - minY;
                    float longestDim = Mathf.Max(widthOfSelection, heightOfSelection);

                    if (longestDim > 1)
                    {
                        float scale = 1.0f / longestDim;

                        for (int i = 0; i < 4; i++)
                        {
                            uvCoord[i] = new Vector2(
                                uvCoord[i].x - minX,
                                uvCoord[i].y - minY);

                            uvCoord[i] *= scale;
                        }
                    }
                    else if (minX < 0 || minY < 0 || maxX > 1 || maxY > 1)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            uvCoord[i] = new Vector2(
                                uvCoord[i].x - minX,
                                uvCoord[i].y - minY);
                        }
                    }
                }
            }

            // Ensure that the uv poly is clockwise
            
            float direction = 0;
            for (int i = 0; i < uvCoord.Count; i++)
            {
                direction += (uvCoord[(i + 1) % uvCoord.Count].x - uvCoord[i].x) * (uvCoord[(i + 1) % uvCoord.Count].y + uvCoord[i].y);
            }

            if (direction > 0)
            {
                // Flip the uv-shape so that it's clockwise!
                Vector2 temp = uvCoord[3];
                uvCoord[3] = uvCoord[0];
                uvCoord[0] = temp;
                temp = uvCoord[2];
                uvCoord[2] = uvCoord[1];
                uvCoord[1] = temp;
            }

            // Find the most suitable top-left coord

            int closestCorner = -1;
            float dTL = float.MaxValue;
            for (int i = 0; i < uvCoord.Count; i++)
            {
                float dd = uvCoord[i].sqrMagnitude;
                if (dd < dTL)
                {
                    dTL = dd;
                    closestCorner = i;
                }
            }

            while (closestCorner > 0)
            {
                Vector2 temp = uvCoord[0];
                uvCoord[0] = uvCoord[1];
                uvCoord[1] = uvCoord[2];
                uvCoord[2] = uvCoord[3];
                uvCoord[3] = temp;
                //rotateUVCoordsCW();
                closestCorner--;
            }

            snapUVCoords(solid, snapUVs);
        }

        private Vector2 easyProjection(Vector3 point, Vector3 planeNormal, Vector3 planeOrigin, Vector3 up)
        {
            planeNormal = planeNormal.normalized;

            // Make up perpendicular to the normal
            up = (up - planeNormal * Vector3.Dot(planeNormal, up)).normalized;

            Vector3 side = Vector3.Cross(up, planeNormal).normalized;

            point = point - planeOrigin;
            point -= planeNormal * Vector3.Dot(planeNormal, point);

            float x = Vector3.Dot(point, side);
            float y = Vector3.Dot(point, up);

            return new Vector2(x, y);
        }

        private Vector3 roundNormalToAxis(Vector3 normal)
        {
            float max = Mathf.Max(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
            if (max == Mathf.Abs(normal.x))
            {
                return new Vector3(1.0f * Mathf.Sign(normal.x), 0.0f, 0.0f);
            }
            else if (max == Mathf.Abs(normal.y))
            {
                return new Vector3(0.0f, 1.0f * Mathf.Sign(normal.y), 0.0f);
            }
            else
            {
                return new Vector3(0.0f, 0.0f, 1.0f * Mathf.Sign(normal.z));
            }
        }

        public void constrainViewposition()
        {

            float pixelMargin = 32.0f;
            if (viewPosition.x > pixelMargin)
            {
                viewPosition.x = pixelMargin;
            }
            if (viewPosition.y > pixelMargin)
            {
                viewPosition.y = pixelMargin;
            }

            float xLimit = -areaWidth * zoom + viewportRectangle.width - pixelMargin;

            if (viewPosition.x < xLimit)
            {
                viewPosition.x = xLimit;
            }
            float yLimit = -areaHeight * zoom + viewportRectangle.height - pixelMargin;

            if (viewPosition.y < yLimit)
            {
                viewPosition.y = yLimit;
            }

            if (areaWidth * zoom  + pixelMargin  * 2< viewportRectangle.width)
            {
                viewPosition.x = (viewportRectangle.width - areaWidth * zoom) / 2;
            }
            if (areaHeight * zoom + pixelMargin  * 2 < viewportRectangle.height)
            {
                viewPosition.y = (viewportRectangle.height - areaHeight * zoom) / 2;
            }
        }

        public void setViewportSize(Rect rect)
        {
            createLineMaterial();

            if (rect != viewportRectangle)
            {
                viewportRectangle = rect;

            }

            if (lineMat != null)
            {
                lineMat.SetInt("_ClipLeft", 0);
                lineMat.SetInt("_ClipTop", 0);
                lineMat.SetInt("_ClipRight", (int)viewportRectangle.xMax);
                lineMat.SetInt("_ClipBottom", (int)viewportRectangle.yMax);
            }
            if (textureMat != null)
            {

                textureMat.SetInt("_ClipLeft", 0);
                textureMat.SetInt("_ClipTop", 0);
                textureMat.SetInt("_ClipRight", (int)viewportRectangle.xMax);
                textureMat.SetInt("_ClipBottom", (int)viewportRectangle.yMax);
            }
        }

        public void updateViewMatrix()
        {
            viewMatrix = Matrix4x4.TRS(new Vector3(viewPosition.x, viewPosition.y, 0.0f), Quaternion.identity, new Vector3((areaWidth) * zoom, (areaHeight) * zoom, 1));
        }

        public void glBeginInterface()
        {

            GL.PushMatrix();
            GL.MultMatrix(viewMatrix);
        }

        public void glEndInterface()
        {
            GL.PopMatrix();
        }
        
        public void glDrawInterface(MeshEdit solid, int selectedTile)
        {
            if (lineMat == null)
            {
                createLineMaterial();

            }
            float wRatio = (viewportRectangle.width / areaWidth) / zoom;
            float hRatio = (viewportRectangle.height / areaHeight) / zoom;

            float offsetXRatio = (viewPosition.x / viewportRectangle.width) * wRatio;
            float offsetYRatio = (viewPosition.y / viewportRectangle.height) * hRatio;

            glDrawGrid(wRatio, hRatio, 1, offsetXRatio, offsetYRatio);

            glDrawTileset(solid, selectedTile);
        }

        private void glDrawGrid(float w, float h, float zoom, float offsetX, float offsetY)
        {
            float left = 0 - offsetX;
            float right = left + w;
            float bottom = 0 - offsetY;
            float top = bottom + h;

            lineMat.SetPass(0);

            GL.Begin(GL.QUADS);
            GL.Color(new Color(0.125f, 0.125f, 0.125f, 1.0f));
            GL.Vertex3(left, top, 0.0f);
            GL.Vertex3(left, bottom, 0.0f);
            GL.Vertex3(right, bottom, 0.0f);
            GL.Vertex3(right, top, 0.0f);
            GL.End();

            left = 0;
            right = 1.0f;
            top = 0;
            bottom = 1.0f;

            GL.Begin(GL.QUADS);
            float g = 80.0f / 256.0f;
            GL.Color(new Color(g, g, g, 1));

            GL.Vertex3(right, bottom, 0);
            GL.Vertex3(left, bottom, 0);
            GL.Vertex3(left, top, 0);
            GL.Vertex3(right, top, 0);

            GL.End();

            float startY = Mathf.Floor(bottom / gridUnitH) * gridUnitH;

            float startX = Mathf.Floor(left / gridUnitW) * gridUnitW;
        }

        private void glDrawTileset(MeshEdit solid, int selectedTile)
        {
            createLineMaterial();

            createTextureMaterial();

            // float tilesPerRow = Mathf.Floor(pageWidth / fullTileWidth);
            // float tilesPerColumn = Mathf.Floor(pageHeight / fullTileHeight);

            GL.Begin(GL.QUADS);

            textureMat.SetTexture("_MainTex", texture);

            textureMat.SetPass(0);

            GL.TexCoord(new Vector3(tile.xMin, 1.0f - tile.yMin, 0.0f));
            GL.Vertex3(0, 0, 0.0f);
            GL.TexCoord(new Vector3(tile.xMax, 1.0f - tile.yMin, 0.0f));
            GL.Vertex3(1, 0, 0.0f);
            GL.TexCoord(new Vector3(tile.xMax, 1.0f - tile.yMax, 0.0f));
            GL.Vertex3(1, 1, 0.0f);
            GL.TexCoord(new Vector3(tile.xMin, 1.0f - tile.yMax, 0.0f));
            GL.Vertex3(0, 1, 0.0f);


            GL.End();

            float pixelX = 1.0f / areaWidth / zoom;
            float pixelY = 1.0f / areaHeight / zoom;


            drawUVLines(solid, pixelX, 0, Color.black);
            drawUVLines(solid, -pixelX, 0, Color.black);
            drawUVLines(solid, 0, pixelY, Color.black);
            drawUVLines(solid, 0, -pixelY, Color.black);
            drawUVLines(solid, 0, 0, Color.white);
        }

        private void drawUVLines(MeshEdit solid, float offsetX, float offsetY, Color colour)
        {
            float pixelX = 1.0f / areaWidth / zoom;
            float pixelY = 1.0f / areaHeight / zoom;

            
            GL.Begin(GL.LINE_STRIP);

            lineMat.SetPass(0);
            GL.Color(colour);
            Vector2 v = solid.editorInfo.uvCoordSnapped[0];
            GL.Vertex3(v.x + offsetX, v.y + offsetY, 0.0f);
            v = solid.editorInfo.uvCoordSnapped[1];
            GL.Vertex3(v.x + offsetX, v.y + offsetY, 0.0f);
            v = solid.editorInfo.uvCoordSnapped[2];
            GL.Vertex3(v.x + offsetX, v.y + offsetY, 0.0f);
            v = solid.editorInfo.uvCoordSnapped[3];
            GL.Vertex3(v.x + offsetX, v.y + offsetY, 0.0f);
            v = solid.editorInfo.uvCoordSnapped[0];
            GL.Vertex3(v.x + offsetX, v.y + offsetY, 0.0f);
            GL.End();
            GL.Begin(GL.QUADS);
            lineMat.SetPass(0);
            GL.Color(colour);
            for (int i = 0; i < 4; i++)
            {
                v = solid.editorInfo.uvCoordSnapped[i];
                GL.Vertex3(v.x + pixelX * 2 + offsetX, v.y + pixelY * 2 + offsetY, 0.0f);
                GL.Vertex3(v.x + pixelX * 2 + offsetX, v.y - pixelY * 2 + offsetY, 0.0f);
                GL.Vertex3(v.x - pixelX * 2 + offsetX, v.y - pixelY * 2 + offsetY, 0.0f);
                GL.Vertex3(v.x - pixelX * 2 + offsetX, v.y + pixelY * 2 + offsetY, 0.0f);
            }
            GL.End();
        }


        public Material lineMat;

        public void createLineMaterial()
        {
            if (lineMat == null)
            {
                Shader shader = Shader.Find("MeshEdit/GUI2");
                lineMat = new Material(shader);
                lineMat.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }


    private class GLLogoDemo
    {
        Rect viewportRectangle;

        Vector2 windowMousePosition;
        Vector2 viewPosition = Vector2.zero;
        Matrix4x4 viewMatrix;
        public float zoom = 1;

        public float animationTimer = 0;

        public void updateEvents()
        {
            init();

            float dT = (Time.realtimeSinceStartup - startTime) * 1.2f;
            startTime = Time.realtimeSinceStartup;

            if (animationTimer < 10.0f)
            {
                animationTimer += dT;

                for (int i = 0; i < letters.Length; i++)
                {
                    letters[i].update(dT);
                }
            }
        }

        public void setViewportSize(Rect rect)
        {
            createLineMaterial();

            createAlphaMaterial();

            if (rect != viewportRectangle)
            {
                viewportRectangle = rect;

            }

            if (lineMat != null)
            {
                lineMat.SetInt("_ClipLeft", 0);
                lineMat.SetInt("_ClipTop", 0);
                lineMat.SetInt("_ClipRight", (int)viewportRectangle.width);
                lineMat.SetInt("_ClipBottom", (int)viewportRectangle.height);
            }
            if (alphaMaterial != null)
            {

                alphaMaterial.SetInt("_ClipLeft", 0);
                alphaMaterial.SetInt("_ClipTop", 0);
                alphaMaterial.SetInt("_ClipRight", (int)viewportRectangle.width);
                alphaMaterial.SetInt("_ClipBottom", (int)viewportRectangle.height);
            }
        }

        float spin = 0;
        private float smootherStep(float x)
        {
            return Mathf.Clamp(x * x * x * (x * (x * 6 - 15) + 10), 0.0f, 1.0f);
        }
        public void glDrawInterface()
        {
            init();

            GL.PushMatrix();
            viewMatrix = Matrix4x4.TRS(new Vector3(viewPosition.x, viewPosition.y, 0.0f), Quaternion.identity, new Vector3(viewportRectangle.width, viewportRectangle.height, 0.001f));
            GL.MultMatrix(viewMatrix);


            lineMat.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.Color(new Color(0.125f, 0.125f, 0.125f, 1.0f));
            GL.Color(Color.Lerp(
                new Color(0.125f, 0.125f, 0.125f, 1.0f),
                Color.white,
                Mathf.Clamp(animationTimer - 5.1f, 0.0f, 1.0f)));
            //GL.Color(Color.white);
            GL.Vertex3(0, 0, 0.0f);
            GL.Vertex3(0, 1, 0.0f);
            GL.Vertex3(1, 1, 0.0f);
            GL.Vertex3(1, 0, 0.0f);
            GL.End();


            float wValue = 120;
            float hValue = 30;
            float idealRatio = hValue / wValue;
            float rectRatio = viewportRectangle.height / viewportRectangle.width;

            Vector3 offset = Vector3.zero;
            Vector3 scale = Vector3.one;

            scale.x = 1.0f / wValue;
            scale.y = 1.0f / hValue;
            scale.z = 1.0f / hValue;

            if (rectRatio > idealRatio)
            {
                // Rectangle taller than ideal
                float innerBoxHeight = (idealRatio * viewportRectangle.width);
                offset.y = ((viewportRectangle.height - innerBoxHeight) / viewportRectangle.height) / 2;

                scale.x *= 1.0f;
                scale.y *= idealRatio / rectRatio;
            }
            else if (rectRatio < idealRatio)
            {
                // Rectangle narrower than ideal
                float innerBoxWidth = viewportRectangle.height / idealRatio;

                offset.x = ((viewportRectangle.width - innerBoxWidth) / viewportRectangle.width) / 2;
                //offset.x = xRatio;
                scale.x *= innerBoxWidth / viewportRectangle.width;
                scale.y *= 1.0f;
            }

            GL.PushMatrix();

            Matrix4x4 innerMatrix = Matrix4x4.TRS(offset, Quaternion.identity, scale);
            GL.MultMatrix(viewMatrix * innerMatrix);

            float centerX = wValue / 2;
            Color blue = new Color(84 / 255.0f, 191 / 255.0f, 249 / 255.0f);
            Color orange = new Color(255 / 255.0f, 154 / 255.0f, 86 / 255.0f);
            Color maroon = new Color(194 / 255.0f, 153 / 255.0f, 159 / 255.0f);
            float gradientLength = 6;
            float margin = 4;
            float separatorXPos = centerX + 16.5f;

            float wireBoxWidth = (wValue - margin * 2);
            float timedWidth = smootherStep( Mathf.Clamp(animationTimer - 1f, 0.0f, 2f) / 2f);

            separatorXPos = separatorXPos * timedWidth;

            float slide = 640.0f * (1.0f - smootherStep( Mathf.Clamp(animationTimer, 0.0f, 3.0f) / 3.0f));

            separatorXPos += slide;

            float lineWeight = 1.2f;


            float orangeT = Mathf.Clamp(animationTimer - 6.05f, 0.0f, 1.5f) / 1.5f;

            if (orangeT > 0)
            {
                orangeT = (smootherStep(orangeT / 2.0f + 0.5f) - 0.5f) * 2.0f;
            }

            float wireBoxMaxX = separatorXPos + orangeT * ((wValue - margin) - separatorXPos);

            maroon = Color.Lerp(blue, maroon, Mathf.Clamp(2 * orangeT, 0.0f, 1.0f));
            orange = Color.Lerp(blue, orange, Mathf.Clamp(2 * orangeT, 0.0f, 1.0f));

            glDrawLine(new Vector2(separatorXPos, margin), new Vector2(separatorXPos, hValue - margin), 0.0f, lineWeight, maroon, 1.0f);
            glDrawLine(new Vector2(margin + slide, margin), new Vector2(separatorXPos - gradientLength, margin), 0.0f, lineWeight, blue, 1.0f);
            glDrawLine(new Vector2(Mathf.Clamp(separatorXPos + gradientLength, 0.0f, wireBoxMaxX), margin), new Vector2(wireBoxMaxX, margin), 0.0f, lineWeight, orange, orange, 1.0f);
            glDrawLine(new Vector2(separatorXPos - gradientLength, margin), new Vector2(separatorXPos, margin), 0.0f, lineWeight, blue, maroon, 1.0f);
            glDrawLine(new Vector2(Mathf.Clamp(separatorXPos + gradientLength, 0.0f, wireBoxMaxX), margin), new Vector2(separatorXPos, margin), 0.0f, lineWeight, orange, maroon, 1.0f);

            glDrawLine(new Vector2(margin + slide, hValue - margin), new Vector2(separatorXPos - gradientLength, hValue - margin), 0.0f, lineWeight, blue, 1.0f);
            glDrawLine(new Vector2(Mathf.Clamp(separatorXPos + gradientLength, 0.0f, wireBoxMaxX), hValue - margin), new Vector2(wireBoxMaxX, hValue - margin), 0.0f, lineWeight, orange, orange, 1.0f);
            glDrawLine(new Vector2(separatorXPos - gradientLength, hValue - margin), new Vector2(separatorXPos, hValue - margin), 0.0f, lineWeight, blue, maroon, 1.0f);
            glDrawLine(new Vector2(Mathf.Clamp(separatorXPos + gradientLength, 0.0f, wireBoxMaxX), hValue - margin), new Vector2(separatorXPos, hValue - margin), 0.0f, lineWeight, orange, maroon, 1.0f);

            glDrawLine(new Vector2(margin + slide, margin), new Vector2(margin + slide, hValue - margin), 0.0f, lineWeight, blue, 1.0f);
            glDrawLine(new Vector2(wireBoxMaxX, margin), new Vector2(wireBoxMaxX, hValue - margin), 0.0f, lineWeight, orange, 1.0f);
            
            GL.Begin(GL.LINES);

            // M
            float h = hValue / 2 + 0.6f;
            float letterScale = 1.375f;
            float startPos = centerX - 42;
            float spacing = 2.0f;
            float depth = 3.0f;
            float z = 5.0f;
            float z2 = 10.0f;
            spin = 0;
            xOffset = 0;
            xOffset += drawGLLetter(letters[0], new Vector3(startPos + xOffset, h, z), spin, letters[0].getScale(), depth);
            xOffset += drawGLLetter(letters[1], new Vector3(startPos + xOffset, h, z), spin, letterScale, depth) + spacing;
            xOffset += drawGLLetter(letters[2], new Vector3(startPos + xOffset, h, z), spin, letterScale, depth) + spacing;
            xOffset += drawGLLetter(letters[3], new Vector3(startPos + xOffset, h, z), spin, letterScale, depth) + spacing;
            xOffset += 4.0f;
            xOffset += drawGLLetter(letters[4], new Vector3(startPos + xOffset, h, z), spin, letterScale, depth) + spacing;
            drawGLLetter(letters[5], new Vector3(startPos + xOffset, h, z), spin, letterScale, depth);
            xOffset += drawGLLetter(letters[6], new Vector3(startPos + xOffset, h, z), spin, letterScale, depth) - spacing / 2.0f;
            xOffset += drawGLLetter(letters[7], new Vector3(startPos + xOffset, h, z), spin, letterScale, depth) + spacing * 2.0f;
            xOffset += drawGLLetter(letters[8], new Vector3(startPos + xOffset, h, z), spin, letterScale, depth);

            GL.End();

            xOffset = 0;
            xOffset += drawGLLetterStill(letters[0], new Vector3(startPos + xOffset, h, z2), blue, spin, letterScale);
            xOffset += drawGLLetterStill(letters[1], new Vector3(startPos + xOffset, h, z2), blue, spin, letterScale) + spacing;
            xOffset += drawGLLetterStill(letters[2], new Vector3(startPos + xOffset, h, z2), blue, spin, letterScale) + spacing;
            xOffset += drawGLLetterStill(letters[3], new Vector3(startPos + xOffset, h, z2), blue, spin, letterScale) + spacing;
            xOffset += 4.0f;
            xOffset += drawGLLetterStill(letters[4], new Vector3(startPos + xOffset, h, z2), blue, spin, letterScale) + spacing;
            drawGLLetterStill(letters[5], new Vector3(startPos + xOffset, h, z2), orange, spin, letterScale);
            xOffset += drawGLLetterStill(letters[6], new Vector3(startPos + xOffset, h, z2), orange, spin, letterScale) - spacing / 2.0f;
            xOffset += drawGLLetterStill(letters[7], new Vector3(startPos + xOffset, h, z2), orange, spin, letterScale) + spacing * 2.0f;
            xOffset += drawGLLetterStill(letters[8], new Vector3(startPos + xOffset, h, z2), orange, spin, letterScale);

            GL.PopMatrix();

            GL.PopMatrix();
        }

        public void glDrawLine(Vector2 a, Vector2 b, float z, float lineWeight, Color colour, float wRatio)
        {
            glDrawLine(a, b, z, lineWeight, colour, colour, wRatio);
        }

        public void glDrawLine(Vector2 a, Vector2 b, float z, float lineWeight, Color colourA, Color colourB, float wRatio)
        {
            GL.Begin(GL.TRIANGLE_STRIP);

            GL.Color(colourA);

            Vector2 t = (a - b).normalized * (lineWeight / 2);

            float stepSize = Mathf.PI / 10;
            float piOver2 = Mathf.PI / 2;

            GL.Vertex3(a.x + t.x, a.y + t.y, z);

            // Capsule cap a
            for (float r = stepSize; r < piOver2; r += stepSize)
            {
                float cos = Mathf.Cos(r);
                float sin = Mathf.Sin(r);
                float cos2 = Mathf.Cos(-r);
                float sin2 = Mathf.Sin(-r);

                GL.Vertex3(a.x + (cos * t.x - sin * t.y) * wRatio, a.y + sin * t.x + cos * t.y, z);
                GL.Vertex3(a.x + (cos2 * t.x - sin2 * t.y) * wRatio, a.y + sin2 * t.x + cos2 * t.y, z);
            }

            GL.Color(colourB);

            t = -t;
            // Capsule cap b
            for (float r = piOver2; r > 0; r -= stepSize)
            {
                float cos = Mathf.Cos(r);
                float sin = Mathf.Sin(r);
                float cos2 = Mathf.Cos(-r);
                float sin2 = Mathf.Sin(-r);

                GL.Vertex3(b.x + (cos2 * t.x - sin2 * t.y) * wRatio, b.y + sin2 * t.x + cos2 * t.y, z);
                GL.Vertex3(b.x + (cos * t.x - sin * t.y) * wRatio, b.y + sin * t.x + cos * t.y, z);
            }



            GL.End();
        }

        float startTime;

        public void resetTimer()
        {
            startTime = Time.realtimeSinceStartup;
        }

        float xOffset = 0.0f;
        void init()
        {
            if (letters == null || letters.Length != 9)
            {
                #region letter definitions
                Vector2[] letterM2D = new Vector2[]
        {
            new Vector2(0f, 2f),
            new Vector2(-2f, 0f),
            new Vector2(-2f, 5f),
            new Vector2(-5f, 5f),
            new Vector2(-5f, -5f),
            new Vector2(-2f, -5f),
            new Vector2(0f, -2f),
            new Vector2(2f, -5f),
            new Vector2(5f, -5f),
            new Vector2(5f, 5f),
            new Vector2(2f, 5f),
            new Vector2(2f, 0f)
        };
                Vector2[] letterE2D = new Vector2[]
               {
            new Vector2(-3.5f, 5f),
            new Vector2(-3.5f, -5f),
            new Vector2(3.5f, -5f),
            new Vector2(3.5f, -2f),
            new Vector2(-0.5f, -2f),
            new Vector2(-0.5f, -1.5f),
            new Vector2(3.5f, -1.5f),
            new Vector2(3.5f, 1.5f),
            new Vector2(-0.5f, 1.5f),
            new Vector2(-0.5f, 2f),
            new Vector2(3.5f, 2f),
            new Vector2(3.5f, 5f)
               };
                Vector2[] letterS2D = new Vector2[]
               {
            new Vector2(7.2f - 3.5f, -4.4f),
            new Vector2(5.2f - 3.5f, -4.9f),
            new Vector2(4.3f - 3.5f, -5.0f),
            new Vector2(2.9f - 3.5f, -5.0f),
            new Vector2(2.2f - 3.5f, -4.9f),
            new Vector2(1.2f - 3.5f, -4.4f),
            new Vector2(0.3f - 3.5f, -3.5f),
            new Vector2(-0.1f - 3.5f, -2.7f),
            new Vector2(-0.2f - 3.5f, -1.7f),
            new Vector2(-0.1f - 3.5f, -0.7f),
            new Vector2(0.3f - 3.5f, 0.1f),
            new Vector2(0.9f - 3.5f, 0.7f),
            new Vector2(1.7f - 3.5f, 1.1f),
            new Vector2(4.3f - 3.5f, 1.5f),
            new Vector2(4.2f - 3.5f, 1.8f),
            new Vector2(0.2f - 3.5f, 1.1f),

            new Vector2(-7.2f + 3.5f, 4.4f),
            new Vector2(-5.2f + 3.5f, 4.9f),
            new Vector2(-4.3f + 3.5f, 5.0f),
            new Vector2(-2.9f + 3.5f, 5.0f),
            new Vector2(-2.2f + 3.5f, 4.9f),
            new Vector2(-1.2f + 3.5f, 4.4f),
            new Vector2(-0.3f + 3.5f, 3.5f),
            new Vector2(0.1f + 3.5f, 2.7f),
            new Vector2(0.2f + 3.5f, 1.7f),
            new Vector2(0.1f + 3.5f, 0.7f),
            new Vector2(-0.3f + 3.5f, -0.1f),
            new Vector2(-0.9f + 3.5f, -0.7f),
            new Vector2(-1.7f + 3.5f, -1.1f),
            new Vector2(-4.3f + 3.5f, -1.5f),
            new Vector2(-4.2f + 3.5f, -1.8f),
            new Vector2(-0.2f + 3.5f, -1.1f),
               };
                Vector2[] letterOuterD2D = new Vector2[]
               {
            new Vector2(0f - 3.5f, -5.0f),
            new Vector2(3.3f - 3.5f, -5.0f),
            new Vector2(4.7f - 3.5f, -4.8f),
            new Vector2(5.9f - 3.5f, -4.1f),
            new Vector2(6.7f - 3.5f, -3.1f),
            new Vector2(7.1f - 3.5f, -1.2f),

            new Vector2(7.1f - 3.5f, 1.2f),
            new Vector2(6.7f - 3.5f, 3.1f),
            new Vector2(5.9f - 3.5f, 4.1f),
            new Vector2(4.7f - 3.5f, 4.8f),
            new Vector2(3.3f - 3.5f, 5.0f),
            new Vector2(0f - 3.5f, 5.0f),
               };
                Vector2[] letterInnerD2D = new Vector2[]
               {
            new Vector2(2.9f - 3.5f, -1.4f),
            new Vector2(3.5f - 3.5f, -1.4f),
            new Vector2(3.9f - 3.5f, -1.1f),
            new Vector2(4.0f - 3.5f, -0.4f),
            new Vector2(4.0f - 3.5f, 0.4f),
            new Vector2(3.9f - 3.5f, 1.1f),
            new Vector2(3.5f - 3.5f, 1.4f),
            new Vector2(2.9f - 3.5f, 1.4f),
               };
                Vector2[] letterH2D = new Vector2[]
               {
            new Vector2(-3.5f, 5f),
            new Vector2(-3.5f, -5f),
            new Vector2(-0.5f, -5f),
            new Vector2(-0.5f, -1.5f),
            new Vector2(0.5f, -1.5f),
            new Vector2(0.5f, -5f),
            new Vector2(3.5f, -5f),
            new Vector2(3.5f, 5f),
            new Vector2(0.5f, 5f),
            new Vector2(0.5f, 1.5f),
            new Vector2(-0.5f, 1.5f),
            new Vector2(-0.5f, 5f)
               };
                Vector2[] letterI2D = new Vector2[]
               {
            new Vector2(-1.5f, 5f),
            new Vector2(1.5f, 5f),
            new Vector2(1.5f, -5f),
            new Vector2(-1.5f, -5f)
               };
                Vector2[] letterT2D = new Vector2[]
               {
            new Vector2(-3.5f, -5f),
            new Vector2(-3.5f, -2f),
            new Vector2(-1.5f, -2f),
            new Vector2(-1.5f, 5f),
            new Vector2(1.5f, 5f),
            new Vector2(1.5f, -2f),
            new Vector2(3.5f, -2f),
            new Vector2(3.5f, -5f)
               };
                #endregion
                letters = new MeshEditLogoLetter[9];

                MeshEditLogoLetter letterM = new MeshEditLogoLetter();
                letterM.points = letterM2D;
                letterM.width = 10;
                letterM.timer = -1.0f;

                MeshEditLogoLetter letterE = new MeshEditLogoLetter();
                letterE.points = letterE2D;
                letterE.width = 7;
                letterE.timer = -1.2f;

                MeshEditLogoLetter letterS = new MeshEditLogoLetter();
                letterS.points = letterS2D;
                letterS.width = 7;
                letterS.timer = -1.4f;
                MeshEditLogoLetter letterH = new MeshEditLogoLetter();
                letterH.points = letterH2D;
                letterH.width = 7;
                letterH.timer = -1.6f;

                MeshEditLogoLetter letterE2 = new MeshEditLogoLetter();
                letterE2.points = letterE2D;
                letterE2.width = 7;
                letterE2.timer = -2.0f;
                MeshEditLogoLetter letterDI = new MeshEditLogoLetter();
                letterDI.points = letterInnerD2D;
                letterDI.width = 0;
                letterDI.timer = -2.2f;
                MeshEditLogoLetter letterDO = new MeshEditLogoLetter();
                letterDO.points = letterOuterD2D;
                letterDO.width = 7;
                letterDO.timer = -2.2f;

                MeshEditLogoLetter letterI = new MeshEditLogoLetter();
                letterI.points = letterI2D;
                letterI.width = 3;
                letterI.timer = -2.4f;
                MeshEditLogoLetter letterT = new MeshEditLogoLetter();
                letterT.points = letterT2D;
                letterT.width = 7;
                letterT.timer = -2.6f;

                letters[0] = letterM;
                letters[1] = letterE;
                letters[2] = letterS;
                letters[3] = letterH;
                letters[4] = letterE2;
                letters[5] = letterDI;
                letters[6] = letterDO;
                letters[7] = letterI;
                letters[8] = letterT;

                resetTimer();
            }
    }

    MeshEditLogoLetter[] letters;

        private class MeshEditLogoLetter
        {
            public float width;
            public  Vector2[] points;
            public float timer;

            public void update(float dt)
            {
                if (timer + dt < 10.0f)
                {
                    timer += dt;
                }
                else
                {
                    timer = 10.0f;
                }
            }

            private float originalScale = 70;
            private float targetScale = 1.375f;
            public float getScale()
            {
                float scaleTime = 2.5f;
                float t = Mathf.Clamp(timer, 0.0f, scaleTime) / scaleTime;

                t = smootherStep(t);

                return Mathf.Lerp(originalScale, targetScale, t);
            }
            private float smootherStep(float x)
            {
                return Mathf.Clamp(x * x * x * (x * (x * 6 - 15) + 10), 0.0f, 1.0f);
            }
            private float originalAngle = -360.0f;
            private float targetAngle = 360.0f;
            public float getAngle()
            {
                float spinTime = 3f;
                float t = Mathf.Clamp(timer, 0.0f, spinTime) / spinTime;

                t = smootherStep(t);

                return originalAngle + (targetAngle - originalAngle) * t;
            }
            public float getLineWeight()
            {
                float growTime = 0.5f;
                float t = (Mathf.Clamp(timer, 3.7f, 3.7f + growTime) - 3.7f) / growTime;

                return t;
            }
        }

        private float drawGLLetter(MeshEditLogoLetter letter, Vector3 position, float angle, float scale, float depth)
        {
            scale = letter.getScale();
            angle = letter.getAngle();
            float lineWeight = letter.getLineWeight();
            
            Matrix4x4 trs = Matrix4x4.TRS(position, Quaternion.AngleAxis(angle, new Vector3(0.0f, 1.0f, 0.1f).normalized), new Vector3(scale, scale, scale));
            //if (lineWeight < 0.1f)
            {
                for (int i = 0; i < letter.points.Length; i++)
                {
                    int ii = (i + 1) % letter.points.Length;

                    Vector3 a = trs.MultiplyPoint3x4((Vector3)letter.points[i] + new Vector3(0.0f, 0.0f, depth / 2));
                    Vector3 b = trs.MultiplyPoint3x4((Vector3)letter.points[ii] + new Vector3(0.0f, 0.0f, depth / 2));

                    GL.Vertex3(a.x, a.y, a.z);
                    GL.Vertex3(b.x, b.y, b.z);

                    if (depth > 0)
                    {
                        Vector3 c = trs.MultiplyPoint3x4((Vector3)letter.points[i] + new Vector3(0.0f, 0.0f, -depth / 2));
                        Vector3 d = trs.MultiplyPoint3x4((Vector3)letter.points[ii] + new Vector3(0.0f, 0.0f, -depth / 2));

                        GL.Vertex3(c.x, c.y, c.z);
                        GL.Vertex3(d.x, d.y, d.z);

                        GL.Vertex3(a.x, a.y, a.z);
                        GL.Vertex3(c.x, c.y, c.z);
                    }
                }
            }
            return letter.width * scale;
        }

        private float drawGLLetterStill(MeshEditLogoLetter letter, Vector3 position, Color colour, float angle, float scale)
        {
            scale = letter.getScale();
            angle = letter.getAngle();
            float lineWeight = letter.getLineWeight();
            Matrix4x4 trs = Matrix4x4.TRS(position, Quaternion.AngleAxis(angle, new Vector3(0.0f, 1.0f, 0.1f).normalized), new Vector3(scale, scale, scale));

            for (int i = 0; i < letter.points.Length; i++)
            {
                int ii = (i + 1) % letter.points.Length;

                Vector3 a = trs.MultiplyPoint3x4((Vector3)letter.points[i] + new Vector3(0.0f, 0.0f, 1.5f));
                Vector3 b = trs.MultiplyPoint3x4((Vector3)letter.points[ii] + new Vector3(0.0f, 0.0f, 1.5f));

                glDrawLine(a, b, a.z, lineWeight, colour, 1.0f);
            }
            return letter.width * scale;
        }
        
        public Material lineMat;

        public void createLineMaterial()
        {
            if (lineMat == null)
            {
                Shader shader = Shader.Find("MeshEdit/GUI2");
                lineMat = new Material(shader);
                lineMat.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        public Material alphaMaterial;

        public void createAlphaMaterial()
        {
            if (alphaMaterial == null)
            {
                Shader shader = Shader.Find("MeshEdit/GUI2");
                alphaMaterial = new Material(shader);
                alphaMaterial.hideFlags = HideFlags.HideAndDontSave;
                alphaMaterial.SetFloat("_Alpha", 0.8f);
            }
        }
    }

}
#endif