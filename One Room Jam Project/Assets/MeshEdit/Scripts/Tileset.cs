using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
/// <summary>
/// The tileset used by the mesh tiling interface
/// </summary>
[Serializable]
public class Tileset : System.Object
{
    [Serializable]
    private class UVAnimation : System.Object
    {
        [SerializeField, HideInInspector]
        public int startTileX;
        [SerializeField, HideInInspector]
        public int startTileY;
        [SerializeField, HideInInspector]
        public int imageSpeed;
        [SerializeField, HideInInspector]
        public int imageCount;
    }
    [SerializeField, HideInInspector]
    List<UVAnimation> uvAnimations;

    [SerializeField, HideInInspector]
    public List<Vector2> tilesetMappedPoints;
    [SerializeField, HideInInspector]
    public List<Vector2> editorMappedPoints;
    [SerializeField, HideInInspector]
    public int[] animationLength;

    [SerializeField, HideInInspector]
    public string tilesetName;

    [SerializeField, HideInInspector]
    public int tileWidth, tileHeight;

    [SerializeField, HideInInspector]
    public int tileOutline;

    [SerializeField, HideInInspector]
    public int pageWidth, pageHeight;
    [SerializeField, HideInInspector]
    public int editorTilesetWidth, editorTilesetHeight;

    [SerializeField, HideInInspector]
    public List<string> textureAssets;
    [SerializeField, HideInInspector]
    public List<string> animationAssets;
    [SerializeField, HideInInspector, XmlIgnore]
    public Texture2D texturePage;
    [SerializeField, HideInInspector, XmlIgnore]
    public Texture2D textureEditorTileset;

    [SerializeField, HideInInspector, XmlIgnore]
    public List<Texture2D> textures;
    [SerializeField, HideInInspector, XmlIgnore]
    public List<Texture2D> animations;

#if UNITY_EDITOR
    public Tileset()
    {
        initialise();
    }
    
    public void reConstructTileset(string name, List<Texture2D> textures, List<Texture2D> animations, int tileWidth, int tileHeight, int tileOutline)
    {
        this.tilesetName = name;
        initialise();

        int widestTexture = 0;

        this.tileWidth = tileWidth;
        this.tileHeight = tileHeight;
        this.tileOutline = tileOutline;

        for (int i = 0; i < textures.Count; i++)
        {
            string fullName = AssetDatabase.GetAssetPath(textures[i]);
            this.textures.Add(textures[i]);
            this.textureAssets.Add(fullName);
            
            if (textures[i].width > widestTexture)
            {
                widestTexture = textures[i].width;
            }
        }
        for (int i = 0; i < animations.Count; i++)
        {
            string fullName = AssetDatabase.GetAssetPath(animations[i]);
            this.animations.Add(animations[i]);
            this.animationAssets.Add(fullName);
        }
    }

    private void initialise()
    {
        pageWidth = 8 * 64;
        pageHeight = 8 * 64;
        tileWidth = 64;
        tileHeight = 64;
        tileOutline = 1;

        textures = new List<Texture2D>();
        animations = new List<Texture2D>();

        textureAssets = new List<string>();
        animationAssets = new List<string>();
    }

    public void loadTexturesFromAssets()
    {
        textures = new List<Texture2D>();
        animations = new List<Texture2D>();
        for (int i = 0; i < textureAssets.Count; i++)
        {
            string path = textureAssets[i];
            textures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        }
        for (int i = 0; i < animationAssets.Count; i++)
        {
            string path = animationAssets[i];
            animations.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        }
        MeshEditWindow.checkEditorFoldersExist();

        texturePage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets" + TilesetManager.locationOfTilesetPagesWindows + "/" + tilesetName + ".png");
        textureEditorTileset = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets" + TilesetManager.locationOfTilesetInfoWindows + "/" + tilesetName + ".png");
    }

    /// <summary>
    /// Adds a texture or animation to the tileset.
    /// Fails if the new texture doesn't fit the tileWidth/tileHeight 
    ///     consistency or is too wide for the texture page.
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="isAnimation"></param>
    /// <returns></returns>
    public bool addTexture(string texturePath, bool isAnimation)
    {
        // Insert to keep textures sorted by width

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        float w = texture.width;

        if (w > pageWidth)
        {
            return false;
        }
        if (w % tileWidth != 0 || texture.height % tileHeight != 0)
        {
            return false;
        }

        if (isAnimation)
        {
            animations.Add(texture);
        }
        else
        {
            int insertionPos = 0;

            for (insertionPos = 0; insertionPos < textures.Count; insertionPos++)
            {
                if (textures[insertionPos].width < w)
                {
                    break;
                }
            }
            textures.Insert(insertionPos, texture);
        }

        packTextures();

        return true;
    }

    public void removeTexture(int index)
    {
        textures.RemoveAt(index);
        textureAssets.RemoveAt(index);
        packTextures();
        saveTexturePageToFile(tilesetName);
    }
    public void packTextures()
    {
        Debug.Log("Packing texture page for tileset: " + tilesetName);
        int packedHeight = 0;
        int widestTexture = 0;

        int fullTileWidth = tileWidth + tileOutline * 2;
        int fullTileHeight = tileHeight + tileOutline * 2;

        for (int i = 0; i < textures.Count; i++)
        {
            packedHeight += textures[i].height;
            if (textures[i].width > widestTexture)
            {
                widestTexture = textures[i].width;
            }
        }

        pageWidth = widestTexture;

        int tilesOccupied = 0;
        int tilesPerRow = pageWidth / tileWidth;
        int animationRows = 0;

        // Add the outlines into the width of the texture page
        int editorTilesetWidth = pageWidth;
        pageWidth += (tileOutline * 2 * tilesPerRow);

        for (int i = 0; i < animations.Count; i++)
        {
            tilesOccupied += (animations[i].width / tileWidth) * (animations[i].height / tileHeight);
            while (tilesOccupied > tilesPerRow)
            {
                tilesOccupied -= tilesPerRow;
                animationRows++;
            }
        }
        if (tilesOccupied > 0)
        {
            animationRows++;
        }

        pageHeight = packedHeight;

        int editorPackedHeight = packedHeight;
        if (animations.Count > 0) { editorPackedHeight += (animations.Count / tilesPerRow + 1) * tileHeight; }
        
        pageHeight += animationRows * tileHeight;

        pageHeight += (pageHeight / tileHeight) * tileOutline * 2;

        texturePage = new Texture2D(pageWidth, pageHeight, TextureFormat.RGBA32, false);
        textureEditorTileset = new Texture2D(editorTilesetWidth, editorPackedHeight, TextureFormat.RGBA32, false);
        editorMappedPoints = new List<Vector2>();
        tilesetMappedPoints = new List<Vector2>();
        
        editorTilesetHeight = editorPackedHeight;
        this.editorTilesetWidth = editorTilesetWidth;

        animationLength = new int[(editorTilesetWidth / tileWidth) * (editorTilesetHeight / tileHeight)];

        // blit all textures vertically, sorted by width
        int texY = 0;
        int texPageY = 0;
        for (int i = 0; i < textures.Count; i++)
        {
            texY += textures[i].height;
            texPageY += textures[i].height + (textures[i].height / tileHeight) * (tileOutline * 2);
            //blitPixelsToTexture(0, pageHeight - texY, textures[i], texturePage);
            blitPixelsToTexture(0, editorPackedHeight - texY, textures[i], textureEditorTileset);

            int tileCount = (textures[i].width / tileWidth) * (textures[i].height / tileWidth);

            int tilesPerRowInTexture = textures[i].width / tileWidth;

            int tilesToTexturePixelOffsetY = textures[i].height - Mathf.CeilToInt((float)tileCount / tilesPerRowInTexture) * (tileHeight);

            // blit one tile at a time to ensure proper outlines
            for (int ii = 0; ii < tileCount; ii++) 
            {
                int srcTileX = (ii % tilesPerRowInTexture) * (tileWidth);
                int srcTileY = (ii / tilesPerRowInTexture) * (tileHeight) + tilesToTexturePixelOffsetY;

                int tileX = (ii % tilesPerRowInTexture) * fullTileWidth;
                int tileY = (ii / tilesPerRowInTexture) * fullTileHeight + tilesToTexturePixelOffsetY;

                blitPixelsToTexture(tileX, pageHeight - texPageY + tileY, srcTileX, srcTileY, tileWidth, tileHeight, textures[i], texturePage, tileOutline);
            }
        }

        // blit all animation frames as a block spritesheet
        uvAnimations = new List<UVAnimation>();
        int startX = 0;
        int startY = texPageY;
        for (int i = 0; i < animations.Count; i++)
        {
            int tileCount = (animations[i].width / tileWidth) * (animations[i].height / tileHeight);
            UVAnimation animationData = new UVAnimation();
            animationData.imageCount = tileCount;
            animationData.startTileX = startX;
            animationData.startTileY = startY;
            animationData.imageSpeed = 4;
            uvAnimations.Add(animationData);
           
            int animX = (i % tilesPerRow ) * tileWidth;
            int animY = editorPackedHeight - (texY + i / tilesPerRow) - tileHeight;
            blitPixelsToTexture(animX, animY, 0, 0, tileWidth, tileHeight, animations[i], textureEditorTileset);
            animationLength[(animX / tileWidth)+ ( animY / tileHeight) * (pageWidth / (fullTileWidth))] = tileCount;


            tilesetMappedPoints.Add(new Vector2(i % tilesPerRow, texY / tileHeight + i / tilesPerRow));
            editorMappedPoints.Add(new Vector2(startX / fullTileWidth, startY / fullTileHeight));

            // blit one tile at a time to save space
            for (int ii = 0; ii < tileCount; ii++)
            {
                blitPixelsToTexture(
                    startX, 
                    pageHeight - startY - fullTileHeight,
                    (ii * tileWidth) % animations[i].width, 
                    tileHeight * (ii / (animations[i].width / tileWidth)), 
                    tileWidth, 
                    tileHeight, 
                    animations[i], 
                    texturePage, tileOutline);
                
                startX += tileWidth + tileOutline * 2;
                if (startX >= tilesPerRow * tileWidth + tileOutline * 2)
                {
                    startX = 0;
                    startY += tileHeight + tileOutline * 2;
                }
            }
        }
        
    }

    private void blitPixelsToTexture(int x, int y, Texture2D sourceTexture, Texture2D targetTexture)
    {
        blitPixelsToTexture(x, y, 0, 0, sourceTexture.width, sourceTexture.height, sourceTexture, targetTexture);
    }
    private void blitPixelsToTexture(int destX, int destY, int srcX, int srcY, int srcWidth, int srcHeight, Texture2D sourceTexture, Texture2D targetTexture, int outline = 0)
    {
        for (int y = 0; y < srcHeight + outline * 2; y++)
        {
            for (int x = 0; x < srcWidth + outline * 2; x++)
            {

                int srcXX = x - outline;
                int srcYY = y - outline;
                if (srcXX < 0) { srcXX = 0; }
                if (srcYY < 0) { srcYY = 0; }
                if (srcXX >= srcWidth) { srcXX = srcWidth - 1; }
                if (srcYY >= srcHeight) { srcYY = srcHeight - 1; }

                Color col = sourceTexture.GetPixel(srcX + srcXX, srcY + srcYY);

                targetTexture.SetPixel(destX + x, destY + y, col);
            }
        }
    }
    
    public void saveTexturePageToFile(string fileName)
    {
        if (texturePage != null)
        {
            MeshEditWindow.checkEditorFoldersExist();

            byte[] bytes = texturePage.EncodeToPNG();

            File.WriteAllBytes(Application.dataPath + TilesetManager.locationOfTilesetPagesWindows + "/" + fileName + ".png", bytes);
            
            // Importasset has to have the extension added, resources.load doesn't.
            AssetDatabase.ImportAsset("Assets" + TilesetManager.locationOfTilesetPagesWindows + " / " + fileName + ".png");
           

            byte[] bytesEditor = textureEditorTileset.EncodeToPNG();

            File.WriteAllBytes(Application.dataPath + TilesetManager.locationOfTilesetInfoWindows + "/" + fileName + ".png", bytesEditor);
            
            AssetDatabase.ImportAsset("Assets" + TilesetManager.locationOfTilesetInfoWindows + " / " +  fileName + ".png");
            
            AssetDatabase.Refresh();
            
        }
    }

    public int getAnimationLength(int selectedTile)
    {
        if (animationLength == null)
        {
            packTextures();
        }
        int w = (editorTilesetWidth / tileWidth);
        int h = (editorTilesetHeight / tileHeight);
        int selectedX = selectedTile % w;
        int selectedY = (h - 1) - selectedTile / w;

        return animationLength[selectedX + selectedY * w];
    }
#endif
}

#if UNITY_EDITOR
public class TilesetManager : EditorWindow
{
    bool groupEnabled;

    List<Tileset> tilesetsAvailable;
    List<string> tilesetTexturesAvailable;
    int selectedTileset;
    // Add menu item named "My Window" to the Window menu
    [MenuItem("Window/Tilesets")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        TilesetManager window = (TilesetManager)EditorWindow.GetWindow(typeof(TilesetManager));

        window.initialise();
    }

    public static string locationOfTilesetInfoWindows = "/MeshEdit/Tilesets/TilesetInfo";
    public static string locationOfTilesetPagesWindows = "/MeshEdit/Tilesets/TexturePages";

    bool isUpToDate = true;
    Texture2D newTexture = null;
    Texture2D newAnimation = null;
    List<Texture2D> textures;
    List<Texture2D> animations;
    string tilesetName;
    int tileWidth;
    int tileHeight;
    int tileOutline;

    void initialise()
    {
        if (tilesetsAvailable == null)
        {
            loadTilesets();
        }
        if (tilesetTexturesAvailable == null)
        {
            loadTilesets();
        }
        changeActiveTileset(0);
    }

    Vector2 scrollPos = Vector2.zero;
    void Update()
    {
        if (textures == null || animations == null)
        {
            int oldSelected = selectedTileset;
            selectedTileset = -1;
            changeActiveTileset(oldSelected);
        }

        if (textures != null && textures.Count > 0)
        {
            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i] == null)
                {
                    textures.RemoveAt(i);
                    i--;
                }
            }
        }
        if (animations != null && animations.Count > 0)
        {
            for (int i = 0; i < animations.Count; i++)
            {
                if (animations[i] == null)
                {
                    animations.RemoveAt(i);
                    i--;
                }
            }
        }
        if (newTexture != null && focusedWindow == this)
        {
            if (isTextureReadable(newTexture))
            {
                isUpToDate = false;
                textures.Add(newTexture);
            }
            else
            {
                Debug.Log("Error! The texture chosen was un-readable. Textures added to a tileset must be made readable in the editor.");
            }
            newTexture = null;
        }

        if (newAnimation != null && focusedWindow == this)
        {
            if (isTextureReadable(newAnimation))
            {
                isUpToDate = false;
                animations.Add(newAnimation);
            }
            else
            {
                Debug.Log("Error! The animation chosen was un-readable. Textures added to a tileset must be made readable in the editor.");
            }
            newAnimation = null;
        }

    }

    void OnGUI()
    {
        // GUILayout.Label("Base Settings", EditorStyles.boldLabel);




        // GUILayout.Label(EditorWindow.focusedWindow.ToString());


        if (tilesetsAvailable == null)
        {
            loadTilesets();
        }
        if (tilesetTexturesAvailable == null)
        {
            loadTilesets();
        }
        // Tileset selection (Drop down)
        int nextTileset = selectedTileset;
        nextTileset = EditorGUILayout.Popup(selectedTileset, tilesetTexturesAvailable.ToArray(), GUILayout.Width(100));
        if (nextTileset != selectedTileset)
        {
            changeActiveTileset(nextTileset);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ New Tileset", GUILayout.Width(100)))
        {
            Tileset tileset = new Tileset();
            string fileName = "New Tileset";
            int num = 0;
            while (tilesetTexturesAvailable.Contains(fileName))
            {
                num++;
                fileName = "New Tileset (" + num.ToString() + ")";

            }
            tileset.tilesetName = fileName;
            tilesetName = fileName;

            tileWidth = 64;
            tileHeight = 64;

            tilesetsAvailable.Add(tileset);
            tilesetTexturesAvailable.Add(fileName);

            changeActiveTileset(tilesetsAvailable.Count - 1);
        }
        if (tilesetsAvailable.Count > 0 && selectedTileset >= 0 && selectedTileset < tilesetsAvailable.Count)
        {
            if (GUILayout.Button("- Delete Tileset", GUILayout.Width(100)))
            {
                // Delete the files from the directories
                deleteTilesetFiles(selectedTileset);
                AssetDatabase.Refresh();
                // Remove from the list
                tilesetsAvailable.RemoveAt(selectedTileset);
                tilesetTexturesAvailable.RemoveAt(selectedTileset);
                nextTileset = selectedTileset - 1;
                if (nextTileset < 0) { nextTileset = 0; }

                // Set up values so that the deleted tileset is not referenced, and a tileset change is forced
                isUpToDate = true;
                selectedTileset = -1;
                changeActiveTileset(nextTileset);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (tilesetsAvailable.Count > 0 && selectedTileset >= 0 && selectedTileset < tilesetsAvailable.Count)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("File Name ", GUILayout.Width(80));
            string oldTilesetName = tilesetName;
            tilesetName = EditorGUILayout.TextField(tilesetName);
            if (oldTilesetName != tilesetName)
            {
                isUpToDate = false;
            }

            GUILayout.FlexibleSpace();

            //GUI.enabled = (!isUpToDate);
            string buttonName = "Apply Changes";
            if (isUpToDate) { buttonName = "Re-Apply Changes"; }
            if (GUILayout.Button(buttonName))
            {
                applyChanges();
                Selection.activeGameObject = null;
            }
            //GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            int newTileWidth = tileWidth;
            EditorGUILayout.LabelField("Tile Width ", GUILayout.Width(80));
            newTileWidth = EditorGUILayout.IntField(tileWidth, GUILayout.Width(70));
            if (newTileWidth != tileWidth && newTileWidth > 0)
            {
                tileWidth = newTileWidth;
                isUpToDate = false;
            }

            int newTileHeight = tileHeight;
            EditorGUILayout.LabelField("Tile Height ", GUILayout.Width(80));
            newTileHeight = EditorGUILayout.IntField(tileHeight, GUILayout.Width(70));
            if (newTileHeight != tileHeight && newTileHeight > 0)
            {
                tileHeight = newTileHeight;
                isUpToDate = false;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            int newTileOutline = tileOutline;
            EditorGUILayout.LabelField("Tile Spacing ", GUILayout.Width(80));
            newTileOutline = EditorGUILayout.IntField(tileOutline, GUILayout.Width(70));
            if (newTileOutline != tileOutline && newTileOutline >= 1)
            {
                tileOutline = newTileOutline;
                isUpToDate = false;
            }

            EditorGUILayout.EndHorizontal();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            if (tilesetTexturesAvailable != null && tilesetTexturesAvailable.Count > 0 && textures != null)
            {
                EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);

                for (int i = 0; i < textures.Count; i++)
                {
                    Texture2D oldT2D = textures[i];
                    textures[i] = (Texture2D)EditorGUILayout.ObjectField(textures[i], typeof(Texture2D), false, GUILayout.Width(240), GUILayout.Height(16));

                    if (oldT2D != textures[i])
                    {
                        isUpToDate = false;
                    }
                }
                newTexture = (Texture2D)EditorGUILayout.ObjectField(newTexture, typeof(Texture2D), false, GUILayout.Width(240), GUILayout.Height(16));
                /*
                EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);

                for (int i = 0; i < animations.Count; i++)
                {
                    Texture2D oldT2D = animations[i];
                    animations[i] = (Texture2D)EditorGUILayout.ObjectField(animations[i], typeof(Texture2D), false, GUILayout.Width(240), GUILayout.Height(16));

                    if (oldT2D != animations[i])
                    {
                        isUpToDate = false;
                    }
                }

                newAnimation = (Texture2D)EditorGUILayout.ObjectField(newAnimation, typeof(Texture2D), false, GUILayout.Width(240), GUILayout.Height(16));
                */
            }

            GUILayout.Space(32);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (tilesetsAvailable != null &&
                tilesetsAvailable[selectedTileset].textureAssets != null && tilesetsAvailable[selectedTileset].textureAssets.Count > 0 &&
                tilesetsAvailable[selectedTileset].animationAssets != null && tilesetsAvailable[selectedTileset].animationAssets.Count > 0)
            {
                EditorGUILayout.LabelField("Page size: " + tilesetsAvailable[selectedTileset].texturePage.width + "x" + tilesetsAvailable[selectedTileset].texturePage.height, GUILayout.Width(120));
            }

            EditorGUILayout.EndHorizontal();
        }

        if (Event.current.type == EventType.Layout)
        {

        }
    }

    private void changeActiveTileset(int index)
    {
        if (index != selectedTileset)
        {
            if (tilesetsAvailable != null && tilesetsAvailable.Count > 0)
            {
                selectedTileset = index;
                isUpToDate = true;
                tilesetsAvailable[selectedTileset].loadTexturesFromAssets();
                tilesetName = tilesetsAvailable[selectedTileset].tilesetName;
                tileWidth = tilesetsAvailable[selectedTileset].tileWidth;
                tileHeight = tilesetsAvailable[selectedTileset].tileHeight;
                tileOutline = tilesetsAvailable[selectedTileset].tileOutline;
                textures = new List<Texture2D>();
                for (int i = 0; i < tilesetsAvailable[selectedTileset].textureAssets.Count; i++)
                {
                    textures.Add(tilesetsAvailable[selectedTileset].textures[i]);
                }
                animations = new List<Texture2D>();
                for (int i = 0; i < tilesetsAvailable[selectedTileset].animations.Count; i++)
                {
                    animations.Add(tilesetsAvailable[selectedTileset].animations[i]);
                }
            }
        }
    }

    private void applyChanges()
    {
        Debug.Log("Updating tileset");
        deleteTilesetFiles(selectedTileset);

        tilesetsAvailable[selectedTileset].reConstructTileset(tilesetName, textures, animations, tileWidth, tileHeight, tileOutline);
        tilesetsAvailable[selectedTileset].packTextures();
        tilesetsAvailable[selectedTileset].saveTexturePageToFile(tilesetName);

        saveTileset(selectedTileset);
        AssetDatabase.Refresh();
        loadTilesets();
        isUpToDate = true;

        MeshEdit[] meshes = GameObject.FindObjectsOfType<MeshEdit>();

        for (int i = 0; i < meshes.Length; i++)
        {
            bool refresh = false;

            for (int ii = 0; ii < meshes[i].uvMaps.Count; ii++)
            {
                if (meshes[i].uvMaps[ii].name == tilesetName)
                {
                    refresh = true;

                    meshes[i].uvMaps[ii].resizeUVSpace(
                        tilesetsAvailable[selectedTileset].texturePage.width,
                        tilesetsAvailable[selectedTileset].texturePage.height,
                        tilesetsAvailable[selectedTileset].tileWidth,
                        tilesetsAvailable[selectedTileset].tileHeight,
                        tilesetsAvailable[selectedTileset].tileOutline);

                }
            }

            if (refresh)
            {
                meshes[i].pushUVData();
                meshes[i].isTilesetRefreshRequired = true;
            }
        }

    }

    private void deleteTilesetFiles(int selectedTileset)
    {

        // Delete original XML file
        string path = Application.dataPath + TilesetManager.locationOfTilesetInfoWindows + "/" + tilesetsAvailable[selectedTileset].tilesetName + ".xml";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        // Delete the editor tileset
        path = Application.dataPath + TilesetManager.locationOfTilesetInfoWindows + "/" + tilesetsAvailable[selectedTileset].tilesetName + ".png";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        // Delete the constructed texture page
        path = Application.dataPath + TilesetManager.locationOfTilesetPagesWindows + "/" + tilesetsAvailable[selectedTileset].tilesetName + ".png";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void saveTileset(int tilesetIndex)
    {
        SerializeObject(tilesetsAvailable[tilesetIndex], Application.dataPath + TilesetManager.locationOfTilesetInfoWindows + "/" + tilesetsAvailable[tilesetIndex].tilesetName);
    }

    private void loadTilesets()
    {
        tilesetsAvailable = new List<Tileset>();
        tilesetTexturesAvailable = new List<string>();


        DirectoryInfo d = new DirectoryInfo(Application.dataPath + TilesetManager.locationOfTilesetInfoWindows);
        FileInfo[] files = d.GetFiles();

        foreach (FileInfo xmlFile in files)
        {
            if (xmlFile.Extension.EndsWith("xml"))
            {
                Tileset tileset = DeSerializeObject<Tileset>(xmlFile.FullName);
                tileset.loadTexturesFromAssets();
                //tileset.packTextures();

                // If either file has been manually renamed then make sure no asset is loaded, since it would cause a FileNotFound error when loading the texture page.

                tilesetsAvailable.Add(tileset);
                tilesetTexturesAvailable.Add(tileset.tilesetName);

            }
        }
    }

    private void createTileset(string name)
    {

    }

    public void SerializeObject<T>(T serializableObject, string fileName)
    {
        if (serializableObject == null) { return; }

        try
        {
            XmlDocument xmlDocument = new XmlDocument();
            XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize(stream, serializableObject);
                stream.Position = 0;
                xmlDocument.Load(stream);
                xmlDocument.Save(fileName + ".xml");
                stream.Close();
            }
        }
        catch (Exception ex)
        {
            throw (ex);
        }
    }

    public bool isTextureReadable(Texture2D texture)
    {

        string texturePath = AssetDatabase.GetAssetPath(texture);
        TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        if (textureImporter.isReadable)
        {
            return true;
        }
        textureImporter.isReadable = true;
        AssetDatabase.ImportAsset(texturePath);

        AssetDatabase.Refresh();
        return true;

        // Instead of checking, we can just set the texture to be readable, read the texture and then set it back to it's original state.
    }

    /// <summary>
    /// Deserializes an xml file into an object list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static T DeSerializeObject<T>(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) { return default(T); }

        T objectOut = default(T);

        try
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(fileName);
            string xmlString = xmlDocument.OuterXml;

            using (StringReader read = new StringReader(xmlString))
            {
                Type outType = typeof(T);

                XmlSerializer serializer = new XmlSerializer(outType);
                using (XmlReader reader = new XmlTextReader(read))
                {
                    objectOut = (T)serializer.Deserialize(reader);
                    reader.Close();
                }

                read.Close();
            }
        }
        catch (Exception ex)
        {
            throw (ex);
        }

        return objectOut;
    }




}
#endif