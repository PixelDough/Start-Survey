using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("")]
public class MeshEdit : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("GameObject/Mesh Edit/Create Custom Mesh", false, 0)]
    public static void createCustomMesh()
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

        GameObject[] objects = new GameObject[1];
        objects[0] = new GameObject("Custom Mesh");
        objects[0].transform.position = SceneView.lastActiveSceneView.pivot;

        MeshFilter mf = objects[0].AddComponent<MeshFilter>();
        mf.mesh = customCube;

        MeshRenderer mr = objects[0].AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("MeshEdit/ModellingShader"));

        MeshEdit me = objects[0].AddComponent<MeshEdit>();

        // Full MeshEdit initialisation
        me.deepCopyMesh(ref customCube, ref me._mesh);

        me.getVerts(false);

        me.tris = new List<int>();
        for (int i = 0; i < me.mesh.triangles.Length; i++)
        {
            me.tris.Add(me.mesh.triangles[i]);
        }

        me.getQuads(false);

        me.getConnectedVertsBasedOnPosition();

        me.rebuildAdjacentFaces();

        me.defineAllHalfEdges();

        me.getMaterials();

        me.customTextureUVMap = new List<Vector2>();
        for (int i = 0; i < me.verts.Count; i++)
        {
            me.customTextureUVMap.Add(Vector2.zero);
        }

        me.checkMeshValidity();

        me.recalculateNormals(me._mesh);
        
        Undo.RegisterCreatedObjectUndo(objects[0], "Create MeshEdit object");

        MeshEditSceneInterface.loadAllUVMaps(me);

        Selection.objects = objects;

        SceneView sceneView = (SceneView)SceneView.sceneViews[0];
        sceneView.Focus();
    }

    [MenuItem("GameObject/Mesh Edit/Import Custom Mesh")]
    public static void importCustomMesh()
    {
        MeshEditWindow.importObj();
    }
#endif
    [SerializeField, HideInInspector]
    string version = "0.4";
    public bool checkVersion()
    {
        if (version != "0.4")
        {
            return false;
        }

        return true;
    }

    [SerializeField, HideInInspector]
    public bool loadedFromPrefab = false;

    [System.Serializable]
    public class UVData : System.Object
    {
        [SerializeField, HideInInspector]
        public string name;
        [SerializeField, HideInInspector]
        public int texWidth;
        [SerializeField, HideInInspector]
        public int texHeight;
        [SerializeField, HideInInspector]
        public int tileWidth;
        [SerializeField, HideInInspector]
        public int tileHeight;
        [SerializeField, HideInInspector]
        public int tileOutline;
        // Used for temp storage in editor functions
        [SerializeField, HideInInspector]
        public Vector2[] _newUvs;
        public Vector2[] newUvs
        {
            get
            {
                if (_newUvs == null)
                {
                    _newUvs = new Vector2[vertCount];
                    for (int i = 0; i < _newUvs.Length; i++)
                    {
                        getNeutralCoord(i, ref _newUvs[i].x, ref _newUvs[i].y);
                    }
                }
                else if (_newUvs.Length != vertCount)
                {
                    Vector2[] newArray = new Vector2[vertCount];

                    for (int i = 0; i < newArray.Length; i++)
                    {
                        if (i >= _newUvs.Length)
                        {
                            getNeutralCoord(i, ref newArray[i].x, ref newArray[i].y);
                        }
                        else
                        {
                            newArray[i] = _newUvs[i];
                        }
                    }

                    _newUvs = newArray;
                }

                return _newUvs;
            }
            set
            {
                _newUvs = value;
            }
        }
        [SerializeField, HideInInspector]
        public Vector2[] _uvs;
        public Vector2[] uvs
        {
            get
            {
                if (_uvs == null)
                {
                    _uvs = new Vector2[vertCount];

                    for (int i = 0; i < _uvs.Length; i++)
                    {
                        getNeutralCoord(i, ref _uvs[i].x, ref _uvs[i].y);
                    }
                }
                else if (_uvs.Length != vertCount)
                {
                    Vector2[] newArray = new Vector2[vertCount];

                    for (int i = 0; i < newArray.Length; i++)
                    {
                        if (i >= _uvs.Length)
                        {
                            getNeutralCoord(i, ref newArray[i].x, ref newArray[i].y);
                        }
                        else
                        {
                            newArray[i] = _uvs[i];
                        }
                    }

                    _uvs = newArray;
                }

                return _uvs;
            }
            set
            {
                _uvs = value;
            }
        }

        public Vector2[] defaultUVs;

        public void getNeutralCoord(int i, ref float x, ref float y)
        {
            float tx = (float)tileOutline / texWidth;
            float ty = 1.0f - (float)tileOutline / texHeight;

            float tw = (float)tileWidth / texWidth;
            float th = (float)tileHeight / texHeight;


            if (defaultUVs != null && i < defaultUVs.Length)
            {
                x = defaultUVs[i].x;
                y = defaultUVs[i].y;
            }
            else
            {
                if (i % 4 == 0)
                {
                    x = tx;
                    y = ty;
                }
                else if (i % 4 == 1)
                {
                    x = tx + tw;
                    y = ty;
                }
                else if (i % 4 == 2)
                {
                    x = tx;
                    y = ty - th;
                }
                else if (i % 4 == 3)
                {
                    x = tx + tw;
                    y = ty - th;
                }
            }

        }
        [SerializeField, HideInInspector]
        public int[] _uvAnimationLength;
        public int[] uvAnimationLength
        {
            get
            {
                if (_uvAnimationLength == null)
                {
                    _uvAnimationLength = new int[vertCount];
                }
                else if (_uvAnimationLength.Length != vertCount)
                {
                    int[] newArray = new int[vertCount];
                    for (int i = 0; i < newArray.Length; i++)
                    {
                        if (i >= _uvAnimationLength.Length)
                        {
                            newArray[i] = 0;
                        }
                        else
                        {
                            newArray[i] = _uvAnimationLength[i];
                        }
                    }
                    _uvAnimationLength = newArray;
                }
                return _uvAnimationLength;
            }
            set
            {
                _uvAnimationLength = value;
            }
        }
        [SerializeField, HideInInspector]
        public int[] _uvAnimationStartPosition;
        public int[] uvAnimationStartPosition
        {
            get
            {
                if (_uvAnimationStartPosition == null)
                {
                    _uvAnimationStartPosition = new int[vertCount];
                }
                else if (_uvAnimationStartPosition.Length != vertCount)
                {
                    int[] newArray = new int[vertCount];
                    for (int i = 0; i < newArray.Length; i++)
                    {
                        if (i >= _uvAnimationStartPosition.Length)
                        {
                            newArray[i] = 0;
                        }
                        else
                        {
                            newArray[i] = _uvAnimationStartPosition[i];
                        }
                    }
                    _uvAnimationStartPosition = newArray;
                }
                return _uvAnimationStartPosition;
            }
            set
            {
                _uvAnimationStartPosition = value;
            }
        }
        [SerializeField, HideInInspector]
        public int[] _uvAnimationIndex;
        public int[] uvAnimationIndex
        {
            get
            {
                if (_uvAnimationIndex == null)
                {
                    _uvAnimationIndex = new int[vertCount];
                }
                else if (_uvAnimationIndex.Length != vertCount)
                {
                    int[] newArray = new int[vertCount];
                    for (int i = 0; i < newArray.Length; i++)
                    {
                        if (i >= _uvAnimationIndex.Length)
                        {
                            newArray[i] = 0;
                        }
                        else
                        {
                            newArray[i] = _uvAnimationIndex[i];
                        }
                    }
                    _uvAnimationIndex = newArray;
                }
                return _uvAnimationIndex;
            }
            set
            {
                _uvAnimationIndex = value;
            }
        }
        [SerializeField, HideInInspector]
        public int[] _uvAnimationQuadPoint;
        public int[] uvAnimationQuadPoint
        {
            get
            {
                if (_uvAnimationQuadPoint == null)
                {
                    _uvAnimationQuadPoint = new int[vertCount];
                }
                else if (_uvAnimationQuadPoint.Length != vertCount)
                {
                    int[] newArray = new int[vertCount];
                    for (int i = 0; i < newArray.Length; i++)
                    {
                        if (i >= _uvAnimationQuadPoint.Length)
                        {
                            newArray[i] = 0;
                        }
                        else
                        {
                            newArray[i] = _uvAnimationQuadPoint[i];
                        }
                    }
                    _uvAnimationQuadPoint = newArray;
                }
                return _uvAnimationQuadPoint;
            }
            set
            {
                _uvAnimationQuadPoint = value;
            }
        }
        [SerializeField, HideInInspector]
        int animationSpeed;
        [SerializeField, HideInInspector]
        int animationTimer;
        [SerializeField, HideInInspector]
        public int vertCount;

        public UVData(string name, int texWidth, int texHeight, int tileWidth, int tileHeight, int tileOutline, int vertCount, Vector2[] defaultUVs = null)
        {
            if (defaultUVs != null)
            {
                this.defaultUVs = defaultUVs;
            }
            this.name = name;
            this.texWidth = texWidth;
            this.texHeight = texHeight;
            this.tileWidth = tileWidth;
            this.tileHeight = tileHeight;
            this.tileOutline = tileOutline;
            animationSpeed = 4;
            animationTimer = 0;

            this.vertCount = vertCount;
        }

        public void resizeUVSpace(int texWidth, int texHeight, int tileWidth, int tileHeight, int tileOutline)
        {
            // TODO: When the same texture is updated, compare the previous texture width/height and re-size the UVs so that the texture isn't warped.
            if (texWidth != 0 && texHeight != 0 && tileWidth != 0 && tileHeight != 0 &&
                (texWidth != this.texWidth || texHeight != this.texHeight || tileOutline != this.tileOutline ||
                tileWidth != this.tileWidth || tileHeight != this.tileHeight))
            {
                for (int i = 0; i < uvs.Length; i++)
                {

                    // Remove original outline from original uv coordinate
                    float x = uvs[i].x * this.texWidth;
                    float y = (1.0f - uvs[i].y) * this.texHeight;

                    float newX = 0;
                    float newY = 0;

                    float thisTotalTileWidth = (this.tileWidth + this.tileOutline * 2);

                    float thisTotalTileHeight = (this.tileHeight + this.tileOutline * 2);

                    float midX = (int)(x / thisTotalTileWidth) * thisTotalTileWidth + (thisTotalTileWidth / 2.0f);
                    float midY = (int)(y / thisTotalTileHeight) * thisTotalTileHeight + (thisTotalTileHeight / 2.0f);

                    float xCoord = (int)(x / (this.tileWidth + this.tileOutline * 2));
                    float yCoord = (int)(y / (this.tileHeight + this.tileOutline * 2));

                    float newMidX = xCoord * (tileWidth + tileOutline * 2) + (tileWidth / 2.0f + tileOutline);
                    float newMidY = yCoord * (tileHeight + tileOutline * 2) + (tileHeight / 2.0f + tileOutline);

                    if (x < midX)
                    {
                        x -= this.tileOutline;
                        newX = newMidX - tileWidth / 2.0f;
                    }
                    else
                    {
                        x += this.tileOutline;
                        newX = newMidX + tileWidth / 2.0f;
                    }
                    if (y > midY)
                    {
                        y -= this.tileOutline;
                        newY = newMidY + tileHeight / 2.0f;
                    }
                    else
                    {
                        y += this.tileOutline;
                        newY = newMidY - tileHeight / 2.0f;
                    }

                    newX = newX / texWidth;
                    newY = newY / texHeight;
                    uvs[i].x = newX;
                    uvs[i].y = 1.0f - newY;

                    newUvs[i].x = newX;
                    newUvs[i].y = 1.0f - newY;

                    /*

                    float tileX = this.texWidth - uvs[i].x * this.texWidth;
                    float newTileX = (texWidth - tileX) / texWidth;
                    float tileY = this.texHeight - uvs[i].y * this.texHeight;
                    float newTileY = (texHeight - tileY) / texHeight;

                    uvs[i].x = newTileX;
                    uvs[i].y = newTileY;*/
                }
                this.texHeight = texHeight;
                this.texWidth = texWidth;
                this.tileWidth = tileWidth;
                this.tileHeight = tileHeight;
                this.tileOutline = tileOutline;

            }


        }

        public void resizeUVLength(int vertCount, int[] newVerts = null)
        {
            this.vertCount = vertCount;

            Vector2[] updateV2 = uvs;
            updateV2 = newUvs;
            int[] update = uvAnimationLength;
            update = uvAnimationQuadPoint;
            update = uvAnimationIndex;
            update = uvAnimationStartPosition;

            if (newVerts != null)
            {
                Console.WriteLine("-uvs changed");
                Console.WriteLine("-newUvs changed");
                for (int i = 0; i < newVerts.Length; i++)
                {
                    uvs[vertCount - newVerts.Length + i] = uvs[newVerts[i]];
                    newUvs[vertCount - newVerts.Length + i] = newUvs[newVerts[i]];
                }
            }
        }

        public Vector2 getCoordsOfTile(Rect rect, int tileWidth, int tileHeight, int texWidth, int texHeight, int tileOutline)
        {
            Vector2 coords = new Vector2(0, 0);
            float unitsX = (tileWidth + tileOutline * 2);
            float unitsY = (tileHeight + tileOutline * 2);
            coords.x = (rect.x * tileWidth - tileOutline) / unitsX;
            coords.y = (rect.y * tileHeight - tileOutline) / unitsY;

            return coords;
        }
        public Rect getTile(int x, int y, int tileWidth, int tileHeight, int texWidth, int texHeight, int tileOutline)
        {
            Rect tile = new Rect(0, 0, 0, 0);

            tile.x = (x * (tileWidth + tileOutline * 2) + tileOutline) / texWidth;
            tile.y = (y * (tileHeight + tileOutline * 2) + tileOutline) / texHeight;
            tile.width = (tileWidth) / texWidth;
            tile.height = (tileHeight) / texHeight;

            return tile;
        }

        public void update(Mesh mesh)
        {
            if (uvAnimationLength == null ||
                uvAnimationLength.Length != uvs.Length)
            {
                return;
            }
            if (uvAnimationIndex == null ||
                uvAnimationIndex.Length != uvs.Length)
            {
                return;
            }
            if (uvAnimationStartPosition == null ||
                uvAnimationStartPosition.Length != uvs.Length)
            {
                return;
            }

            if (uvs != null &&
                uvAnimationLength != null &&
                uvAnimationIndex != null &&
                uvAnimationStartPosition != null)
            {

                if (uvs.Length != mesh.vertices.Length)
                {
                    uvAnimationLength = null;
                    uvAnimationIndex = null;
                    uvAnimationQuadPoint = null;
                    uvAnimationStartPosition = null;
                    uvs = null;
                    newUvs = null;
                    return;
                }

                // TODO: Temporarily holding this feature off until we can organise a dedicated class for the UV animator
                //animationTimer++;

                if (animationTimer > animationSpeed)
                {
                    float xUnit = 1.0f / (texWidth / (tileWidth + tileOutline * 2));
                    float yUnit = 1.0f / (texHeight / (tileHeight + tileOutline * 2));

                    animationTimer = 0;

                    newUvs = new Vector2[uvs.Length];
                    for (int i = 0; i < uvs.Length; i++)
                    {
                        newUvs[i] = uvs[i];

                        if (uvAnimationLength[i] > 1)
                        {
                            uvAnimationIndex[i]++;
                            if (uvAnimationIndex[i] >= uvAnimationLength[i])
                            {
                                int tilesPerRow = texWidth / (tileWidth + tileOutline * 2);
                                int tilesPerColumn = texHeight / (tileHeight + tileOutline * 2);
                                float x = (uvAnimationStartPosition[i] % tilesPerRow) / (float)tilesPerRow + tileOutline / (float)texWidth;
                                float y = (uvAnimationStartPosition[i] / tilesPerRow) / (float)tilesPerColumn + tileOutline / (float)texHeight;



                                // This stretches out the quads to fit the UV tile
                                float qOffsetX = 0;
                                if (uvAnimationQuadPoint[i] == 1 || uvAnimationQuadPoint[i] == 2)
                                {
                                    qOffsetX = xUnit - (tileOutline * 2 / (float)texWidth);
                                }
                                float qOffsetY = 0;
                                if (uvAnimationQuadPoint[i] == 0 || uvAnimationQuadPoint[i] == 1)
                                {
                                    qOffsetY = yUnit - (tileOutline * 2 / (float)texHeight);
                                }


                                newUvs[i].x = x + qOffsetX;
                                newUvs[i].y = y + qOffsetY;

                                uvAnimationIndex[i] = 0;
                            }
                            else
                            {
                                // qOffset is the tile Width offset from the root position of the UV
                                float qOffset = 0;
                                if (uvAnimationQuadPoint[i] == 1 || uvAnimationQuadPoint[i] == 2)
                                {
                                    qOffset = xUnit - (tileOutline * 2 / (float)texWidth);
                                }

                                newUvs[i].x += xUnit;

                                if (newUvs[i].x - qOffset >= 1.0f)
                                {
                                    newUvs[i].x = qOffset + tileOutline / (float)texWidth;
                                    newUvs[i].y -= yUnit;
                                }
                            }
                        }
                    }

                    mesh.uv = newUvs;
                    //uvs = newUvs;
                }
            }
        }

        public void intialise(int length)
        {

            if (uvAnimationLength == null ||
                uvAnimationLength.Length != length)
            {
                uvAnimationLength = new int[length];
            }
            if (uvAnimationStartPosition == null ||
                uvAnimationStartPosition.Length != length)
            {
                uvAnimationStartPosition = new int[length];
            }
            if (uvAnimationIndex == null ||
                uvAnimationIndex.Length != length)
            {
                uvAnimationIndex = new int[length];
            }
            if (uvAnimationQuadPoint == null ||
                uvAnimationQuadPoint.Length != length)
            {
                uvAnimationQuadPoint = new int[length];
            }
        }



        public void removeVert(int index)
        {
            if (newUvs != null && newUvs.Length > 0)
            {
                Vector2[] newUvs2 = new Vector2[newUvs.Length - 1];
                Vector2[] uvs2 = new Vector2[uvs.Length - 1];
                bool hasAnimInfo = false;

                int[] uvAnimationLength2 = new int[0];
                int[] uvAnimationStartPosition2 = new int[0];
                int[] uvAnimationIndex2 = new int[0];
                int[] uvAnimationQuadPoint2 = new int[0];
                if (uvAnimationLength != null && uvAnimationLength.Length > 0)
                {
                    hasAnimInfo = true;
                    uvAnimationLength2 = new int[uvAnimationLength.Length - 1];
                    uvAnimationStartPosition2 = new int[uvAnimationStartPosition.Length - 1];
                    uvAnimationIndex2 = new int[uvAnimationIndex.Length - 1];
                    uvAnimationQuadPoint2 = new int[uvAnimationQuadPoint.Length - 1];
                }
                int c = 0;
                for (int i = 0; i < uvs.Length; i++)
                {
                    if (i == index)
                    {
                        i++;
                    }
                    if (i < uvs.Length)
                    {
                        if (hasAnimInfo)
                        {
                            newUvs2[c] = newUvs[i];
                            uvs2[c] = uvs[i];
                            uvAnimationLength2[c] = uvAnimationLength[i];
                            uvAnimationStartPosition2[c] = uvAnimationStartPosition[i];
                            uvAnimationIndex2[c] = uvAnimationIndex[i];
                            uvAnimationQuadPoint2[c] = uvAnimationQuadPoint[i];
                        }
                        c++;
                    }
                }

                newUvs = newUvs2;
                uvs = uvs2;
                uvAnimationLength = uvAnimationLength2;
                uvAnimationStartPosition = uvAnimationStartPosition2;
                uvAnimationIndex = uvAnimationIndex2;
                uvAnimationQuadPoint = uvAnimationQuadPoint2;
                vertCount--;
            }
        }
        public void removeVerts(List<int> indexes)
        {
            if (newUvs != null && newUvs.Length > 0)
            {
                Vector2[] newUvs2 = new Vector2[newUvs.Length - 1];
                Vector2[] uvs2 = new Vector2[uvs.Length - 1];
                bool hasAnimInfo = false;

                int[] uvAnimationLength2 = new int[0];
                int[] uvAnimationStartPosition2 = new int[0];
                int[] uvAnimationIndex2 = new int[0];
                int[] uvAnimationQuadPoint2 = new int[0];
                if (uvAnimationLength != null && uvAnimationLength.Length > 0)
                {
                    hasAnimInfo = true;
                    uvAnimationLength2 = new int[uvAnimationLength.Length - 1];
                    uvAnimationStartPosition2 = new int[uvAnimationStartPosition.Length - 1];
                    uvAnimationIndex2 = new int[uvAnimationIndex.Length - 1];
                    uvAnimationQuadPoint2 = new int[uvAnimationQuadPoint.Length - 1];
                }
                int c = 0;
                for (int i = 0; i < uvs.Length; i++)
                {
                    if (indexes.Contains(i))
                    {
                        continue;
                    }


                    if (hasAnimInfo)
                    {
                        newUvs2[c] = newUvs[i];
                        uvs2[c] = uvs[i];
                        uvAnimationLength2[c] = uvAnimationLength[i];
                        uvAnimationStartPosition2[c] = uvAnimationStartPosition[i];
                        uvAnimationIndex2[c] = uvAnimationIndex[i];
                        uvAnimationQuadPoint2[c] = uvAnimationQuadPoint[i];
                    }
                    c++;

                }

                newUvs = newUvs2;
                uvs = uvs2;
                uvAnimationLength = uvAnimationLength2;
                uvAnimationStartPosition = uvAnimationStartPosition2;
                uvAnimationIndex = uvAnimationIndex2;
                uvAnimationQuadPoint = uvAnimationQuadPoint2;
                vertCount -= indexes.Count;
            }
        }

        public UVData deepCopy()
        {
            UVData newUVData = new UVData(name, texWidth, texHeight, tileWidth, tileHeight, tileOutline, vertCount, defaultUVs);
            newUVData._uvs = new Vector2[vertCount];
            newUVData._newUvs = new Vector2[vertCount];
            newUVData._uvAnimationIndex = new int[vertCount];
            newUVData._uvAnimationQuadPoint = new int[vertCount];
            newUVData._uvAnimationStartPosition = new int[vertCount];
            newUVData._uvAnimationLength = new int[vertCount];

            for (int i = 0; i < _uvs.Length; i++)
            {
                newUVData._uvs[i] = _uvs[i];
                newUVData._newUvs[i] = _newUvs[i];
                newUVData._uvAnimationIndex[i] = _uvAnimationIndex[i];
                newUVData._uvAnimationQuadPoint[i] = _uvAnimationQuadPoint[i];
                newUVData._uvAnimationStartPosition[i] = _uvAnimationStartPosition[i];
                newUVData._uvAnimationLength[i] = _uvAnimationLength[i];
            }

            

            return newUVData;
        }
    }

    public class Triangle
    {
        public Vector3 n;
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public Vector3 center;

        public Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;

            // If the points are very small, they may require scaling to ensure they're not rounded to zero during the cross product process.
            Vector3 bc = (b - a) * 1000;
            Vector3 ab = (c - a) * 1000;

            n = Vector3.Cross(bc, ab);

            n = n.normalized;

            recalculateCenter();
        }

        public void scale(Vector3 s)
        {
            a.x *= s.x;
            a.y *= s.y;
            a.z *= s.z;

            b.x *= s.x;
            b.y *= s.y;
            b.z *= s.z;

            c.x *= s.x;
            c.y *= s.y;
            c.z *= s.z;

            recalculateCenter();
        }

        public void translate(Vector3 t)
        {
            a += t;
            b += t;
            c += t;

            recalculateCenter();
        }

        public void rotate(Vector3 r)
        {
            a = Quaternion.Euler(r) * a;
            b = Quaternion.Euler(r) * b;
            c = Quaternion.Euler(r) * c;
            n = Quaternion.Euler(r) * n;

            recalculateCenter();
        }
        public void rotate(Quaternion r)
        {
            a = r * a;
            b = r * b;
            c = r * c;
            n = r * n;

            recalculateCenter();
        }


        public void rotate(Vector3 r, Vector3 origin)
        {
            translate(-origin);
            a = Quaternion.Euler(r) * a;
            b = Quaternion.Euler(r) * b;
            c = Quaternion.Euler(r) * c;
            n = Quaternion.Euler(r) * n;

            recalculateCenter();

            translate(origin);
        }

        public void recalculateCenter()
        {
            center = (a + b + c) / 3;
        }

        public string ToString(string format = "f4")
        {
            return ("a: " + a.ToString(format) + " b: " + b.ToString(format) + " c: " + c.ToString(format) + " n: " + n.ToString(format));
        }
    }

    [System.Serializable]
    public class ListWrapper
    {
        [SerializeField, HideInInspector]
        public List<int> list;

        public ListWrapper()
        {
            list = new List<int>();
        }

        public ListWrapper(int[] array)
        {
            list = new List<int>(array);
        }

        public ListWrapper(List<int> list)
        {
            this.list = list;
        }

        public void Add(int item)
        {
            list.Add(item);
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }
        public void Remove(int item)
        {
            list.Remove(item);
        }

        public bool Contains(int item)
        {
            return list.Contains(item);
        }

        public int Count
        {
            get
            {
                return list.Count;
            }
        }

        public void Clear()
        {
            list.Clear();
        }

        public ListWrapper deepCopy()
        {
            ListWrapper newList = new ListWrapper();

            for (int i = 0; i < list.Count; i++)
            {
                newList.Add(list[i]);
            }

            return newList;
        }
    }

#if UNITY_EDITOR
    /* A class used to store temporary values used during editing.
     * Used as a go-between for the Mesh Edit window and the custom editor
     */
     [System.Serializable]
    public class EditorInfo
    {
        public enum MeshEditOperation { Standard, SelectCircle, LoopCut }

        public MeshEditOperation editOperation = MeshEditOperation.Standard;
        // Mesh Edit

        [NonSerialized]
        public int transformMode = 0;
        [SerializeField]
        public bool showNormalControls;
        [SerializeField]
        public bool showAddPrimitiveControls;
        [SerializeField]
        public bool showGeneralOperations;
        [SerializeField]
        public bool showUVEditor;
        // Tile Edit

        [System.Serializable]
        public class CustomTilesetEditorSettings
        {
            [SerializeField, HideInInspector]
            public int tileWidth = 32;
            [SerializeField, HideInInspector]
            public int tileHeight = 32;
            [SerializeField, HideInInspector]
            public int tileOutline;

            public CustomTilesetEditorSettings()
            {
                tileWidth = 32;
                tileHeight = 32;
                tileOutline = 1;
            }
        }

        [SerializeField, HideInInspector]
        public List<CustomTilesetEditorSettings> customTilesetSettings;
        [NonSerialized]
        public int tileDirection = 0;
        [SerializeField, HideInInspector]
        public int tilesPerRow = 0;
        [SerializeField, HideInInspector]
        public int tilesPerColumn = 0;
        [SerializeField, HideInInspector]
        public int tileWidth = 32;
        [SerializeField, HideInInspector]
        public int tileHeight = 32;
        [SerializeField, HideInInspector]
        public int tileOutline;
        [SerializeField, HideInInspector]
        public int tileCount;
        [SerializeField]
        public int selectedTile = 0;
        [NonSerialized]
        public List<Vector2> uvCoordSnapped = new List<Vector2>(new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f) });
        [SerializeField, HideInInspector]
        public Texture texturePage;
        [SerializeField, HideInInspector]
        public bool showUVEditorSettings;

        [SerializeField, HideInInspector]
        public int uvMapExportTextureWidth = 512;
        [SerializeField, HideInInspector]
        public int uvMapExportTextureHeight = 512;
        [SerializeField, HideInInspector]
        public float uvMapUnwrapIslandPadding = 0.1f;

        // Vertex Paint

        [SerializeField, HideInInspector]
        public Color paintColour = Color.white;
        [SerializeField, HideInInspector]
        public List<Color> colourHistory;
        [NonSerialized]
        public int maxColours = 20;
        [SerializeField, HideInInspector]
        public bool showMaterialPicker;
    }

    public void setVertMode(int newVertMode)
    {
        vertMode = newVertMode;

        if (vertMode == 0)
        {
            // To verts
            selectionConvertToVerts();
            selectionAddTouchingVerts();

            selectedFaces = new bool[selectedFaces.Length];
        }
        else if (vertMode == 1)
        {
            // To faces
            selectionConvertToFaces();
            selectedVerts = new bool[selectedVerts.Length];
        }
    }

    private void selectionAddTouchingVerts()
    {

        for (int i = 0; i < selectedVerts.Length; i++)
        {
            if (selectedVerts[i])
            {
                for (int j = 0; j < connectedVerts[i].Count; j++)
                {
                    selectedVerts[connectedVerts[i].list[j]] = true;
                }
            }
        }
    }

    [SerializeField, HideInInspector]
    public EditorInfo editorInfo = new EditorInfo();
#endif
    // UV Information
    [SerializeField, HideInInspector]
    public List<UVData> _uvMaps;
    public List<UVData> uvMaps
    {
        get
        {
            if (_uvMaps == null)
            {
                _uvMaps = new List<UVData>();
            }
            return _uvMaps;
        }
        set
        {
            _uvMaps = value;
        }
    }

    [SerializeField, HideInInspector]
    public List<Vector2> customTextureUVMap;

    [SerializeField, HideInInspector]
    public bool hasDefaultUVs;
    [SerializeField, HideInInspector]
    public Vector2[] defaultUVs;
    
    [SerializeField, HideInInspector]
    public int selectedTileset = - 1;

    [SerializeField, HideInInspector]
    public bool isTilesetRefreshRequired;

    // Geometry Information
    [SerializeField, HideInInspector]
    public Mesh _mesh;
    [SerializeField, HideInInspector]
    public Mesh mesh
    {
        get
        {
#if UNITY_EDITOR
            if (_mesh == null)
            {
                if (
                verts != null &&
                tris != null &&
                colours != null &&
                faceNormals != null)
                {
                    pushNewGeometry();
                }
                else
                {
                    Debug.Log("Error! MeshEdit object has insufficient data to construct a mesh.");

                }
            }
#endif
            return _mesh;
        }
        set
        {
            _mesh = value;
        }
    }
    [SerializeField, HideInInspector]
    public Mesh meshModified;
    [SerializeField, HideInInspector]
    public List<Vector3> verts;
    [SerializeField, HideInInspector]
    public List<Color> colours;
    [SerializeField, HideInInspector]
    public List<Vector3> faceNormals;
    [SerializeField, HideInInspector]
    public List<Vector3> vertNormals;
    [SerializeField, HideInInspector]
    public List<Vector3> meshNormals;
    // 0.4
    [SerializeField, HideInInspector]
    public float smoothnessThreshold;
    [SerializeField, HideInInspector]
    public List<int> tris;
    [SerializeField, HideInInspector]
    public List<int> quads;
    [SerializeField, HideInInspector]
    public List<int> quadMaterial;
    [SerializeField, HideInInspector]
    public List<Material> materials;
    [SerializeField, HideInInspector]
    public int selectedMaterial;
    [SerializeField, HideInInspector]
    public List<int> materialUVMap;
    [SerializeField, HideInInspector]
    public Texture selectedTexture;
    [SerializeField, HideInInspector]
    public List<ListWrapper> connectedVerts;
    [SerializeField, HideInInspector]
    public string lastPrefabPath = "";
    
    [HideInInspector]
    public const float minimumScale = 0.00001f;

    public static int[] quadEdgePatternClockwise = { 0, 1, 1, 3, 3, 2, 2, 0 };

    [SerializeField, HideInInspector]
    public List<ListWrapper> _adjacentFaces;
    public List<ListWrapper> adjacentFaces
    {
        get
        {
            // DO NOT DELETE THIS
            // This is necessary to run MeshEdit on any models created before v0.3
            if (_adjacentFaces == null)
            {
                rebuildAdjacentFaces();
            }

            return _adjacentFaces;
        }
    }

    public void rebuildAdjacentFaces(int skippedFaces = 0)
    {
        if (skippedFaces == 0)
        {
            _adjacentFaces = new List<ListWrapper>();
        }

        for (int i = skippedFaces * 4; i < quads.Count; i += 4)
        {
            // Potentially adjacent faces exist connected to the vert-pairs 0-1, 1-3, 2-0 and 3-0
            // TODO: FIX the dang face system so that quads are represented by clockwise verts, NOT this silly quad-only system. You'll have to do it eventually!
            ListWrapper adjacentToFace = new ListWrapper();

            // This loop will add four values to the new list, going clockwise around a default face
            for (int c = 0; c < MeshEdit.quadEdgePatternClockwise.Length; c += 2)
            {
                int adjacentFace = -1;

                int quadIndex0 = i + MeshEdit.quadEdgePatternClockwise[c + 0];
                int vertIndex0 = quads[quadIndex0];
                //Debug.Log("RECALC ADJACENT: " + vertIndex0 + "/" + connectedVerts.Count + " QC: " + quads.Count);

                for (int j = 0; j < connectedVerts[vertIndex0].Count; j++)
                {
                    for (int jj = 0; jj < connectedVerts[quads[i + MeshEdit.quadEdgePatternClockwise[c + 1]]].Count; jj++)
                    {
                        int connectedVert0 = connectedVerts[quads[i + MeshEdit.quadEdgePatternClockwise[c + 0]]].list[j];
                        int connectedVert1 = connectedVerts[quads[i + MeshEdit.quadEdgePatternClockwise[c + 1]]].list[jj];

                        int connectedFace0 = quads.IndexOf(connectedVert0) / 4;
                        int connectedFace1 = quads.IndexOf(connectedVert1) / 4;

                        if (connectedFace0 == connectedFace1)
                        {
                            // Success! An adjacent face has been found
                            adjacentFace = connectedFace0;
                        }
                    }
                }
                adjacentToFace.Add(adjacentFace);
            }

            _adjacentFaces.Add(adjacentToFace);
        }
    }

    public bool areFacesAdjacent(int faceA, int faceB)
    {
        return (
            adjacentFaces[faceA].list.Contains(faceB) &&
            adjacentFaces[faceB].list.Contains(faceA));
    }

    public List<int> getSharedVerts(int faceA, int faceB)
    {
        if (faceA == -1 || faceB == -1)
        {
            return null;
        }

        List<int> sharedVerts = new List<int>();

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int vi = quads[faceA * 4 + i];
                int vj = quads[faceB * 4 + j];

                if (isShared(vi, vj))
                {
                    sharedVerts.Add(vi);
                    sharedVerts.Add(vj);
                }
            }
        }

        return sharedVerts;
    }

    public int getFaceFromVert(int vert)
    {
        return (quads.IndexOf(vert) / 4);
    }

    public int getSideOfFaceBThatContinuesSideFromFaceA(int faceA, int sideOfFaceA, int faceB)
    {
        int vertA = quads[faceA * 4 + quadEdgePatternClockwise[sideOfFaceA * 2 + 0]];
        int vertB = quads[faceA * 4 + quadEdgePatternClockwise[sideOfFaceA * 2 + 1]];

        int sharedSide = adjacentFaces[faceB].list.IndexOf(faceA);
        // Search for an edge on face B that shares ONLY ONE of these two verts, while not containing the same two verts between the faces
        for (int i = 0; i < 4; i++)
        {
            if (i != sharedSide)
            {
                int c = 0;

                int vA = quads[faceB * 4 + quadEdgePatternClockwise[i * 2 + 0]];
                int vB = quads[faceB * 4 + quadEdgePatternClockwise[i * 2 + 1]];

                if (isShared(vertA, vA) || isShared(vertA, vB))
                {
                    c++;
                }
                if (isShared(vertB, vA) || isShared(vertB, vB))
                {
                    c++;
                }

                if (c == 1)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    // Editor Information
    [SerializeField, HideInInspector]
    public int editMode = 0;

    private bool isSelected = false;

    public enum DrawMode { GameView, WireFrame, WireFrameOpaque, Texture, VertexColour }
    [HideInInspector]
    DrawMode drawMode = DrawMode.GameView;

    [SerializeField, HideInInspector]
    public int vertMode;
    [SerializeField, HideInInspector]
    public bool[] selectedVerts;
    [SerializeField, HideInInspector]
    public bool[] selectedFaces;
    [SerializeField, HideInInspector]
    public bool[] selectedUVs;
    [SerializeField, HideInInspector]
    public bool[] selectedUVFaces;
    [SerializeField, HideInInspector]
    public bool isMeshTransparent;

    [HideInInspector]
    public float cutCount;
    [HideInInspector]
    public List<int> facesCut;
    [HideInInspector]
    public List<int> cutsAB;

    public Vector3 quadCenter(int quad)
    {
        return (
            verts[quads[quad * 4 + 0]] +
            verts[quads[quad * 4 + 1]] +
            verts[quads[quad * 4 + 2]] +
            verts[quads[quad * 4 + 3]]) / 4;
    }
    

#if UNITY_EDITOR
    [SerializeField, HideInInspector]
    private void onGameObjectPaste()
    {
        // This method should be called when a meshEdit object is pasted inside the scene, so that it can be duplicated
        // Saved prefabs that are instantiated multiple times to not have the same problem with paired meshes, so this function could be used for both prefab instantiation and copy/paste?
    }




    public void setVertMode(int vertMode, bool[] selectedVerts, bool[] selectedFaces)
    {
        this.vertMode = vertMode;
        if (vertMode == 0)
        {
            this.selectedVerts = selectedVerts;
        }
        else if (vertMode == 1)
        {
            this.selectedFaces = selectedFaces;
        }
    }
    
    [SerializeField, HideInInspector]
    private Material _material;
    private Material material
    {
        get
        {
            if (_material == null)
            {
                _material = new Material(Shader.Find("Particles/Standard Unlit"));
            }
            return _material;
        }
        set
        {
            _material = value;
        }
    }

    
    void Update()
    {
        if (updateSelectedStates())
        {
            isSelected = true;

            if (editMode == 0)
            {
                checkTransforms();
            }
        }

        if (_mesh != null &&
            uvMaps != null &&
            materialUVMap != null &&
            materialUVMap.Count > 0 &&
            selectedMaterial < materialUVMap.Count &&
            materialUVMap[selectedMaterial] >= 0 &&
            materialUVMap[selectedMaterial] < uvMaps.Count)
        {
            uvMaps[materialUVMap[selectedMaterial]].update(_mesh);
        }


    }


    public void setToEditorPosition()
    {
        editorPosition = transform.position;
        editorScale = transform.localScale;
        editorRotation = transform.rotation;
    }


    public bool updateSelectedStates()
    {
        bool isSelected = !(Selection.activeTransform == null ||
            Selection.activeTransform.gameObject != gameObject);

        if (Application.isEditor && !Application.isPlaying)
        {
            if (!isSelected)
            {
                this.isSelected = false;

                vertMode = -1;
                selectedFaces = null;
                selectedVerts = null;

                cutsAB = null;
                cutCount = 0;
                facesCut = null;

                editMode = 0;
                checkTransforms();

                isMeshTransparent = false;

            }
            GetComponent<MeshRenderer>().enabled = !isMeshTransparent || editMode == 0 || editMode == 2;
        }
        return isSelected;
    }


    public void setDrawMode(DrawMode drawMode)
    {
        if (this.drawMode != drawMode)
        {
            this.drawMode = drawMode;

            if (drawMode == DrawMode.GameView)
            {
                GetComponent<MeshRenderer>().enabled = true;
            }
            else if (drawMode == DrawMode.WireFrame)
            {
                GetComponent<MeshRenderer>().enabled = false;
            }

        }
    }

    [SerializeField, HideInInspector]
    public int drawNormals = 0;
    [SerializeField, HideInInspector]
    public int paintMode = 0;
    [SerializeField, HideInInspector]
    public float normalLength = 0.5f;
    Color orange = new Color(0.9f, 0.25f, 0.08f);

    public void OnDrawGizmos()
    {
        if (updateSelectedStates())
        {
            if (quads != null && quads.Count > 0)
            {
                //drawMeshWithGizmos();

                if (cutsAB != null)
                {
                    createLineMaterial();
                    createLineMaterial2D();
                    // Apply the line material
                    lineMaterial.SetPass(0);
                    

                    //GL.Begin(GL.LINES);
                    GL.Color(new Color(1.0f, 0.0f, 0.0f, 0.5f));
                    float cuts = Mathf.Floor(cutCount);
                    Vector3 a;
                    Vector3 b;
                    /*
                    for (int i = 0; i < facesCut.Count; i++)
                    {
                        for (int ii = 0; ii < cuts; ii++)
                        {
                            float sep = 1.0f / (cuts + 1.0f);
                            float ratio = sep * (ii + 1);

                            a = verts[cutsAB[i * 4 + 0]] + (verts[cutsAB[i * 4 + 1]] - verts[cutsAB[i * 4 + 0]]) * ratio;
                            b = verts[cutsAB[i * 4 + 2]] + (verts[cutsAB[i * 4 + 3]] - verts[cutsAB[i * 4 + 2]]) * ratio;

                            GL.Vertex3(a.x, a.y, a.z);
                            GL.Vertex3(b.x, b.y, b.z);
                        }

                    }
                    if (tracker != null)
                    {
                        for (int i = 0; i < tracker.Count; i += 2)
                        {
                            GL.Color(new Color(0.0f, 0.0f, 1.0f, 1.0f));
                            GL.Vertex3(tracker[i].x, tracker[i].y, tracker[i].z);
                            GL.Color(new Color(0.0f, 1.0f, 0.0f, 1.0f));
                            GL.Vertex3(tracker[i + 1].x, tracker[i + 1].y, tracker[i + 1].z);
                        }
                    }
                    GL.End();
                    */
                    GL.PushMatrix();
                    GL.LoadPixelMatrix();

                    lineMaterial2D.SetPass(0);

                    for (int i = 0; i < facesCut.Count; i++)
                    {
                        for (int ii = 0; ii < cuts; ii++)
                        {
                            float sep = 1.0f / (cuts + 1.0f);
                            float ratio = sep * (ii + 1);

                            a = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(verts[cutsAB[i * 4 + 0]] + (verts[cutsAB[i * 4 + 1]] - verts[cutsAB[i * 4 + 0]]) * ratio);
                            b = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(verts[cutsAB[i * 4 + 2]] + (verts[cutsAB[i * 4 + 3]] - verts[cutsAB[i * 4 + 2]]) * ratio);

                            glDrawLine(a, b, 2.0f, Color.red, Color.red);
                        }

                    }
                    GL.PopMatrix();


                    Gizmos.color = Color.red;
                    /*
                    float cuts = Mathf.Floor(cutCount);
                    for (int i = 0; i < facesCut.Count; i++)
                    {
                        for (int ii = 0; ii < cuts; ii++)
                        {
                            float sep = 1.0f / (cuts + 1.0f);
                            float ratio = sep * (ii + 1);

                            Gizmos.DrawLine(
                                cutsAB[i * 4 + 0] + (cutsAB[i * 4 + 1] - cutsAB[i * 4 + 0]) * ratio,
                                cutsAB[i * 4 + 2] + (cutsAB[i * 4 + 3] - cutsAB[i * 4 + 2]) * ratio);
                        }
                    }
                    */
                }
                Gizmos.color = Color.white;
            }

        }


        if (Selection.activeGameObject == gameObject)
        {
            if (debugMode == DebugShowInfo.ConnectedVerts)
            {
                for (int i = 0; i < verts.Count; i++)
                {
                    int q = quads.IndexOf(i) / 4;
                    Vector3 qOffset = quadCenter(q) - verts[i];
                    Handles.color = Color.white;
                    string list = i + " {";

                    for (int ii = 0; ii < connectedVerts[i].Count; ii++)
                    {
                        list += connectedVerts[i].list[ii].ToString();
                        if (ii < connectedVerts[i].Count - 1)
                        {
                            list += ", ";
                        }
                    }
                    list += "}";

                    Handles.Label(verts[i] + qOffset * 0.25f, list);
                }
            }
            if (debugMode == DebugShowInfo.AdjacentFaces)
            {
                for (int i = 0; i < adjacentFaces.Count; i++)
                {
                    if (selectedFaces[i])
                    {

                        Handles.color = Color.white;

                        Handles.color = Color.blue * 1.2f;
                        Handles.Label(quadCenter(i), i.ToString());
                        Vector3 c = quadCenter(i);
                        for (int ii = 0; ii < MeshEdit.quadEdgePatternClockwise.Length; ii += 2)
                        {
                            Handles.Label(
                                 ((verts[quads[i * 4 + MeshEdit.quadEdgePatternClockwise[ii + 0]]] + verts[quads[i * 4 + MeshEdit.quadEdgePatternClockwise[ii + 1]]] + c) / 3),
                                (ii / 2).ToString() + "{" + adjacentFaces[i].list[ii / 2] + "}");
                        }
                    }
                }
            }
            else if (debugMode == DebugShowInfo.HalfEdges)
            {
                for (int i = 0; i < adjacentFaces.Count; i++)
                {
                    Handles.color = Color.white;

                    Handles.Label(quadCenter(i), i.ToString());
                    Vector3 c = quadCenter(i);
                    for (int ii = 0; ii < MeshEdit.quadEdgePatternClockwise.Length; ii += 2)
                    {
                        string str = "";

                        if (halfEdges[i].edges[ii / 2].isSeam &&
                            halfEdges[i].edges[ii / 2].isSharp)
                        {
                            str += "Seam, Shrp";
                        }
                        else if (halfEdges[i].edges[ii / 2].isSeam)
                        {
                            str += "Seam";
                        }
                        else if (halfEdges[i].edges[ii / 2].isSharp)
                        {
                            str += "Shrp";
                        }

                        Handles.Label(
                             ((verts[quads[i * 4 + MeshEdit.quadEdgePatternClockwise[ii + 0]]] + verts[quads[i * 4 + MeshEdit.quadEdgePatternClockwise[ii + 1]]] + c) / 3),
                            (ii / 2).ToString() + "{" + str + "}");
                    }
                }
            }
            else if (debugMode == DebugShowInfo.FaceMaterial)
            {
                for (int i = 0; i < quadMaterial.Count; i++)
                {
                    Handles.color = Color.white;

                    Handles.Label(quadCenter(i), i + "{" + quadMaterial[i] + "}");
                }
            }
            else if (debugMode == DebugShowInfo.FaceTriangles)
            {
                for (int i = 0; i < adjacentFaces.Count; i++)
                {
                    if (Vector3.Dot(SceneView.lastActiveSceneView.camera.transform.forward, faceNormals[i]) < 0)
                    {

                        Handles.color = Color.white;

                        Handles.color = Color.blue * 1.2f;
                        Handles.Label(quadCenter(i), i.ToString());
                        Vector3 c = quadCenter(i);
                        for (int ii = 0; ii < 4; ii++)
                        {
                            Handles.Label(
                                 ((verts[quads[i * 4 + ii]] * 4 + c) / 5),
                                ii + "{" + quads[i * 4 + ii] + "}");
                        }
                        Vector3 tCenter = (verts[tris[i * 6 + 0]] + verts[tris[i * 6 + 1]] + verts[tris[i * 6 + 2]]) / 3;

                        Handles.Label(
                             tCenter,
                            "t0");
                        Handles.Label(
                             ((verts[tris[i * 6 + 0]] + tCenter) / 2),
                            "a{" + tris[i * 6 + 0] + "}");
                        Handles.Label(
                             ((verts[tris[i * 6 + 1]] + tCenter) / 2),
                            "b{" + tris[i * 6 + 1] + "}");
                        Handles.Label(
                             ((verts[tris[i * 6 + 2]] + tCenter) / 2),
                            "c{" + tris[i * 6 + 2] + "}");
                        tCenter = (verts[tris[i * 6 + 3]] + verts[tris[i * 6 + 4]] + verts[tris[i * 6 + 5]]) / 3;
                        Handles.Label(
                             tCenter,
                            "t1");
                        Handles.Label(
                             ((verts[tris[i * 6 + 3]] + tCenter) / 2),
                            "a{" + tris[i * 6 + 3] + "}");
                        Handles.Label(
                             ((verts[tris[i * 6 + 4]] + tCenter) / 2),
                            "b{" + tris[i * 6 + 4] + "}");
                        Handles.Label(
                             ((verts[tris[i * 6 + 5]] + tCenter) / 2),
                            "c{" + tris[i * 6 + 5] + "}");

                    }
                }
            }
        }

        #region GL Drawing
        if (isSelected)
        {
            createLineMaterial();
            // Apply the line material
            lineMaterial.SetPass(0);

            GL.PushMatrix();
            // Set transformation matrix for drawing to
            // match our transform
            GL.MultMatrix(Matrix4x4.identity);

            if (editMode == 1)
            {
                if (selectedFaces != null && vertMode == 1)
                {
                    // Draw lines
                    int index = 0;
                    for (int i = 0; i < selectedFaces.Length; i++)
                    {
                        GL.Begin(GL.TRIANGLE_STRIP);
                        GL.Color(new Color(0.95f, 0.95f, 0.95f, 0.6f));
                        if (selectedFaces[i])
                        {
                            index = i * 4;
                            GL.Vertex3(verts[quads[index + 0]].x, verts[quads[index + 0]].y, verts[quads[index + 0]].z);
                            GL.Vertex3(verts[quads[index + 1]].x, verts[quads[index + 1]].y, verts[quads[index + 1]].z);
                            GL.Vertex3(verts[quads[index + 2]].x, verts[quads[index + 2]].y, verts[quads[index + 2]].z);
                            GL.Vertex3(verts[quads[index + 3]].x, verts[quads[index + 3]].y, verts[quads[index + 3]].z);
                        }
                        GL.End();
                    }
                }
                else if (selectedVerts != null && vertMode == 0)
                {
                    for (int i = 0; i < selectedVerts.Length; i += 4)
                    {
                        if (selectedVerts[i + 0] &&
                            selectedVerts[i + 1] &&
                            selectedVerts[i + 2] &&
                            selectedVerts[i + 3])
                        {
                            GL.Begin(GL.TRIANGLE_STRIP);
                            GL.Color(new Color(0.95f, 0.95f, 0.95f, 0.6f));

                            GL.Vertex3(verts[i + 0].x, verts[i + 0].y, verts[i + 0].z);
                            GL.Vertex3(verts[i + 1].x, verts[i + 1].y, verts[i + 1].z);
                            GL.Vertex3(verts[i + 2].x, verts[i + 2].y, verts[i + 2].z);
                            GL.Vertex3(verts[i + 3].x, verts[i + 3].y, verts[i + 3].z);

                            GL.End();
                        }
                    }
                }
            }
            GL.PopMatrix();
        }
        #endregion
    }

    public void glDrawLine(Vector2 a, Vector2 b, float lineWeight, Color colourA, Color colourB)
    {
        GL.Begin(GL.TRIANGLE_STRIP);

        GL.Color(colourA);

        Vector2 t = (a - b).normalized * (lineWeight / 2);

        float stepSize = Mathf.PI / 10;
        float piOver2 = Mathf.PI / 2;

        GL.Vertex3(a.x + t.x, a.y + t.y, 0.0f);

        // Capsule cap a
        for (float r = stepSize; r < piOver2; r += stepSize)
        {
            float cos = Mathf.Cos(r);
            float sin = Mathf.Sin(r);
            float cos2 = Mathf.Cos(-r);
            float sin2 = Mathf.Sin(-r);

            GL.Vertex3(a.x + (cos * t.x - sin * t.y) , a.y + sin * t.x + cos * t.y, 0);
            GL.Vertex3(a.x + (cos2 * t.x - sin2 * t.y) , a.y + sin2 * t.x + cos2 * t.y, 0);
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

            GL.Vertex3(b.x + (cos2 * t.x - sin2 * t.y) , b.y + sin2 * t.x + cos2 * t.y, 0.0f);
            GL.Vertex3(b.x + (cos * t.x - sin * t.y) , b.y + sin * t.x + cos * t.y, 0.0f);
        }



        GL.End();
    }

    public void drawMeshWithGizmos()
    {

        Gizmos.color = Color.black;
        float r = 1.5f;
        float d;
        Vector3 spR, spL;
        //Debug.Log("Time since last frame: " + Time.deltaTime);

        for (int i = 0; i < quads.Count; i += 4)
        {
            if (editMode == 1 && vertMode == 1 && selectedFaces != null)
            {
                if (selectedFaces[i / 4])
                {
                    Gizmos.color = Color.white;
                }
                else
                {
                    Gizmos.color = Color.black;
                }
                Vector3 q = (
                    verts[quads[i + 0]] +
                    verts[quads[i + 1]] +
                    verts[quads[i + 2]] +
                    verts[quads[i + 3]]) * 0.25f;

                d = Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, q);
                spR = HandleUtility.GUIPointToWorldRay(verts[i] + new Vector3(r, 0, 0)).GetPoint(d);
                spL = HandleUtility.GUIPointToWorldRay(verts[i] + new Vector3(-r, 0, 0)).GetPoint(d);
                //Graphics.DrawMesh(cube, q, Quaternion.identity, material, 0);
                Gizmos.DrawCube(q, Vector3.one * Vector3.Distance(spR, spL));
            }
            if (editMode == 3 && paintMode == 1)
            {
                Vector3 q = (
                    verts[quads[i + 0]] +
                    verts[quads[i + 1]] +
                    verts[quads[i + 2]] +
                    verts[quads[i + 3]]) * 0.25f;

                d = Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, q);
                spR = HandleUtility.GUIPointToWorldRay(verts[i] + new Vector3(r, 0, 0)).GetPoint(d);
                spL = HandleUtility.GUIPointToWorldRay(verts[i] + new Vector3(-r, 0, 0)).GetPoint(d);

                Gizmos.color = (colours[i + 0] + colours[i + 1] + colours[i + 2] + colours[i + 3]) / 4.0f;
                Gizmos.DrawCube(q, Vector3.one * Vector3.Distance(spR, spL));
            }
            Gizmos.color = Color.black;

            Gizmos.DrawLine(verts[quads[i + 2]], verts[quads[i + 0]]);
            Gizmos.DrawLine(verts[quads[i + 0]], verts[quads[i + 1]]);
            Gizmos.DrawLine(verts[quads[i + 3]], verts[quads[i + 1]]);
            Gizmos.DrawLine(verts[quads[i + 3]], verts[quads[i + 2]]);
            if (debugMode == DebugShowInfo.FaceTriangles)
            {
                Gizmos.DrawLine(verts[quads[i + 1]], verts[quads[i + 2]]);
            }
        }

        if ((vertMode == 0 && selectedVerts != null) || (editMode == 3 && paintMode == 0))
        {
            for (int i = 0; i < verts.Count; i++)
            {
                if (selectedVerts != null && selectedVerts.Length > i && selectedVerts[i])
                {
                    Gizmos.color = Color.white;
                }
                else
                {
                    Gizmos.color = Color.black;
                }
                d = Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, verts[i]);
                spR = HandleUtility.GUIPointToWorldRay(verts[i] + new Vector3(r, 0, 0)).GetPoint(d);
                spL = HandleUtility.GUIPointToWorldRay(verts[i] + new Vector3(-r, 0, 0)).GetPoint(d);
                //Graphics.DrawMesh(cube, verts[i], Quaternion.identity, material, 0);

                if (editMode == 3)
                {
                    Gizmos.color = colours[i];
                }
                Gizmos.DrawCube(verts[i], Vector3.one * Vector3.Distance(spR, spL));
            }

        }
        if (drawNormals > 0)// && editorInfo.transformMode == 0)
        {
            if (drawNormals == 1)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < quads.Count; i += 4)
                {
                    Vector3 c = (verts[quads[i + 0]] + verts[quads[i + 1]] + verts[quads[i + 2]] + verts[quads[i + 3]]) / 4.0f;

                    Gizmos.DrawLine(c, c + faceNormals[i / 4] * normalLength);
                }
            }
            else if (drawNormals == 2)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < vertNormals.Count; i++)
                {
                    Gizmos.DrawLine(verts[i], verts[i] + vertNormals[i].normalized * normalLength);
                }
            }
            else if (drawNormals == 3)
            {

                Gizmos.color = Color.magenta;

                for (int i = 0; i < quads.Count; i += 4)
                {
                    int tA = (i / 4) * 2 * 3;
                    int tB = ((i / 4) * 2 + 1) * 3;
                    Vector3 ab = verts[tris[tA + 1]] - verts[tris[tA + 0]];
                    Vector3 ac = verts[tris[tA + 2]] - verts[tris[tA + 0]];
                    Vector3 dc = verts[tris[tB + 1]] - verts[tris[tB + 0]];
                    Vector3 db = verts[tris[tB + 2]] - verts[tris[tB + 0]];

                    Vector3 a = Vector3.Cross(ab, ac);
                    Vector3 b = Vector3.Cross(dc, db);
                    Vector3 c = (verts[tris[tA + 0]] + verts[tris[tA + 1]] + verts[tris[tA + 2]]) / 3.0f;
                    Gizmos.DrawLine(c, c + a.normalized * normalLength);
                    c = (verts[tris[tB + 0]] + verts[tris[tB + 1]] + verts[tris[tB + 2]]) / 3.0f;
                    Gizmos.DrawLine(c, c + b.normalized * normalLength);
                }
            }
        }
    }

    bool[] doYouDrawThatEdge; // hey wouldn't it be cool if you could add questionmarks to variable names?
    bool[] doYouDrawThatVert; // yeah probably not
    public float tempPaintCircleRadius = 1.0f;
    public void drawMeshWithGL()
    {
        createLineMaterial();

        lineMaterial.SetPass(0);
       // lineMaterial.

        bool selectedVertsDefined = (selectedVerts != null && selectedVerts.Length == verts.Count);
        bool selectedFacesDefined = (selectedFaces != null && selectedFaces.Length == quads.Count / 4);

        

        float glExtrude = 0.002f;
        float r = 1f;
        float d;

        Vector3 spR, spL;

        Vector3 right = SceneView.lastActiveSceneView.camera.transform.right;
        Vector3 up = SceneView.lastActiveSceneView.camera.transform.up;

        GL.PushMatrix();
        
        // if (doYouDrawThatEdge == null || doYouDrawThatEdge.Length != quads.Count)
        {
            doYouDrawThatEdge = new bool[quads.Count];
            doYouDrawThatVert = new bool[verts.Count];
            for (int i = 0; i < doYouDrawThatEdge.Length; i++)
            {
                doYouDrawThatEdge[i] = true;
                doYouDrawThatVert[i] = true;
            }


        }

        // Edges with a property
        int f = 0;
        if (adjacentFaces != null && 
            halfEdges != null && 
            halfEdges.Count == adjacentFaces.Count)
        {
            GL.Begin(GL.QUADS);
            GL.Color(Color.black);
            for (int i = 0; i < quads.Count; i += 4)
            {
                if (halfEdges[f].edges != null && halfEdges[f].edges.Length == 4)
                {
                    for (int e = 0; e < 4; e++)
                    {
                        if (doYouDrawThatEdge[i + e])
                        {
                            int oppositeFace = adjacentFaces[f].list[e];

                            if (oppositeFace > -1)
                            {
                                int oppositeSide = adjacentFaces[oppositeFace].list.IndexOf(f);

                                if (oppositeSide > -1)
                                {
                                    doYouDrawThatEdge[oppositeFace * 4 + oppositeSide] = false;
                                }

                            }

                            int v0Index = quads[i + quadEdgePatternClockwise[e * 2 + 0]];
                            int v1Index = quads[i + quadEdgePatternClockwise[e * 2 + 1]];
                            Vector3 v0 = verts[v0Index] + vertNormals[v0Index] * glExtrude * 0.5f;
                            Vector3 v1 = verts[v1Index] + vertNormals[v1Index] * glExtrude * 0.5f;


                            if (halfEdges[f].edges[e].isSharp || halfEdges[f].edges[e].isSeam)
                            {

                                Vector2 v02D = HandleUtility.WorldToGUIPoint(v0);
                                Vector2 v12D = HandleUtility.WorldToGUIPoint(v1);
                                Vector2 t = v12D - v02D;
                                Vector2 n = new Vector2(t.y, t.x).normalized;

                                float d0 = Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, v0);
                                float d1 = Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, v1);

                                spR = HandleUtility.GUIPointToWorldRay(v0 + new Vector3(r * 1f, 0, 0)).GetPoint(d0);
                                spL = HandleUtility.GUIPointToWorldRay(v0 + new Vector3(-r * 1f, 0, 0)).GetPoint(d0);
                                float pixelWidth0 = Vector3.Distance(spR, spL);
                                spR = HandleUtility.GUIPointToWorldRay(v1 + new Vector3(r * 1f, 0, 0)).GetPoint(d1);
                                spL = HandleUtility.GUIPointToWorldRay(v1 + new Vector3(-r * 1f, 0, 0)).GetPoint(d1);
                                float pixelWidth1 = Vector3.Distance(spR, spL);
                                //Graphics.DrawMesh(cube, q, Quaternion.identity, material, 0);

                                right = SceneView.lastActiveSceneView.camera.transform.right * pixelWidth0 * n.x;
                                up = SceneView.lastActiveSceneView.camera.transform.up * pixelWidth0 * n.y;

                                GL.Vertex3(
                                    v0.x + right.x + up.x,
                                    v0.y + right.y + up.y,
                                    v0.z + right.z + up.z);

                                GL.Vertex3(
                                    v0.x - right.x - up.x,
                                    v0.y - right.y - up.y,
                                    v0.z - right.z - up.z);

                                right = SceneView.lastActiveSceneView.camera.transform.right * pixelWidth1 * n.x;
                                up = SceneView.lastActiveSceneView.camera.transform.up * pixelWidth1 * n.y;

                                GL.Vertex3(
                                    v1.x - right.x - up.x,
                                    v1.y - right.y - up.y,
                                    v1.z - right.z - up.z);

                                GL.Vertex3(
                                    v1.x + right.x + up.x,
                                    v1.y + right.y + up.y,
                                    v1.z + right.z + up.z);
                            }
                        }
                    }
                }
                f++;
            }
            GL.End();
        }
        // Edges
        
        GL.Begin(GL.LINES);
        f = 0;

        if (adjacentFaces != null &&
            halfEdges != null && 
            halfEdges.Count == adjacentFaces.Count)
        {
            for (int i = 0; i < quads.Count; i += 4)
            {
                if (halfEdges[f].edges != null && halfEdges[f].edges.Length == 4)
                {
                    for (int e = 0; e < 4; e++)
                    {
                        if (doYouDrawThatEdge[i + e])
                        {
                            Color neutralColour = Color.black;

                            if (halfEdges[f].edges[e].isSharp &&
                                halfEdges[f].edges[e].isSeam)
                            {
                                neutralColour = Color.gray;
                            }
                            else if (halfEdges[f].edges[e].isSharp)
                            {
                                neutralColour = Color.red;
                            }
                            else if (halfEdges[f].edges[e].isSeam)
                            {
                                neutralColour = Color.cyan;
                            }
                            else
                            {
                                neutralColour = Color.black;
                            }
                            int v0 = quads[i + quadEdgePatternClockwise[e * 2 + 0]];
                            int v1 = quads[i + quadEdgePatternClockwise[e * 2 + 1]];

                            if (vertMode == 0 && selectedVertsDefined && selectedVerts[v0])
                            {
                                GL.Color(Color.white);
                            }
                            else
                            {
                                GL.Color(neutralColour);
                            }


                            Vector3 v = verts[v0] + vertNormals[v0] * glExtrude;


                            GL.Vertex3(v.x, v.y, v.z);


                            if (vertMode == 0 && selectedVertsDefined && selectedVerts[v1])
                            {
                                GL.Color(Color.white);
                            }
                            else
                            {
                                GL.Color(neutralColour);
                            }

                            v = verts[v1] + vertNormals[v1] * glExtrude;

                            GL.Vertex3(v.x, v.y, v.z);
                        }
                    }
                }
                f++;

                if (debugMode == DebugShowInfo.FaceTriangles)
                {
                    GL.Color(Color.black);
                    GL.Vertex3(verts[quads[i + 2]].x, verts[quads[i + 2]].y, verts[quads[i + 2]].z);
                    GL.Vertex3(verts[quads[i + 1]].x, verts[quads[i + 1]].y, verts[quads[i + 1]].z);
                }
            }
            GL.End();
        }







        GL.Begin(GL.QUADS);
        for (int i = 0; i < quads.Count; i += 4)
        {
            // Face Pips
            if (((editMode == 1 && vertMode == 1 && selectedFacesDefined) || (editMode == 3 && paintMode == 1)))

            {
                Vector3 q = (
                    verts[quads[i + 0]] +
                    verts[quads[i + 1]] +
                    verts[quads[i + 2]] +
                    verts[quads[i + 3]]) * 0.25f + faceNormals[i / 4] * glExtrude;

                d = Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, q);

                spR = HandleUtility.GUIPointToWorldRay(verts[i] + new Vector3(r, 0, 0)).GetPoint(d);
                spL = HandleUtility.GUIPointToWorldRay(verts[i] + new Vector3(-r, 0, 0)).GetPoint(d);
                float pixelWidth = Vector3.Distance(spR, spL);
                //Graphics.DrawMesh(cube, q, Quaternion.identity, material, 0);
                right = SceneView.lastActiveSceneView.camera.transform.right * pixelWidth;
                up = SceneView.lastActiveSceneView.camera.transform.up * pixelWidth;


                if (selectedFacesDefined && selectedFaces[i / 4])
                {
                    GL.Color(Color.white);
                }
                else
                {
                    GL.Color(Color.black);
                }

                if (editMode == 3)
                {
                    Color col = new Color(
                        (colours[i + 0].r + colours[i + 1].r + colours[i + 2].r + colours[i + 3].r) / 4.0f,
                        (colours[i + 0].g + colours[i + 1].g + colours[i + 2].g + colours[i + 3].g) / 4.0f,
                        (colours[i + 0].b + colours[i + 1].b + colours[i + 2].b + colours[i + 3].b) / 4.0f,
                        (colours[i + 0].a + colours[i + 1].a + colours[i + 2].a + colours[i + 3].a) / 4.0f);

                    GL.Color(col);
                }

                GL.Vertex3(q.x + right.x + up.x, q.y + right.y + up.y, q.z + right.z + up.z);
                GL.Vertex3(q.x - right.x + up.x, q.y - right.y + up.y, q.z - right.z + up.z);
                GL.Vertex3(q.x - right.x - up.x, q.y - right.y - up.y, q.z - right.z - up.z);
                GL.Vertex3(q.x + right.x - up.x, q.y + right.y - up.y, q.z + right.z - up.z);

            }
        }
        GL.End();

        // Vert Pips

        if ((editMode == 1 && vertMode == 0 && selectedVertsDefined) || (editMode == 3 && paintMode == 0))
        {
            GL.Begin(GL.QUADS);
            for (int i = 0; i < verts.Count; i++)
            {
                if (doYouDrawThatVert[i])
                {
                    for (int j = 0; j < connectedVerts[i].Count; j++)
                    {
                        doYouDrawThatVert[connectedVerts[i].list[j]] = false;
                    }


                    Vector3 v = verts[i] + vertNormals[i] * glExtrude;
                    d = Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, v);

                    spR = HandleUtility.GUIPointToWorldRay(v + new Vector3(r, 0, 0)).GetPoint(d);
                    spL = HandleUtility.GUIPointToWorldRay(v + new Vector3(-r, 0, 0)).GetPoint(d);

                    float pixelWidth = Vector3.Distance(spR, spL);

                    right = SceneView.lastActiveSceneView.camera.transform.right * pixelWidth;
                     up = SceneView.lastActiveSceneView.camera.transform.up * pixelWidth;

                    if (selectedVertsDefined && selectedVerts[i])
                    {
                        GL.Color(Color.white);
                    }
                    else
                    {
                        GL.Color(Color.black);
                    }

                    if (editMode == 3)
                    {
                        GL.Color(colours[i]);
                    }

                    GL.Vertex3(v.x + right.x + up.x, v.y + right.y + up.y, v.z + right.z + up.z);
                    GL.Vertex3(v.x - right.x + up.x, v.y - right.y + up.y, v.z - right.z + up.z);
                    GL.Vertex3(v.x - right.x - up.x, v.y - right.y - up.y, v.z - right.z - up.z);
                    GL.Vertex3(v.x + right.x - up.x, v.y + right.y - up.y, v.z + right.z - up.z);

                }
            }

            GL.End();
        }

        if (drawNormals > 0)// &&  editorInfo.transformMode == 0)
        { 
            if (drawNormals == 1)
            {
                GL.Begin(GL.LINES);
                GL.Color(Color.green);
                for (int i = 0; i < quads.Count; i += 4)
                {
                    Vector3 c = (verts[quads[i + 0]] + verts[quads[i + 1]] + verts[quads[i + 2]] + verts[quads[i + 3]]) / 4.0f;
                    
                    GL.Vertex3(c.x, c.y, c.z);
                    c = c + faceNormals[i / 4] * normalLength;
                    GL.Vertex3(c.x, c.y, c.z);
                }
                GL.End();
            }
            else if (drawNormals == 2)
            {

                GL.Begin(GL.LINES);
                GL.Color(Color.blue);
                for (int ii = 0; ii < adjacentFaces.Count; ii++)
                {
                    for (int iii = 0; iii < 4; iii++)
                    {
                        int i = quads[ii * 4 + iii];

                        if (i < vertNormals.Count) // && ((selectedFaces[ii] && vertMode == 1) || (selectedVerts[i] && vertMode== 0)) )
                        {
                            Vector3 c = verts[i];
                            GL.Vertex3(c.x, c.y, c.z);
                            c = c + (Matrix4x4.TRS(Vector3.zero, gameObject.transform.rotation, gameObject.transform.lossyScale).MultiplyPoint3x4(mesh.normals[i]).normalized) * normalLength;// vertNormals[i].normalized * normalLength;
                            GL.Vertex3(c.x, c.y, c.z);
                        }
                    }
                }
                GL.End();
            }
            else if (drawNormals == 3)
            {

                GL.Begin(GL.LINES);
                GL.Color(Color.magenta);
                
                for (int i = 0; i < quads.Count; i += 4)
                {
                    int tA = (i / 4) * 2 * 3;
                    int tB = ((i / 4) * 2 + 1) * 3;
                    Vector3 ab = verts[tris[tA + 1]] - verts[tris[tA + 0]];
                    Vector3 ac = verts[tris[tA + 2]] - verts[tris[tA + 0]];
                    Vector3 dc = verts[tris[tB + 1]] - verts[tris[tB + 0]];
                    Vector3 db = verts[tris[tB + 2]] - verts[tris[tB + 0]];

                    Vector3 a = Vector3.Cross(ab, ac);
                    Vector3 b = Vector3.Cross(dc, db);
                    Vector3 c = (verts[tris[tA + 0]] + verts[tris[tA + 1]] + verts[tris[tA + 2]]) / 3.0f;

                    GL.Vertex3(c.x, c.y, c.z);
                    c = c + a.normalized * normalLength;
                    GL.Vertex3(c.x, c.y, c.z);
                    c = (verts[tris[tB + 0]] + verts[tris[tB + 1]] + verts[tris[tB + 2]]) / 3.0f;
                    GL.Vertex3(c.x, c.y, c.z);
                    c = c + b.normalized * normalLength;
                    GL.Vertex3(c.x, c.y, c.z);
                }
                GL.End();
            }
        }

        GL.PopMatrix();

        if (editMode == 3)
        {
            Vector2 mp = Event.current.mousePosition;
            float steps = 32;
            float step = (Mathf.PI * 2) / steps;

            float radius = tempPaintCircleRadius;

            lineMaterial.SetPass(0);

            GL.LoadPixelMatrix();

            GL.PushMatrix();
            GL.Begin(GL.LINE_STRIP);
            GL.Color(Color.white);

            float y = SceneView.lastActiveSceneView.camera.pixelHeight;
            for (int i = 0; i <= steps; i++)
            {
                GL.Vertex3(Mathf.Round(mp.x + Mathf.Cos(step * i) * radius), Mathf.Round(y - mp.y + Mathf.Sin(step * i) * radius), 0.99f);
            }
            GL.End();

            GL.PopMatrix();

        }
    }

    static Material lineMaterial;
    static void createLineMaterial()
    {
        if (!lineMaterial)
        {
            Shader shader = Shader.Find("MeshEdit/3DLine");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
    }
    static Material lineMaterial2D;
    static void createLineMaterial2D()
    {
        if (!lineMaterial2D)
        {
            Texture2D pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);

            Shader shader = Shader.Find("MeshEdit/AOTLine");
            lineMaterial2D = new Material(shader);

            lineMaterial2D.SetTexture("_MainTex", pixel);
            lineMaterial2D.SetFloat("_Alpha", 1f);
        }
    }

#endif
    public enum DebugShowInfo { Off, General, ConnectedVerts, AdjacentFaces, FaceTriangles, HalfEdges, FaceMaterial, OperationTime };
    [SerializeField]
    public DebugShowInfo debugMode = DebugShowInfo.Off;

#if UNITY_EDITOR
    [SerializeField, HideInInspector]
    private Vector3 editorPosition;
    [SerializeField, HideInInspector]
    private Vector3 editorScale;
    [SerializeField, HideInInspector]
    private Quaternion editorRotation;



    public int getEdgeIndexFromFaceVerts(int face, int vertA, int vertB)
    {
        for (int i = 0; i < quadEdgePatternClockwise.Length; i += 2)
        {
            if ((quads[face * 4 + quadEdgePatternClockwise[i + 0]] == vertA &&
                quads[face * 4 + quadEdgePatternClockwise[i + 1]] == vertB) ||
                (quads[face * 4 + quadEdgePatternClockwise[i + 0]] == vertB &&
                quads[face * 4 + quadEdgePatternClockwise[i + 1]] == vertA))
            {
                return i / 2;
            }
        }

        return -1;
    }

    public void recalculateNormals(Mesh mesh)
    {
        List<int> verts = new List<int>();
        List<int> faces = new List<int>();
        for (int i = 0; i < this.verts.Count; i++)
        {
            verts.Add(i);
        }
        for (int i = 0; i < this.adjacentFaces.Count; i++)
        {
            faces.Add(i);
        }
        recalculateSelectedNormals(mesh, verts, faces);

    }

    public Vector3[] getSharpNormals(Mesh mesh)
    {
        beginTimer("Get sharp normals");
        Vector3[] vertNormals = new Vector3[mesh.normals.Length];
        int[] div = new int[mesh.normals.Length];

        Vector3 a;
        Vector3 b;
        Vector3 c;
        Vector3 ab;
        Vector3 ac;

        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            int ta = mesh.triangles[i + 0];
            int tb = mesh.triangles[i + 1];
            int tc = mesh.triangles[i + 2];

            a = mesh.vertices[ta];
            b = mesh.vertices[tb];
            c = mesh.vertices[tc];

            // A
            ab.x = b.x - a.x;
            ab.y = b.y - a.y;
            ab.z = b.z - a.z;

            ac.x = c.x - a.x;
            ac.y = c.y - a.y;
            ac.z = c.z - a.z;

            vertNormals[ta] += Vector3.Cross(ab, ac).normalized;
            div[ta]++;

            // B
            ab.x = c.x - b.x;
            ab.y = c.y - b.y;
            ab.z = c.z - b.z;

            ac.x = a.x - b.x;
            ac.y = a.y - b.y;
            ac.z = a.z - b.z;

            vertNormals[tb] += Vector3.Cross(ab, ac).normalized;
            div[tb]++;

            // C
            ab.x = a.x - c.x;
            ab.y = a.y - c.y;
            ab.z = a.z - c.z;

            ac.x = b.x - c.x;
            ac.y = b.y - c.y;
            ac.z = b.z - c.z;

            vertNormals[tc] += Vector3.Cross(ab, ac).normalized;
            div[tc]++;
        }

        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            if (div[i] > 0)
            {
                vertNormals[i] /= div[i];
            }
        }

        endTimer();
        return vertNormals;
    }



    public Vector3 getSharpNormal(Mesh mesh, int face, int q)
    {
        int va = quads[face * 4 + 0];
        int vb = quads[face * 4 + 1];
        int vc = quads[face * 4 + 2];
        int vd = quads[face * 4 + 3];
        
        if (q == 0)
        {
            return Vector3.Cross(
                mesh.vertices[vb] - mesh.vertices[va],
                mesh.vertices[vc] - mesh.vertices[va]
                ).normalized;
        }
        else if (q == 1)
        {
            return (
                Vector3.Cross(
                    mesh.vertices[vc] - mesh.vertices[vb],
                    mesh.vertices[va] - mesh.vertices[vb]
                    ).normalized +

                Vector3.Cross(
                    mesh.vertices[vd] - mesh.vertices[vb],
                    mesh.vertices[vc] - mesh.vertices[vb]
                    ).normalized) / 2.0f;

        }
        else if (q == 2)
        {
            return (
                Vector3.Cross(
                    mesh.vertices[va] - mesh.vertices[vc],
                    mesh.vertices[vb] - mesh.vertices[vc]
                    ).normalized +

                Vector3.Cross(
                    mesh.vertices[vb] - mesh.vertices[vc],
                    mesh.vertices[vd] - mesh.vertices[vc]
                    ).normalized) / 2.0f;

        }
        else if (q == 3)
        {
            return Vector3.Cross(
                mesh.vertices[vc] - mesh.vertices[vd],
                mesh.vertices[vb] - mesh.vertices[vd]
                ).normalized;
        }

        return Vector3.zero;
        
    }
    
    public void recalculateNormalsOld(Mesh mesh = null)
    {
        beginTimer("Recalculate all normals");
        mesh.RecalculateNormals();
        Vector3 p = gameObject.transform.position;
        Quaternion r = gameObject.transform.rotation;
        Vector3 s = gameObject.transform.lossyScale;

        #region Normal calculation
        // 0.4
        for (int k = 0; k < faceNormals.Count; k++)
        {
            for (int j = 0; j < 4; j++)
            {
                int i = k * 4 + j;

                Vector3 n = mesh.normals[i];
                int sharedCount = 1;
                for (int ii = 0; ii < connectedVerts[i].list.Count; ii++)
                {
                    if (connectedVerts[i].list[ii] != i)
                    {
                        float angleBetween = Vector3.Angle(mesh.normals[i], mesh.normals[connectedVerts[i].list[ii]]);

                        if (angleBetween <= smoothnessThreshold)
                        {
                            n += mesh.normals[connectedVerts[i].list[ii]];
                            sharedCount++;
                        }
                    }
                }
                n /= sharedCount;

                vertNormals[i] = n;
            }
        }

        mesh.normals = vertNormals.ToArray();

        for (int i = 0; i < vertNormals.Count; i++)
        {
            Vector3 n = vertNormals[i];
            n.x *= s.x;
            n.y *= s.y;
            n.z *= s.z;
            n = r * n;
            vertNormals[i] = n;
        }
        #endregion

        faceNormals = new List<Vector3>();
        for (int i = 0; i < quads.Count; i += 4)
        {
            Vector3 n = Vector3.zero;

            int tA = (i / 4) * 2 * 3;
            int tB = ((i / 4) * 2 + 1) * 3;
            Vector3 ab = verts[tris[tA + 1]] - verts[tris[tA + 0]];
            Vector3 ac = verts[tris[tA + 2]] - verts[tris[tA + 0]];
            Vector3 dc = verts[tris[tB + 1]] - verts[tris[tB + 0]];
            Vector3 db = verts[tris[tB + 2]] - verts[tris[tB + 0]];


            Vector3 a = Vector3.Cross(ab, ac);
            Vector3 b = Vector3.Cross(dc, db);
            n = a + b;
            faceNormals.Add(n.normalized);
        }
        endTimer();
    }
    public void recalculateSelectedNormals(Mesh mesh, List<int> updatedVerts, List<int> updatedFaces)
    {
        if (meshNormals != null)
        {
            // On Extrude, there is a "recalculateNormals" call which updates all normals
            // During the transformation there is a constant recalculate call which only affects a limited number of verts. At this point, the previous verts have been affected in a way that causes a normal error, since there is a new set of zero area faces adjacent to the existing faces when their normals are calculated.

            //Vector3[] sharpNormals = getSharpNormals(mesh);
            bool[] vertsRemaining = new bool[verts.Count];

            Vector3 n;

            Vector3 p = gameObject.transform.position;
            Quaternion r = gameObject.transform.rotation;
            Vector3 s = gameObject.transform.lossyScale;

            for (int i = 0; i < updatedVerts.Count; i++)
            {
                vertsRemaining[updatedVerts[i]] = true;
            }

            beginTimer("Main loop of update verts");
            for (int iii = 0; iii < updatedFaces.Count; iii++)
            {
                int i = updatedFaces[iii];
                for (int q = 0; q < 4; q++)
                {
                    int vert = quads[i * 4 + q];

                    if (vertsRemaining[vert])
                    {
                        n = getSharpNormal(mesh, i, q);
                        List<int> normalIndexes = new List<int>();
                        normalIndexes.Add(vert);

                        int nextQ = -1;

                        Vector3 lastVertNormal = n;
                        int faceCW = getNextFaceAroundVert(i, ref vert, ref nextQ, -1);

                        int nextFace = faceCW;
                        int lastFace = i;

                        int c = 0;


                        while (
                            nextFace >= 0 &&
                            nextFace != i)
                        {
                            c++;
                            if (c > 10000)
                            {
                                Debug.Log("Loop too long!!");
                                break;
                            }

                            Vector3 vertNormal = getSharpNormal(mesh, nextFace, nextQ);

                            // Break if the two normals are not within the smoothness threshhold
                            if (Vector3.Angle(lastVertNormal, vertNormal) >= smoothnessThreshold)
                            {
                                break;
                            }
                            int sideOfOldFace = adjacentFaces[lastFace].list.IndexOf(nextFace);
                            int sideOfNewFace = adjacentFaces[nextFace].list.IndexOf(lastFace);

                            // Break if faces are separated by a sharp edge
                            if (halfEdges[lastFace].edges[sideOfOldFace].isSharp ||
                                halfEdges[nextFace].edges[sideOfNewFace].isSharp)
                            {
                                break;
                            }
                            n += vertNormal;
                            normalIndexes.Add(vert);

                            int temp = nextFace;

                            lastVertNormal = vertNormal;
                            nextFace = getNextFaceAroundVert(nextFace, ref vert, ref nextQ, lastFace);
                            lastFace = temp;
                        }



                        if (nextFace != i)
                        {
                            vert = quads[i * 4 + q];
                            int faceCCW = getNextFaceAroundVert(i, ref vert, ref nextQ, faceCW);
                            nextFace = faceCCW;
                            lastFace = i;

                            while (
                                nextFace >= 0 &&
                                nextFace != i)
                            {

                                c++;
                                if (c > 10000)
                                {
                                    Debug.Log("Loop too long!!");
                                    break;
                                }
                                Vector3 vertNormal = getSharpNormal(mesh, nextFace, nextQ);

                                // Break if the two normals are not within the smoothness threshhold
                                if (vertNormal.sqrMagnitude == 0 || Vector3.Angle(lastVertNormal, vertNormal) >= smoothnessThreshold)
                                {
                                    break;
                                }
                                int sideOfOldFace = adjacentFaces[lastFace].list.IndexOf(nextFace);
                                int sideOfNewFace = adjacentFaces[nextFace].list.IndexOf(lastFace);

                                // Break if faces are separated by a sharp edge
                                if (halfEdges[lastFace].edges[sideOfOldFace].isSharp ||
                                    halfEdges[nextFace].edges[sideOfNewFace].isSharp)
                                {
                                    break;
                                }
                                n += vertNormal;
                                normalIndexes.Add(vert);

                                int temp = nextFace;

                                lastVertNormal = vertNormal;
                                nextFace = getNextFaceAroundVert(nextFace, ref vert, ref nextQ, lastFace);
                                lastFace = temp;
                            }
                        }

                        n /= normalIndexes.Count;
                        for (int ii = 0; ii < normalIndexes.Count; ii++)
                        {
                            meshNormals[normalIndexes[ii]] = n;
                            vertsRemaining[normalIndexes[ii]] = false;
                        }
                    }
                }
            }

            mesh.normals = meshNormals.ToArray();
            endTimer();
            beginTimer("Update the world space normals");
            for (int i = 0; i < updatedVerts.Count; i++)
            {
                n = meshNormals[updatedVerts[i]];

                n.x *= s.x;
                n.y *= s.y;
                n.z *= s.z;
                n = r * n;

                vertNormals[updatedVerts[i]] = n.normalized;

            }

            endTimer();
            // Face normals

            beginTimer("Update world face normals");
            for (int i = 0; i < updatedFaces.Count; i++)
            {
                int face = updatedFaces[i];
                {
                    n = Vector3.zero;

                    int tA = face * 2 * 3;
                    int tB = (face * 2 + 1) * 3;
                    Vector3 ab = verts[tris[tA + 1]] - verts[tris[tA + 0]];
                    Vector3 ac = verts[tris[tA + 2]] - verts[tris[tA + 0]];
                    Vector3 dc = verts[tris[tB + 1]] - verts[tris[tB + 0]];
                    Vector3 db = verts[tris[tB + 2]] - verts[tris[tB + 0]];


                    Vector3 a = Vector3.Cross(ab, ac);
                    Vector3 b = Vector3.Cross(dc, db);
                    n = a + b;
                    faceNormals[face] = n.normalized;
                }
            }
            endTimer();
        }
        else
        {
            Debug.Log("Error: Mesh Normals have not been defined");
        }
    }

    public void recalculateSelectedNormalsOld(Mesh mesh, List<int> updatedVerts, List<int> updatedFaces)
    {
        beginTimer("Recalculate selected normals");
        mesh.RecalculateNormals();

        
        Vector3 p = gameObject.transform.position;
        Quaternion r = gameObject.transform.rotation;
        Vector3 s = gameObject.transform.lossyScale;

        Vector3 n;

        bool[] vertsRemaining = new bool[verts.Count];
        for (int i = 0; i < updatedVerts.Count; i++)
        {
            vertsRemaining[updatedVerts[i]] = true;
        }

        s.x = Mathf.Max(Mathf.Abs(s.x), minimumScale) * Mathf.Sign(s.x);
        s.y = Mathf.Max(Mathf.Abs(s.y), minimumScale) * Mathf.Sign(s.y);
        s.z = Mathf.Max(Mathf.Abs(s.z), minimumScale) * Mathf.Sign(s.z);

        for (int i = 0; i < vertNormals.Count; i++)
        {
            n = vertNormals[i];

            n /= s.x;
            n /= s.y;
            n /= s.z;

            n = Quaternion.Inverse(r) * n;

            vertNormals[i] = n;

        }





        for (int i = 0; i < updatedVerts.Count; i++)
        {















            int vert = updatedVerts[i];
            if (vertsRemaining[vert])
            {
                n = mesh.normals[vert];
                int sharedCount = 1;
                List<int> sameNormals = new List<int>();
                sameNormals.Add(updatedVerts[i]);
                for (int ii = 0; ii < connectedVerts[vert].list.Count; ii++)
                {
                    int connectedVert = connectedVerts[vert].list[ii];
                    if (connectedVert != vert)
                    {
                        float angleBetween = Vector3.Angle(mesh.normals[vert], mesh.normals[connectedVert]);

                        if (angleBetween <= smoothnessThreshold)
                        {
                            n += mesh.normals[connectedVert];
                            sameNormals.Add(connectedVert);
                            sharedCount++;
                        }
                    }
                }
                n /= sharedCount;

                for (int ii = 0; ii < sameNormals.Count; ii++)
                {
                    vertNormals[sameNormals[ii]] = n;
                    vertsRemaining[sameNormals[ii]] = false;
                }
            }
        }
        mesh.normals = vertNormals.ToArray();

        for (int i = 0; i < vertNormals.Count; i++)
        {
            n = vertNormals[i];
            
            n.x *= s.x;
            n.y *= s.y;
            n.z *= s.z;
            n = r * n;

            vertNormals[i] = n;

        }

        for (int i = 0; i < updatedFaces.Count; i++)
        {
            int face = updatedFaces[i];
            {
                n = Vector3.zero;

                int tA = face * 2 * 3;
                int tB = (face * 2 + 1) * 3;
                Vector3 ab = verts[tris[tA + 1]] - verts[tris[tA + 0]];
                Vector3 ac = verts[tris[tA + 2]] - verts[tris[tA + 0]];
                Vector3 dc = verts[tris[tB + 1]] - verts[tris[tB + 0]];
                Vector3 db = verts[tris[tB + 2]] - verts[tris[tB + 0]];


                Vector3 a = Vector3.Cross(ab, ac);
                Vector3 b = Vector3.Cross(dc, db);
                n = a + b;
                faceNormals[face] = n.normalized;
            }
        }
        endTimer();
    }

    public void checkMeshValidity()
    {
        // Notes on potential errors:
        // When checking the validity of a mesh that has just had it's state Undone or Redone, the Mesh (property: _mesh) may not have been undone along with the values in the other
        //  arrays (e.g. verts or tris). Because of this, if you compare any list or array to a property directly on the mesh, it may not be accurate to the state you are intending to check against,
        //  and will cause errors. E.g. the error caused where setting triangles back to the mesh was setting triangles for verts that did not exist in the mesh, because it had been compared to the 
        //  mesh after Undoing an extrusion function.

        Vector3 p = gameObject.transform.position;
        Quaternion r = gameObject.transform.rotation;
        Vector3 s = gameObject.transform.lossyScale;

        // Transform info
        if (p != editorPosition ||
            r != editorRotation ||
            s != editorScale)
        {
            editorPosition = p;
            editorRotation = r;
            editorScale = s;
            // GetVerts is necessary here, because "verts" represents the verts in world space and must be updated to reflect the transforms of the mesh.
            getVerts(false);
        }

        if (_mesh == null)
        {
            Debug.Log("Warning! Local Mesh is null at a time when it may be called");

            Mesh srcMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;

            deepCopyMesh(ref srcMesh, ref _mesh);
        }

        if (verts == null ||
            colours == null ||
            vertNormals == null)
        {
            Debug.Log("Warning! Verts are null at a time when they may be called");
            getVerts(false);
        }
        
        if (meshNormals == null || meshNormals.Count != vertNormals.Count)
        {
            Debug.Log("Warning! Mesh normals have not been defined or are not up to date.");
            meshNormals = new List<Vector3>();
            for (int i = 0; i < vertNormals.Count; i++)
            {
                meshNormals.Add(vertNormals[i]);
            }
        }

        if (tris == null || tris.Count != verts.Count * (3.0f / 2.0f))
        {
            Debug.Log("Warning! Tris are null at a time when they may be called");
            tris = new List<int>();
            for (int i = 0; i < mesh.triangles.Length; i++)
            {
                tris.Add(mesh.triangles[i]);
            }
        }

        for (int i = 0; i < uvMaps.Count; i++)
        {
            uvMaps[i].vertCount = verts.Count;
        }

        if (quads == null)// || quads.Count != verts.Count || faceNormals.Count != verts.Count / 4)
        {
            Debug.Log("Warning! Quads are null at a time when they may be called");
            getQuads(false);
        }

        if (connectedVerts == null || connectedVerts.Count != verts.Count)
        {
            Debug.Log("Warning! Connected verts is either null or not accurate to the vert list.");
            getConnectedVertsBasedOnPosition();
        }

        // This line will actually rebuild the adjacentFaces list, because it has its creation function in its getter
        if (adjacentFaces == null || adjacentFaces.Count != quads.Count / 4)
        {
            Debug.Log("C" + (adjacentFaces.Count) + " / " + quads.Count);
            Debug.Log("Warning! Adjacent faces is either null or not accurate to the list of faces.");
            rebuildAdjacentFaces();
        }


        int targetMaterialCount = 1;
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            targetMaterialCount = renderer.sharedMaterials.Length;
        }

        if (materials == null || materials.Count == 0 || materials.Count != targetMaterialCount)
        {
            getMaterials();
            selectedMaterial = (int)Mathf.Clamp( selectedMaterial, 0, materials.Count - 1);
            Debug.Log("Warning! Material list is either null or inaccurate.");
        }

        if (materialUVMap == null || materialUVMap.Count != materials.Count)
        {
            Debug.Log("Warning! Material UVMap reference is either null or not accurate.");

            materialUVMap = new List<int>();
            for (int i = 0; i < materials.Count; i++)
            {
                materialUVMap.Add(materialUVMap[selectedMaterial]);
            }
        }

        if (editorInfo.customTilesetSettings == null || editorInfo.customTilesetSettings.Count < materials.Count)
        {
            editorInfo.customTilesetSettings = new List<EditorInfo.CustomTilesetEditorSettings>();
            for (int i = 0; i < materials.Count; i++)
            {
                editorInfo.customTilesetSettings.Add(new EditorInfo.CustomTilesetEditorSettings());
            }
        }

        if (quadMaterial == null || quadMaterial.Count != quads.Count / 4)
        {
            Debug.Log("Warning! Face material list is either null or not accurate to the list of faces.");

            quadMaterial = new List<int>();
            for (int i = 0; i < quads.Count; i += 4)
            {
                quadMaterial.Add(0);
            }
        }

        if (customTextureUVMap == null || customTextureUVMap.Count != verts.Count)
        {
            Debug.Log("Warning! UV map for custom textures is either null or not accurate to the list of verts.");
            customTextureUVMap = new List<Vector2>();
            for (int i = 0; i < verts.Count; i++)
            {
                customTextureUVMap.Add(Vector2.zero);
            }
        }

        if (halfEdges == null || halfEdges.Count != adjacentFaces.Count)
        {
            Debug.Log("Warning! Half Edges are either null or not mapped to the list of faces.");
            //if (halfEdges == null) { Debug.Log("Null"); }else { Debug.Log("HEdges: " + halfEdges.Count + "/" + adjacentFaces.Count); }
            defineAllHalfEdges();
        }
    }

    public void getMaterials()
    {
        materials = new List<Material>();
        editorInfo.customTilesetSettings = new List<EditorInfo.CustomTilesetEditorSettings>();
        materialUVMap = new List<int>();

        Material[] tempMaterials = gameObject.GetComponent<Renderer>().sharedMaterials;
        for (int i = 0; i < tempMaterials.Length; i++)
        {
            materials.Add(tempMaterials[i]);
            editorInfo.customTilesetSettings.Add(new EditorInfo.CustomTilesetEditorSettings());
            materialUVMap.Add(-1);
        }

        quadMaterial = new List<int>();
        for (int i = 0; i < quads.Count; i += 4)
        {
            quadMaterial.Add(0);
        }
    }
    public void addMaterial()
    {
        Material newMaterial = new Material(Shader.Find("MeshEdit/ModellingShader"));

        materials.Add(newMaterial);
        materialUVMap.Add(-1);
        editorInfo.customTilesetSettings.Add(new EditorInfo.CustomTilesetEditorSettings());
        gameObject.GetComponent<Renderer>().sharedMaterials = materials.ToArray();

        setTriangles();
    }

    public bool removeMaterial(int index)
    {
        if (index >= 0 && index < materials.Count && materials.Count > 1)
        {
            for (int i = 0; i < quadMaterial.Count; i++)
            {
                if (quadMaterial[i] == index)
                {
                    quadMaterial[i] = 0;
                }
                else if (quadMaterial[i] > index)
                {
                    quadMaterial[i]--;
                }
            }

            materialUVMap.RemoveAt(index);
            materials.RemoveAt(index);
            editorInfo.customTilesetSettings.RemoveAt(index);
            gameObject.GetComponent<Renderer>().sharedMaterials = materials.ToArray();

            setTriangles();
            return true;
        }
        else
        {
            return false;
        }
    }

    // Get the verts, vertNormals and colours from the display mesh
    public void getVerts(bool useDisplayMesh)
    {
        Mesh m = mesh;
        if (useDisplayMesh)
        {
            m = gameObject.GetComponent<MeshFilter>().sharedMesh;
        }

        Vector3 p = gameObject.transform.position;
        Quaternion r = gameObject.transform.rotation;
        Vector3 s = gameObject.transform.lossyScale;

        verts = new List<Vector3>();
        vertNormals = new List<Vector3>();
        meshNormals = new List<Vector3>();
        colours = new List<Color>();
        s.x = Mathf.Max(Mathf.Abs(s.x), minimumScale) * Mathf.Sign(s.x);
        s.y = Mathf.Max(Mathf.Abs(s.y), minimumScale) * Mathf.Sign(s.y);
        s.z = Mathf.Max(Mathf.Abs(s.z), minimumScale) * Mathf.Sign(s.z);
        for (int i = 0; i < m.vertices.Length; i++)
        {
            Vector3 v = m.vertices[i];
            v.x *= s.x;
            v.y *= s.y;
            v.z *= s.z;
            v = r * v;
            v += p;
            verts.Add(v);

            Vector3 n = m.normals[i];
            meshNormals.Add(new Vector3(n.x, n.y, n.z));
            n.x *= s.x;
            n.y *= s.y;
            n.z *= s.z;
            n = r * n;
            vertNormals.Add(n);

            colours.Add(m.colors[i]);
        }
    }

    public void getConnectedVertsBasedOnPosition()
    {
        connectedVerts = new List<ListWrapper>();
        for (int i = 0; i < verts.Count; i++)
        {
            List<int> sharedVerts = new List<int>();
            for (int j = 0; j < verts.Count; j++)
            {
                if (j != i)
                {
                    if (verts[i] == verts[j])
                    {
                        sharedVerts.Add(j);
                    }
                }
            }

            connectedVerts.Add(new ListWrapper(sharedVerts));
        }
    }

    // Gets the quads, faceNormals from display mesh
    public void getQuads(bool useDisplayMesh)
    {
        Mesh m = _mesh;
        if (useDisplayMesh)
        {
            // DO NOT reference the mesh that belongs to the MeshEdit object
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            m = meshFilter.sharedMesh;
        }

        quads = new List<int>();
        faceNormals = new List<Vector3>();
        quadMaterial = new List<int>();

        for (int i = 0; i < m.triangles.Length; i += 3)
        {
            int[] coords = { -1, -1, -1, -1 };
            coords[0] = m.triangles[i];
            coords[1] = m.triangles[i + 1];
            coords[2] = m.triangles[i + 2];
            coords[3] = findAdjacentTriPoint(m.triangles, m.vertices, ref coords[1], ref coords[2], ref coords[0]);

            bool isNewQuad = true;

            for (int ii = 0; ii < 4; ii++)
            {
                if (quads.Contains(coords[ii]))
                {
                    isNewQuad = false;
                    break;
                }
            }

            if (isNewQuad)
            {
                Vector3 n = Vector3.zero;
                for (int ii = 0; ii < 4; ii++)
                {
                    quads.Add(coords[ii]);
                    n += vertNormals[coords[ii]];
                }
                n /= 4;
                faceNormals.Add(n);
                quadMaterial.Add(0);

            }
        }
    }

    List<Vector3> tracker;

    public void getFirstFaceInLoop(ref int face, ref int a, ref int b, ref int c, ref int d, List<int> facesChecked = null)
    {
        if (facesChecked == null)
        {
            tracker = new List<Vector3>();
            facesChecked = new List<int>();
        }
        int connectedFace = -1;

        c = a;
        d = b;

        facesChecked.Add(face);
        // This could be made more efficient by only calling adjacent faces (via connectedVerts)
        for (int i = 0; i < quads.Count; i += 4)
        {
            if (i != face * 4)
            {
                bool foundA = false;
                bool foundB = false;

                int fA = -1;
                int fB = -1;
                int fC = -1;
                int fD = -1;
                for (int ii = 0; ii < 4; ii++)
                {
                    if (!foundA && connectedVerts[quads[i + ii]].Contains(quads[face * 4 + a]))
                    {

                        fA = ii;
                        foundA = true;
                    }
                    else if (!foundB && connectedVerts[quads[i + ii]].Contains(quads[face * 4 + b]))
                    {
                        fB = ii;
                        foundB = true;
                    }
                }

                if (foundA && foundB)
                {
                    connectedFace = i / 4;

                    getOppositeSideOfQuadRelative(connectedFace, fA, fB, out fC, out fD);

                    a = fC;
                    b = fD;

                    if (facesChecked.Contains(connectedFace))
                    {

                        return;
                    }
                    else
                    {
                        face = connectedFace;
                        getFirstFaceInLoop(ref face, ref a, ref b, ref c, ref d, facesChecked);
                    }

                    break;
                }
            }
        }

        return;
    }

    public int getSameEdgeOnAdjacentFace(int currentFace, int adjacentFaceIndex, int edgeOnCurrentFace)
    {
        return -1;
    }

    public void getLoopCut(int face, int a, int b, ref List<int> facesCut, ref List<int> cutsAB, ref List<int[]> cutAdjacentFaces, ref bool isLooping)
    {
        int connectedFace = -1;

        int c = -1;
        int d = -1;

        getOppositeSideOfQuadRelative(face, a, b, out c, out d);

        facesCut.Add(face);
        cutsAB.Add(quads[face * 4 + a]);
        cutsAB.Add(quads[face * 4 + b]);
        cutsAB.Add(quads[face * 4 + c]);
        cutsAB.Add(quads[face * 4 + d]);

        int[] tempAdjacent = new int[4];
        int fcC = 0;
        for (int i = 0; i < quadEdgePatternClockwise.Length; i += 2)
        {
            if ((a == quadEdgePatternClockwise[i + 0] &&
                b == quadEdgePatternClockwise[i + 1]) ||
                (a == quadEdgePatternClockwise[i + 1] &&
                b == quadEdgePatternClockwise[i + 0]))
            {
                tempAdjacent[0] = adjacentFaces[face].list[i / 2];
                fcC += 1;
            }

            else if ((b == quadEdgePatternClockwise[i + 0] &&
                d == quadEdgePatternClockwise[i + 1]) ||
                (b == quadEdgePatternClockwise[i + 1] &&
                d == quadEdgePatternClockwise[i + 0]))
            {
                tempAdjacent[1] = adjacentFaces[face].list[i / 2];
                fcC += 2;
            }
            else if ((d == quadEdgePatternClockwise[i + 0] &&
                c == quadEdgePatternClockwise[i + 1]) ||
                (d == quadEdgePatternClockwise[i + 1] &&
                c == quadEdgePatternClockwise[i + 0]))
            {
                tempAdjacent[2] = adjacentFaces[face].list[i / 2];
                fcC += 4;
            }
            else if ((a == quadEdgePatternClockwise[i + 0] &&
                c == quadEdgePatternClockwise[i + 1]) ||
                (a == quadEdgePatternClockwise[i + 1] &&
                c == quadEdgePatternClockwise[i + 0]))
            {
                tempAdjacent[3] = adjacentFaces[face].list[i / 2];
                fcC += 8;
            }
        }

        cutAdjacentFaces.Add(tempAdjacent);

        for (int i = 0; i < quads.Count; i += 4)
        {
            if (i != face * 4)
            {
                bool foundA = false;
                bool foundB = false;

                int fA = -1;
                int fB = -1;
                int fC = -1;
                int fD = -1;
                for (int ii = 0; ii < 4; ii++)
                {
                    if (!foundA && connectedVerts[quads[i + ii]].Contains(quads[face * 4 + a]))
                    {

                        fA = ii;
                        foundA = true;
                    }
                    else if (!foundB && connectedVerts[quads[i + ii]].Contains(quads[face * 4 + b]))
                    {
                        fB = ii;
                        foundB = true;
                    }
                }

                if (foundA && foundB)
                {
                    connectedFace = i / 4;

                    getOppositeSideOfQuadRelative(connectedFace, fA, fB, out fC, out fD);


                    c = fC;
                    d = fD;

                    if (facesCut.Contains(connectedFace))
                    {
                        if (connectedFace == facesCut[0])
                        {
                            isLooping = true;
                        }
                        // We've found the start of the loop
                        return;
                    }
                    else
                    {
                        getLoopCut(connectedFace, c, d, ref facesCut, ref cutsAB, ref cutAdjacentFaces, ref isLooping);
                    }

                    break;
                }
            }
        }

        return;
    }

    // TODO: to make a REAL version of loop cut that really covers all contingencies, you need to update the "adjacentFaces" list to contain a list of ALL adjacent faces. Then you can properly backstep through the list of found faces and get a full loop.
    public void getLoopCutV2(int face, bool isFaceNSLoop, ref List<int> facesCut, ref List<List<int>> tempAdjacentFaces, ref List<int> cutsAB)
    {/*
        // FORMAT
        // The returned info represents a list of connected faces that are directly adjacent and face the same direction
        int startingFace = face;
        bool wasStartingFaceNS = isFaceNSLoop;

        int edgeFacing = 1;

        if (isFaceNSLoop)
        {
            edgeFacing = 0;
        }

        int nextFace = face;
        int nextEdgeFacing = edgeFacing;
        bool isNextFaceNS = isFaceNSLoop;


        // First, find where the loop starts
        int c = 0;

        List<int> facesPassed = new List<int>();

        do
        {
            c++;

            // Add the last face to the list


            // Get the next face & edge in the loop
            face = nextFace;
            isFaceNSLoop = isNextFaceNS;

            facesPassed.Add(face);

            edgeFacing = nextEdgeFacing;
            nextFace = adjacentFaces[face].list[edgeFacing];

            if (nextFace != -1)
            {
                nextEdgeFacing = adjacentFaces[nextFace].list.IndexOf(face);
                nextEdgeFacing = (nextEdgeFacing + 2) % 4;

                isNextFaceNS = true;
                if (nextEdgeFacing % 2 == 1)
                {
                    isNextFaceNS = false;
                }
            }



            // Check against the previous faces in the loop
            int indexOfRepeatedFace = facesPassed.IndexOf(nextFace);

            if (indexOfRepeatedFace >= 0)
            {
                break;
            }
        }
        while (nextFace != -1 && c < 100);
        

        // Flip the travelling direction of the loop
        edgeFacing = (edgeFacing + 2) % 4;
        nextEdgeFacing = edgeFacing;
        startingFace = face;
        isNextFaceNS = isFaceNSLoop;
        nextFace = face;

        c = 0;
        do
        {
            c++;
            face = nextFace;
            edgeFacing = nextEdgeFacing;
            isFaceNSLoop = isNextFaceNS;
            // Add the last face to the list
            facesCut.Add(face);
            // THIS IS INCORRECT as it sometimes gets the right orientation but flipped horizontally compared to the previous verts. It's absolutely necessary that the verts align in the cutsAB list

            if (edgeFacing == 0)
            {
                cutsAB.Add(quads[face * 4 + 0]);
                cutsAB.Add(quads[face * 4 + 1]);
                cutsAB.Add(quads[face * 4 + 2]);
                cutsAB.Add(quads[face * 4 + 3]);
            }
            else if (edgeFacing == 1)
            {
                cutsAB.Add(quads[face * 4 + 1]);
                cutsAB.Add(quads[face * 4 + 3]);
                cutsAB.Add(quads[face * 4 + 0]);
                cutsAB.Add(quads[face * 4 + 2]);
            }
            else if (edgeFacing == 2)
            {
                cutsAB.Add(quads[face * 4 + 3]);
                cutsAB.Add(quads[face * 4 + 2]);
                cutsAB.Add(quads[face * 4 + 1]);
                cutsAB.Add(quads[face * 4 + 0]);
            }
            else if (edgeFacing == 3)
            {
                cutsAB.Add(quads[face * 4 + 2]);
                cutsAB.Add(quads[face * 4 + 0]);
                cutsAB.Add(quads[face * 4 + 3]);
                cutsAB.Add(quads[face * 4 + 1]);
            }


            // Get the next face & edge in the loop
            nextFace = adjacentFaces[face].list[edgeFacing];

            if (nextFace != -1)
            {
                nextEdgeFacing = adjacentFaces[nextFace].list.IndexOf(face);
                nextEdgeFacing = (nextEdgeFacing + 2) % 4;

                isNextFaceNS = true;
                if (nextEdgeFacing % 2 == 1)
                {
                    isNextFaceNS = false;
                }
            }


            // Check against the previous faces in the loop

            int indexOfRepeatedFace = facesCut.IndexOf(nextFace);

            if (indexOfRepeatedFace >= 0)
            {
                break;
            }
        }
        while (nextFace != -1 && c < 100);

        return;*/
    }

    public static List<int> getFaceLoop(List<ListWrapper> adjacentFaces, int selectedFace, bool isNSCut)
    {
        List<bool> list = new List<bool>();
        return getFaceLoop(adjacentFaces, selectedFace, isNSCut, out list);
    }
    public static List<int> getFaceLoop(List<ListWrapper> adjacentFaces, int selectedFace, bool isNSCut, out List<bool> faceDirections)
    {
        List<int> addedFaces = new List<int>();
        List<bool> addedFaceDirection = new List<bool>();
        addedFaces.Add(selectedFace);
        addedFaceDirection.Add(isNSCut);
        // keep adding quads to cut until you encounter a quad with no adjacent face to cut, OR if you encounter a quad that has already been cut in the SAME DIRECTION that you try to cut it
        // Repeat this loop forward and backward from the selected face
        // A cut along the North-West (NW) axis refers to the cut travelling from edge 0 to edge 1 (The edges created by verts 0-1 and 3-2)

        for (int i = 0; i < 2; i++)
        {
            int edgeTheNextCutEnteredFrom = 0;

            if (!isNSCut)
            {
                edgeTheNextCutEnteredFrom = 1;
            }

            if (i > 0)
            {
                edgeTheNextCutEnteredFrom = (edgeTheNextCutEnteredFrom + 2) % 4;
            }

            bool isConnectionFound = true;

            int cc = 0;

            int currentFace = selectedFace;

            while (isConnectionFound)
            {
                cc++;
                if (cc > 10000)
                {
                    Console.WriteLine("Error");
                    break;
                }

                int nextFace = adjacentFaces[currentFace].list[(edgeTheNextCutEnteredFrom + 2) % 4];
                if (nextFace == -1)
                {
                    break;
                }

                edgeTheNextCutEnteredFrom = adjacentFaces[nextFace].list.IndexOf(currentFace);

                // The North-South variable isn't absolute, and a series of NS cuts may make a loop that cuts perpendicular along the loop. It's only necessary to compare a quad to itself in the case of a crossover in the loop.
                bool isNextAnNSCut = (edgeTheNextCutEnteredFrom == 0 || edgeTheNextCutEnteredFrom == 2);

                // Make sure "nextFace" hasn't already been cut in the same way
                int repeatIndex = addedFaces.IndexOf(nextFace);
                if (repeatIndex >= 0)
                {
                    if (addedFaceDirection[repeatIndex] == isNextAnNSCut)
                    {
                        isConnectionFound = false;
                    }
                }

                if (isConnectionFound)
                {
                    addedFaces.Add(nextFace);
                    currentFace = nextFace;

                    addedFaceDirection.Add(isNextAnNSCut);
                }

            }
        }
        faceDirections = addedFaceDirection;
        return addedFaces;
    }

    public void flipFaces(bool[] selectedFaces)
    {
        int[] newTriangles = new int[mesh.triangles.Length];
        for (int i = 0; i < newTriangles.Length; i++)
        {
            newTriangles[i] = mesh.triangles[i];
        }
        for (int i = 0; i < newTriangles.Length; i += 3)
        {
            int face = i / 6;
            if (selectedFaces[face])
            {
                int temp = newTriangles[i + 1];
                newTriangles[i + 1] = newTriangles[i + 2];
                newTriangles[i + 2] = temp;

                faceNormals[face] = faceNormals[face] * -1;

                temp = tris[i + 1];
                tris[i + 1] = tris[i + 2];
                tris[i + 2] = temp;
            }
        }
        for (int i = 0; i < quads.Count; i += 4)
        {
            int face = i / 4;
            if (selectedFaces[face])
            {
                halfEdges[face].flipEdges();

                int temp = quads[i + 1];
                quads[i + 1] = quads[i + 2];
                quads[i + 2] = temp;

                temp = adjacentFaces[face].list[0];
                adjacentFaces[face].list[0] = adjacentFaces[face].list[3];
                adjacentFaces[face].list[3] = temp;
                temp = adjacentFaces[face].list[1];
                adjacentFaces[face].list[1] = adjacentFaces[face].list[2];
                adjacentFaces[face].list[2] = temp;


                //   Debug.Log("New Face Adjacency: {" + adjacentFaces[face].list[0] + ", " + adjacentFaces[face].list[1] + ", " + adjacentFaces[face].list[2] + ", " + adjacentFaces[face].list[3] + "}");
            } 
        }

        setTriangles();
        // mesh.triangles = newTriangles;
        recalculateNormals(mesh);
        pushLocalMeshToGameObject();
    }


    public void flipSaddling(bool[] selectedFaces)
    {
        int[] newTriangles = new int[mesh.triangles.Length];
        for (int i = 0; i < newTriangles.Length; i++)
        {
            newTriangles[i] = mesh.triangles[i];
        }

        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedFaces[i])
            {
                // Rotate the indexes of the quad
                int temp = quads[i * 4 + 0];
                quads[i * 4 + 0] = quads[i * 4 + 1];
                quads[i * 4 + 1] = quads[i * 4 + 3];
                quads[i * 4 + 3] = quads[i * 4 + 2];
                quads[i * 4 + 2] = temp;

                halfEdges[i].rotateEdges();

                int t = i * 6;

                newTriangles[t + 0] = quads[i * 4 + 0];
                newTriangles[t + 1] = quads[i * 4 + 1];
                newTriangles[t + 2] = quads[i * 4 + 2];
                newTriangles[t + 3] = quads[i * 4 + 3];
                newTriangles[t + 4] = quads[i * 4 + 2];
                newTriangles[t + 5] = quads[i * 4 + 1];

                tris[t + 0] = quads[i * 4 + 0];
                tris[t + 1] = quads[i * 4 + 1];
                tris[t + 2] = quads[i * 4 + 2];
                tris[t + 3] = quads[i * 4 + 3];
                tris[t + 4] = quads[i * 4 + 2];
                tris[t + 5] = quads[i * 4 + 1];

                temp = adjacentFaces[i].list[0];
                int j = 0;
                for (j = 1; j < adjacentFaces[i].Count; j++)
                {
                    adjacentFaces[i].list[j - 1] = adjacentFaces[i].list[j];
                }
                adjacentFaces[i].list[j - 1] = temp;
            }
        }

        //mesh.triangles = newTriangles;

        setTriangles();

        recalculateNormals(mesh);

        pushLocalMeshToGameObject();
    }
    public void setTriangles()
    {

        if (materials.Count > 1)
        {
            List<List<int>> newTriangles = new List<List<int>>();
            for (int i = 0; i < materials.Count; i++)
            {
                newTriangles.Add(new List<int>());
            }

            mesh.subMeshCount = materials.Count;
            for (int i = 0; i < quadMaterial.Count; i++)
            {
                newTriangles[quadMaterial[i]].Add(quads[i * 4 + 0]);
                newTriangles[quadMaterial[i]].Add(quads[i * 4 + 1]);
                newTriangles[quadMaterial[i]].Add(quads[i * 4 + 2]);
                newTriangles[quadMaterial[i]].Add(quads[i * 4 + 3]);
                newTriangles[quadMaterial[i]].Add(quads[i * 4 + 2]);
                newTriangles[quadMaterial[i]].Add(quads[i * 4 + 1]);


            }

            for (int i = 0; i < materials.Count; i++)
            {
                mesh.SetTriangles(newTriangles[i].ToArray(), i);
            }
        }
        else
        {
            mesh.triangles = tris.ToArray();
        }
    }

    /* Disconnects all links in a connection group to the selected vert
     */
    private void disconnect(int referenceVert, int disconnectedVert)
    {
        for (int i = 0; i < connectedVerts[referenceVert].Count; i++)
        {
            connectedVerts[connectedVerts[referenceVert].list[i]].Remove(disconnectedVert);
        }
        connectedVerts[disconnectedVert].Clear();
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
#endif
    public bool isShared(int vertA, int vertB)
    {
        return (connectedVerts[vertA].Contains(vertB) ||
            connectedVerts[vertB].Contains(vertA));
    }
#if UNITY_EDITOR
    public bool areVertsSharingAnEdge(int vertA, int vertB)
    {
        for (int i = -1; i < connectedVerts[vertA].Count; i++)
        {
            int faceA = quads.IndexOf(vertA) / 4;
            if (i >= 0)
            {
                faceA = quads.IndexOf(connectedVerts[vertA].list[i]) / 4;
            }

            for (int ii = -1; ii < connectedVerts[vertB].Count; ii++)
            {
                int faceB = quads.IndexOf(vertB) / 4;

                if (ii >= 0)
                {
                    faceB = quads.IndexOf(connectedVerts[vertB].list[ii]) / 4;
                }

                if (faceA == faceB)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Extrude a face around the perimiter of the selected area.
    /// Returns the new array of selected vertices
    /// </summary>
    /// <param name="selectedVerts"></param>
    /// <param name="selectedFaces"></param>
    /// <returns></returns>
    public bool[] extrude(List<int> selectedEdges, List<int> faceRefs, bool[] selectedVerts, bool[] selectedFaces, Vector3 direction)
    {
        //SelectedEdges:
        // ...[][a, b, c, d][]...
        //FaceRefs
        // ...[][f1, f2][]...
        // a and b connect to a selected edge, c and d don't, c and d can be -1, meaning there is no face across from the selected face
        // f1 is the selected face and f2 is the opposite face, f2 can be -1
        // if f1 isn't selected, the edge has no face

        #region Save the connections existing between selected edges









        DateTime start = DateTime.Now;

        int vertexCount = verts.Count;
        int faceCount = adjacentFaces.Count;

        List<int>[] originalConnections = new List<int>[selectedEdges.Count];



        /* There are three types of link we need to connect to make sure the new faces are properly attached to the rest of the mesh
         * 1. Top->Down: Where the vert leading an extrusion is linked to the original mesh, pass that down to the bottom of the new extrusion (ignoring verts from selected faces)
         * 2. Down->Up : The vert leading the extrusion described previously, that must be passed to the upward portion of the new face
         * 3. Lateral  : The connections of each new face around the perimeter of the selected area
         *  
         *              ／
         *            ／
         *     ＼   ／                              0 ／ |
         *        ＼ 1   Selected Face             ／ 2
         *        |3 ＼                         ／   
         *        |     ＼                   ／
         *        |       edge            ／
         *        |           ＼  0 1  ／
         *        |              ＼ ／
         *        |              2 | 3
         *        |                |
         *        |                |
         *                         |
         *                         |
         * 
         */

        for (int i = 0; i < selectedEdges.Count; i++)
        {
            originalConnections[i] = new List<int>();
        }
        // Top-Down and Down-Up Connections
        for (int i = 0; i < selectedEdges.Count; i++)
        {
            // Save the Top-Down connections
            if (i % 4 < 2)
            {
                int topVert = selectedEdges[i];
                int topQuad = quads.IndexOf(topVert) / 4;
                for (int j = 0; j < connectedVerts[topVert].Count; j++)
                {
                    // For each of the verts linked to the leading point of the new face
                    // Save all non-selected connections to later apply them to the lower side of the new face
                    int connected = connectedVerts[topVert].list[j];
                    if (connected < vertexCount)
                    {
                        // If the vert belongs to a selected face, ignore it!
                        int quad = quads.IndexOf(connected) / 4;
                        if (!selectedFaces[quad] &&
                            connected != topVert) // <- for safety
                        {
                            originalConnections[i + 2].Add(connected);
                            // Reverse connection
                            connectedVerts[connected].Add(vertexCount + (i + 2));
                        }
                        else if (selectedFaces[quad])
                        {
                            // Other Down-Up connections
                            originalConnections[i].Add(connected);
                            // Reverse connection
                            connectedVerts[connected].Add(vertexCount + i);
                        }
                    }
                }
                // Finally, check to see if the leading face is selected! If it isn't, it's an edge extrusion where the top face must be added
                if (!selectedFaces[topQuad])
                {
                    originalConnections[i + 2].Add(topVert);
                    // Reverse connection
                    connectedVerts[topVert].Add(vertexCount + (i + 2));
                }

                if (selectedFaces[topQuad])
                {
                    // Save the main Down-Up connection
                    originalConnections[i].Add(topVert);
                    // Reverse connection
                    connectedVerts[topVert].Add(vertexCount + i);
                }
            }
        }

        // Lateral connections
        // If either top/bottom vert on a given side share a vert with a new edge, connect it on the top and bottom
        for (int i = 0; i < selectedEdges.Count; i++)
        {
            if (i % 4 > 1)
            {
                // Verts on underside of extrusion
                int iAlt = i;
                if (selectedEdges[i] < 0) { iAlt -= 2; }
                for (int ii = 0; ii < selectedEdges.Count; ii++)
                {
                    if (ii % 4 > 1)
                    {
                        if (i != ii)
                        {
                            int iiAlt = ii;
                            if (selectedEdges[ii] < 0) { iiAlt -= 2; }

                            // Checks if the faces that this edge was extruded from had any connections
                            if (isShared(selectedEdges[iAlt], selectedEdges[iiAlt]))
                            {
                                originalConnections[i].Add(vertexCount + ii);
                            }

                            // For the case of a lone plane being extruded and not having it's corners sewn together:
                            // If the underside has no connection and it was extruded from the same face, then sew them pre-emptively
                            if (selectedEdges[i] == -1 && selectedEdges[ii] == -1)
                            {
                                // Find which face the edge was extruded from originally, if they're the same then stitch the new face (lagging section)
                                //int faceI = / 4;
                                //int faceII =  / 4;
                                if (selectedEdges[ii - 2] == selectedEdges[i - 2])
                                {
                                    originalConnections[i].Add(vertexCount + ii);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Verts leading the extrusion
                for (int ii = 0; ii < selectedEdges.Count; ii++)
                {
                    if (ii % 4 < 2)
                    {
                        if (i != ii)
                        {
                            if (isShared(selectedEdges[i], selectedEdges[ii]) ||
                                selectedEdges[i] == selectedEdges[ii])
                            {
                                originalConnections[i].Add(vertexCount + ii);
                            }
                        }
                    }
                }
            }
        }

        DateTime after = DateTime.Now;

        #endregion

        #region Sever existing connections along the perimeter of the selection
        start = DateTime.Now;
        for (int i = 0; i < selectedEdges.Count; i++)
        {
            int leadingFace = faceRefs[(i / 4) * 2];
            // If the extrusion doesn't involve a face, no cuts need to be made
            if (selectedEdges[i] >= 0 &&
                leadingFace >= 0 &&
                selectedFaces[leadingFace])
            {
                // Selected nodes
                // The first two verts of each edge are a part of the selected face, so they need to be severed from any non-selected face.
                for (int ii = 0; ii < connectedVerts[selectedEdges[i]].Count; ii++)
                {
                    int q = quads.IndexOf(connectedVerts[selectedEdges[i]].list[ii]) / 4;
                    // We added a bunch of connections to verts that don't exist yet, so ignore any verts that are over the number of verts
                    if (q >= 0 &&
                        connectedVerts[selectedEdges[i]].list[ii] < vertexCount)
                    {
                        if (i % 4 < 2)
                        {
                            // Selected faces
                            if (!selectedFaces[q])
                            {
                                connectedVerts[connectedVerts[selectedEdges[i]].list[ii]].Remove(selectedEdges[i]);

                                connectedVerts[selectedEdges[i]].RemoveAt(ii);
                                ii--;
                            }
                        }
                        else
                        {
                            // Non-selected faces
                            if (selectedFaces[q])
                            {
                                connectedVerts[connectedVerts[selectedEdges[i]].list[ii]].Remove(selectedEdges[i]);
                                connectedVerts[selectedEdges[i]].RemoveAt(ii);
                                ii--;
                            }
                        }
                    }
                }
            }
        }
        after = DateTime.Now;

        #endregion

        #region Create new quads along the perimeter of the selection
        start = DateTime.Now;
        for (int i = 0; i < selectedEdges.Count; i += 4)
        {

            int f = faceRefs[i / 2];

            bool isFaceSelected = false;
            if (f >= 0)
            {
                isFaceSelected = selectedFaces[f];
            }
            int c = selectedEdges[i + 2];
            int d = selectedEdges[i + 3];
            if (c < 0 || d < 0)
            {
                c = selectedEdges[i + 0];
                d = selectedEdges[i + 1];
            }
            addQuadBetweenFaces(selectedEdges[i], selectedEdges[i + 1], selectedEdges[i + 2], selectedEdges[i + 3], direction, f, isFaceSelected);

        }
        after = DateTime.Now;

        #endregion

        #region Add saved connections to the new set of verts
        start = DateTime.Now;
        for (int i = 0; i < verts.Count - vertexCount; i++)
        {
            for (int j = 0; j < originalConnections[i].Count; j++)
            {
                if (!connectedVerts[vertexCount + i].Contains(originalConnections[i][j]))
                {
                    connectedVerts[vertexCount + i].Add(originalConnections[i][j]);
                }
            }
        }
        after = DateTime.Now;

        #endregion

        start = DateTime.Now;
        //shareConnectedVerts();
        after = DateTime.Now;


        // Each of the four vertices that make up a plane will be physically touching 0 or more other vertices from adjacent quads (unless the the quad is a plane)
        // Count the overlapping number for each vert on the selected section, and then compare to the number when moved. If the value is LOWER then a quad must be created using that point
        int[] vertOverlap = new int[selectedVerts.Length];
        int[] selectedVertOverlap = new int[selectedVerts.Length];
        for (int i = 0; i < selectedVerts.Length; i++)
        {
            if (selectedVerts[i])
            {
                for (int ii = 0; ii < selectedVerts.Length; ii++)
                {
                    if (ii != i &&
                        connectedVerts[ii].Contains(i))
                    //verts[ii] == verts[i])
                    {
                        vertOverlap[i]++;

                        if (selectedVerts[ii])
                        {
                            selectedVertOverlap[i]++;
                        }
                    }
                }
            }
        }

        // Extrude doesn't just create new faces that require building, they also modify the adjacency of pre-existsing faces. So the only EASY method is to rebuild the entire list
        rebuildAdjacentFaces(0);
        bleedInwardAllEdgeValues(faceCount);
        //defineAllHalfEdges();

        int vertCount = verts.Count;
        bool[] selectedVertsUpdated = new bool[vertCount];


        bool[] centeredSelectedFaces = new bool[selectedVerts.Length];
        for (int i = 0; i < selectedVerts.Length; i++)
        {
            if (selectedVertOverlap[i] == vertOverlap[i])
            {
                selectedVerts[i] = false;
                centeredSelectedFaces[i] = selectedVertOverlap[i] > 0;
            }
        }





        for (int i = 0; i < selectedVertsUpdated.Length; i++)
        {
            if (i < selectedVerts.Length)
            {
                selectedVertsUpdated[i] = selectedVerts[i];
            }
            else
            {
                // By selecting every second new vertice in this pattern, you end up grabbing the face-touching edge that lets you stretch the plane out during extrusion.
                selectedVertsUpdated[i] = ((i - selectedVerts.Length) % 4 == 0 || (i - selectedVerts.Length) % 4 == 1);
                //selectedVertsUpdated[i] = ((i - selectedVerts.Length) % 4 == 0 || (i - selectedVerts.Length) % 4 == 3);
            }
        }
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedFaces[i])
            {
                selectedVertsUpdated[i * 4 + 0] = true;
                selectedVertsUpdated[i * 4 + 1] = true;
                selectedVertsUpdated[i * 4 + 2] = true;
                selectedVertsUpdated[i * 4 + 3] = true;
            }
        }

        start = DateTime.Now;
        pushNewGeometry();
        after = DateTime.Now;

        return selectedVertsUpdated;
    }

    public void separate(bool[] selectedFaces)
    {

    }

    public void join(bool[] selectedVerts)
    {
        Vector3 average = Vector3.zero;
        int selectedCount = 0;
        for (int i = 0; i < selectedVerts.Length; i++)
        {
            if (selectedVerts[i])
            {
                average += verts[i];
                selectedCount++;
                for (int ii = 0; ii < selectedVerts.Length; ii++)
                {
                    if (ii != i)
                    {
                        if (!connectedVerts[i].Contains(ii))
                        {
                            connectedVerts[i].Add(ii);
                        }
                    }
                }
            }
        }
        average /= selectedCount;
        for (int i = 0; i < selectedVerts.Length; i++)
        {
            verts[i] = average;
        }
        pushNewGeometry();

    }


    public void delete(bool[] selectedVerts, bool[] selectedFaces)
    {
        beginTimer("Delete");
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            selectedFaces[i] = selectedFaces[i] ||
                (selectedVerts[quads[i * 4 + 0]] ||
                selectedVerts[quads[i * 4 + 1]] ||
                selectedVerts[quads[i * 4 + 2]] ||
                selectedVerts[quads[i * 4 + 3]]);
        }



        bool[] vertDelete = new bool[verts.Count];
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedFaces[i])
            {
                for (int ii = 0; ii < 4; ii++)
                {
                    vertDelete[i * 4 + ii] = true;
                }
            }
        }

        bool[] quadDelete = new bool[quads.Count / 4];
        bool[] triDelete = new bool[tris.Count / 3];

        List<int> uvsToRemove = new List<int>();

        beginTimer("Delete - General Operations");

        for (int i = vertDelete.Length - 1; i >= 0; i--)
        {
            if (vertDelete[i])
            {
                // Connected Verts
                for (int c = 0; c < connectedVerts.Count; c++)
                {
                    for (int cc = connectedVerts[c].Count - 1; cc >= 0; cc--)
                    {
                        if (connectedVerts[c].list[cc] == i)
                        {
                            connectedVerts[c].RemoveAt(cc);
                        }
                        else
                        {
                            if (connectedVerts[c].list[cc] > i)
                            {
                                connectedVerts[c].list[cc]--;
                            }
                        }
                    }
                }

                connectedVerts.RemoveAt(i);
                // Colour
                colours.RemoveAt(i);
                // Vert
                verts.RemoveAt(i);
                // Normal at vert
                vertNormals.RemoveAt(i);
                meshNormals.RemoveAt(i);
                // UVs
                customTextureUVMap.RemoveAt(i);
                uvsToRemove.Add(i);

                // Quads
                for (int q = 0; q < quads.Count; q += 4)
                {
                    for (int qi = 0; qi < 4; qi++)
                    {
                        if (q + qi >= quads.Count)
                        {

                        }
                        if (quads[q + qi] == i)
                        {
                            quadDelete[q / 4] = true;
                        }
                        else if (quads[q + qi] > i)
                        {
                            quads[q + qi]--;
                        }
                    }
                }
                // Tris
                for (int t = 0; t < tris.Count; t += 3)
                {
                    for (int ti = 0; ti < 3; ti++)
                    {
                        if (tris[t + ti] == i)
                        {
                            triDelete[t / 3] = true;
                        }
                        else if (tris[t + ti] > i)
                        {
                            tris[t + ti]--;
                        }
                    }
                }
            }
        }

        endTimer();

        beginTimer("Delete - UVMaps");
        for (int u = 0; u < uvMaps.Count; u++)
        {
            uvMaps[u].removeVerts(uvsToRemove);
        }
        endTimer();

        beginTimer("Delete - Adjacent face operations");
        // Remove adjacency references
        if (adjacentFaces != null)
        {
            for (int i = quadDelete.Length - 1; i >= 0; i--)
            {
                if (quadDelete[i])
                {
                    for (int j = 0; j < adjacentFaces.Count; j++)
                    {
                        for (int jj = 0; jj < adjacentFaces[j].Count; jj++)
                        {
                            // Delete face reference if its going to be deleted
                            if (adjacentFaces[j].list[jj] == i)
                            {
                                adjacentFaces[j].list[jj] = -1;
                            }
                            // Decrement face indexes
                            else if (adjacentFaces[j].list[jj] > i)
                            {
                                adjacentFaces[j].list[jj]--;
                            }
                        }
                    }
                }
            }
        }
        endTimer();

        beginTimer("Delete - Final delete");
        for (int i = quadDelete.Length - 1; i >= 0; i--)
        {
            if (quadDelete[i])
            {
                quads.RemoveAt(i * 4 + 3);
                quads.RemoveAt(i * 4 + 2);
                quads.RemoveAt(i * 4 + 1);
                quads.RemoveAt(i * 4 + 0);
                faceNormals.RemoveAt(i);
                halfEdges.RemoveAt(i);
                quadMaterial.RemoveAt(i);

                if (adjacentFaces != null)
                {
                    adjacentFaces.RemoveAt(i);
                }
            }
        }
        for (int i = triDelete.Length - 1; i >= 0; i--)
        {
            if (triDelete[i])
            {
                tris.RemoveAt(i * 3 + 2);
                tris.RemoveAt(i * 3 + 1);
                tris.RemoveAt(i * 3 + 0);
            }
        }

        for (int u = 0; u < uvMaps.Count; u++)
        {
            uvMaps[u].vertCount = verts.Count;
        }
        
        
        endTimer();
        endTimer();

        pushNewGeometry();
    }

    public bool dissolve(bool[] selectedVerts, bool[] selectedFaces)
    {
        List<int> facePairs = new List<int>();
        List<int> newQuads = new List<int>();

        int faceCount = 0;
        int oldFaceCount = adjacentFaces.Count;

        for (int i = 0; i < faceNormals.Count; i++)
        {
            int faceA = i;

            // If faceA has no vert that is selected, then continue to the next face
            bool isFacePartSelected = false;
            for (int j = 0; j < 4; j++)
            {
                if (selectedVerts[quads[faceA * 4]])
                {
                    isFacePartSelected = true;
                    break;
                }
            }

            if (isFacePartSelected)
            {
                if (!facePairs.Contains(faceA))
                {
                    bool isAnyAdjacentFaceValid = false;


                    for (int j = 0; j < adjacentFaces[faceA].Count; j++)
                    {
                        int faceB = adjacentFaces[faceA].list[j];

                        if (!facePairs.Contains(faceB))
                        {
                            List<int> sharedVerts = getSharedVerts(faceA, faceB);


                            if (sharedVerts != null && sharedVerts.Count == 4)
                            {
                                isAnyAdjacentFaceValid = true;
                                bool isValid = true;
                                for (int k = 0; k < sharedVerts.Count; k++)
                                {
                                    if (!selectedVerts[sharedVerts[k]])
                                    {
                                        isValid = false;
                                        break;
                                    }
                                }
                                if (isValid)
                                {
                                    // Get the verts of the new quad                         
                                    int a;
                                    int b;
                                    int c;
                                    int d;

                                    getOppositeSideOfQuad(faceA, sharedVerts[0], sharedVerts[2], out a, out b);
                                    getOppositeSideOfQuad(faceB, sharedVerts[1], sharedVerts[3], out c, out d);

                                    newQuads.Add(a);
                                    newQuads.Add(b);
                                    newQuads.Add(c);
                                    newQuads.Add(d);
                                    facePairs.Add(faceA);
                                    facePairs.Add(faceB);
                                    faceCount++;

                                    for (int k = 0; k < sharedVerts.Count; k++)
                                    {
                                        // selectedVerts[sharedVerts[k]] = false;
                                    }
                                }
                            }
                        }
                    }


                    if (!isAnyAdjacentFaceValid)
                    {
                        return false;
                    }
                }
            }
            else
            {
                // Deselect all the verts so that they won't get in the way of future operations?
            }
        }


        int vertsBeforeCreation = verts.Count;

        for (int i = 0; i < faceCount; i++)
        {
            addQuadBetweenFaces(
                newQuads[i * 4 + 0],
                newQuads[i * 4 + 1],
                newQuads[i * 4 + 2],
                newQuads[i * 4 + 3],
                (faceNormals[facePairs[i * 2 + 0]] + faceNormals[facePairs[i * 2 + 1]]) / 2,
                new int[] {
                    newQuads[i * 4 + 0],
                    newQuads[i * 4 + 1],
                    newQuads[i * 4 + 2],
                    newQuads[i * 4 + 3] });
        }


        int vertCount = verts.Count;


        delete(selectedVerts, selectedFaces);
        int vertsDeleted = vertCount - verts.Count;



        for (int i = verts.Count - faceCount * 4; i < verts.Count; i++)
        {
            cleanVertConnections(i);
        }

        rebuildAdjacentFaces(0);
        bleedInwardAllEdgeValues(oldFaceCount - vertsDeleted / 4);

        return true;
    }

    public void cleanVertConnections(int vert)
    {
        beginTimer("cleanVertConnections");
        for (int i = connectedVerts[vert].Count - 1; i >= 0; i--)
        {
            // Delete all self-references
            int v = connectedVerts[vert].list[i];
            if (v == vert)
            {
                connectedVerts[vert].RemoveAt(i);
                continue;
            }
        }

        int j = 0;
        while (j < connectedVerts[vert].Count)
        {
            int ii = j + 1;
            while (ii < connectedVerts[vert].Count)
            {
                if (connectedVerts[vert].list[j] == connectedVerts[vert].list[ii])
                {
                    connectedVerts[vert].RemoveAt(ii);
                }
                ii++;
            }
            j++;
        }
        endTimer();
    }

    [SerializeField, HideInInspector]
    public bool transformsApplied;

    public void checkTransforms()
    {
        Vector3 p = gameObject.transform.position;
        Quaternion r = gameObject.transform.rotation;
        Vector3 s = gameObject.transform.lossyScale;

        bool update = false;
        bool updateP = false, updateR = false, updateS = false;
        Vector3 offsetP = Vector3.zero;
        Quaternion offsetR = Quaternion.identity;
        Vector3 offsetS = Vector3.zero;
        if (p != editorPosition)
        {
            offsetP = editorPosition - p;
            editorPosition = p;
            update = true;
            updateP = true;
        }
        if (s != editorScale)
        {
            offsetS.x = s.x / editorScale.x;
            offsetS.y = s.y / editorScale.y;
            offsetS.z = s.z / editorScale.z;
            editorScale = s;
            update = true;
            updateS = true;
        }
        if (r != editorRotation)
        {
            offsetR = editorRotation;
            editorRotation = r;

            update = true;
            updateR = true;
        }
        if (update)
        {
            for (int i = 0; i < verts.Count; i++)
            {
                if (updateP)
                {
                    verts[i] -= offsetP;
                }
                if (updateS)
                {
                    verts[i] -= p;
                    verts[i] = Quaternion.Inverse(r) * verts[i];
                    verts[i] = new Vector3(
                        verts[i].x * offsetS.x,
                        verts[i].y * offsetS.y,
                        verts[i].z * offsetS.z);
                    verts[i] = r * verts[i];
                    verts[i] += p;
                }
                if (updateR)
                {
                    verts[i] -= p;
                    verts[i] = Quaternion.Inverse(offsetR) * verts[i];
                    verts[i] = editorRotation * verts[i];
                    verts[i] += p;
                }
            }

        }

        
        if ((update || !transformsApplied)) // && !isSelected)
        {
            applyModifiers(true);
        }
    }

    // Stub class, used to block the other references when MeshEdit is exported
    private class MeshMod : MonoBehaviour
    {
        public void applyAllMods()
        {

        }
    }

    public void applyModifiers(bool forceUpdate)
    {
        MeshMod[] mods = gameObject.GetComponents<MeshMod>();
        if (mods.Length > 0)
        {
            mods[0].applyAllMods();
        }
        /*
        deepCopyLocalMeshToModifiedMesh();

        for (int i = 0; i < mods.Length; i++)
        {
            if (mods[i].enabled)
            {
                mods[i].apply(meshModified);
            }
        }
        pushModifiedMeshToGameObject();
        */
        transformsApplied = true;

    }

    // v0.3
    public void deepCopyMesh(ref Mesh srcMesh, ref Mesh dstMesh)
    {
        dstMesh = new Mesh();

        Vector3[] vertices = new Vector3[srcMesh.vertexCount];
        Vector3[] normals = new Vector3[srcMesh.normals.Length];
        Vector2[] uvs = new Vector2[srcMesh.uv.Length];
        int[] triangles = new int[srcMesh.triangles.Length];
        Color[] colours = new Color[srcMesh.colors.Length];

        for (int i = 0; i < srcMesh.vertices.Length; i++)
        {
            vertices[i] = (srcMesh.vertices[i]);
            normals[i] = (srcMesh.normals[i]);
            uvs[i] = (srcMesh.uv[i]);
            colours[i] = (srcMesh.colors[i]);
        }
        for (int i = 0; i < srcMesh.triangles.Length; i++)
        {
            triangles[i] = (srcMesh.triangles[i]);
        }

        dstMesh.vertices = vertices;
        dstMesh.normals = normals;
        dstMesh.uv = uvs;
        dstMesh.colors = colours;

        dstMesh.subMeshCount = srcMesh.subMeshCount;
        for (int i = 0; i < srcMesh.subMeshCount; i++)
        {
            dstMesh.SetTriangles(srcMesh.GetTriangles(i), i);
        }
        //dstMesh.triangles = triangles;

        dstMesh.RecalculateBounds();
        dstMesh.RecalculateNormals();
    }

    // v0.3
    public MeshEdit deepCopyMeshEditScript(MeshEdit meshEdit)
    {
        meshEdit.version = version;

        // TO COPY
        #region Verts, Quads, ConnectedVerts, VertNormals, Colours
        meshEdit.verts = new List<Vector3>();
        meshEdit.quads = new List<int>();
        meshEdit.connectedVerts = new List<ListWrapper>();
        meshEdit.colours = new List<Color>();
        meshEdit.vertNormals = new List<Vector3>();
        meshEdit.meshNormals = new List<Vector3>();
        meshEdit.customTextureUVMap = new List<Vector2>();

        for (int i = 0; i < verts.Count; i++)
        {
            meshEdit.verts.Add(new Vector3(verts[i].x, verts[i].y, verts[i].z));
            meshEdit.quads.Add(quads[i]);
            meshEdit.connectedVerts.Add(connectedVerts[i].deepCopy());
            meshEdit.colours.Add(colours[i]);
            meshEdit.vertNormals.Add(vertNormals[i]);
            meshEdit.meshNormals.Add(meshNormals[i]);
            meshEdit.customTextureUVMap.Add(customTextureUVMap[i]);
        }
        #endregion


        #region FaceNormals, AdjacentFaces, QuadMaterials
        meshEdit.faceNormals = new List<Vector3>();
        meshEdit._adjacentFaces = new List<ListWrapper>();
        meshEdit.quadMaterial = new List<int>();
        meshEdit.halfEdges = new List<MeshEditFaceEdges>();

        for (int i = 0; i < faceNormals.Count; i++)
        {
            meshEdit.faceNormals.Add(faceNormals[i]);
            meshEdit._adjacentFaces.Add(adjacentFaces[i].deepCopy());
            meshEdit.quadMaterial.Add(quadMaterial[i]);
            meshEdit.halfEdges.Add(halfEdges[i].deepCopy());
        }
        #endregion
        #region Tris
        meshEdit.tris = new List<int>();
        
        for (int i = 0; i < tris.Count; i++)
        {
            meshEdit.tris.Add(tris[i]);
        }
        #endregion
        
        #region UVMaps
        meshEdit.uvMaps = new List<UVData>();
        for (int i = 0; i < uvMaps.Count;i++)
        {
            meshEdit.uvMaps.Add(uvMaps[i].deepCopy());
        }
        #endregion
        
        
        #region Materials, MaterialUVMap, SelectedMaterial

        meshEdit.materials = new List<Material>();
        meshEdit.materialUVMap = new List<int>(); 
        editorInfo.customTilesetSettings = new List<EditorInfo.CustomTilesetEditorSettings>();

        for (int i = 0; i < materials.Count; i++)
        {
            editorInfo.customTilesetSettings.Add(new EditorInfo.CustomTilesetEditorSettings());
            meshEdit.materials.Add(Instantiate(materials[i]));
            meshEdit.materialUVMap.Add(materialUVMap[i]);
        }
        meshEdit.selectedMaterial = selectedMaterial;

        #endregion
        
        // Version
        #region DefaultUVs / hasDefaultUVs

        if (defaultUVs != null)
        {
            meshEdit.defaultUVs = new Vector2[defaultUVs.Length];
            for (int i = 0; i < defaultUVs.Length; i++)
            {
                meshEdit.defaultUVs[i] = defaultUVs[i];
            }
        }
        meshEdit.hasDefaultUVs = hasDefaultUVs;
        #endregion
        #region LastTilesetUsed

        meshEdit.selectedTileset = selectedTileset;
        #endregion
        // TO BUILD

        // Mesh
        
        return meshEdit;
    }


    public void shareConnectedVerts()
    {
        for (int i = 0; i < connectedVerts.Count; i++)
        {
            bool isShared = false;

            while (!isShared)
            {
                isShared = true;
                for (int ii = 0; ii < connectedVerts.Count; ii++)
                {
                    if (ii != i)
                    {
                        if (connectedVerts[ii].Contains(i) ||
                            connectedVerts[i].Contains(ii))
                        {
                            if (!connectedVerts[i].Contains(ii))
                            {
                                isShared = false;
                                connectedVerts[i].Add(ii);
                            }
                            if (!connectedVerts[ii].Contains(i))
                            {
                                isShared = false;
                                connectedVerts[ii].Add(i);
                            }

                            for (int j = 0; j < connectedVerts[ii].Count; j++)
                            {
                                if (connectedVerts[ii].list[j] != i &&
                                    !connectedVerts[i].Contains(connectedVerts[ii].list[j]))
                                {
                                    isShared = false;
                                    connectedVerts[i].Add(connectedVerts[ii].list[j]);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    /*
    public List<int> getSharedVerts(int vert, List<int> sharedVerts = null)
    {
        if (sharedVerts == null)
        {
            sharedVerts = new List<int>();
            sharedVerts.Add(vert);
        }
        {
            for (int i = 0; i < connectedVerts[i].Count; i++)
            {
                if (vert != i)
                {
                    getSharedVerts
                }
            }
        }
    }*/

    public void recenterPivot(Vector3 normalisedCenter)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        for (int i = 0; i < verts.Count; i++)
        {
            if (verts[i].x < minX)
            {
                minX = verts[i].x;
            }
            if (verts[i].y < minY)
            {
                minY = verts[i].y;
            }
            if (verts[i].z < minZ)
            {
                minZ = verts[i].z;
            }
            if (verts[i].x > maxX)
            {
                maxX = verts[i].x;
            }
            if (verts[i].y > maxY)
            {
                maxY = verts[i].y;
            }
            if (verts[i].z > maxZ)
            {
                maxZ = verts[i].z;
            }
        }

        Vector3 centerPoint = new Vector3(
            minX + (maxX - minX) * normalisedCenter.x,
            minY + (maxY - minY) * normalisedCenter.y,
            minZ + (maxZ - minZ) * normalisedCenter.z);


        gameObject.transform.position = centerPoint;
        
        // EditorPosition has to be updated as well!
        editorPosition = gameObject.transform.position;

        pushNewGeometry();
    }

    public void addQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 n)
    {
    }

    public bool isVertCovered(int vert)
    {
        return isVertCovered(verts[vert]);
    }

    public bool isVertCovered(Vector3 vert)
    {
        bool isCovered = false;
        Vector3 p = vert;
        Ray r = new Ray(p, (SceneView.lastActiveSceneView.camera.transform.position - p));

        r = HandleUtility.GUIPointToWorldRay(HandleUtility.WorldToGUIPoint(p));
        float d2Vert = Vector3.Distance(r.origin, p);
        Vector3 intersection = Vector3.zero;

        Triangle t = new Triangle(Vector3.zero, Vector3.zero, Vector3.zero);
        for (int i = 0; i < quads.Count; i += 4)
        {
            t.a = verts[quads[i + 0]];
            t.b = verts[quads[i + 1]];
            t.c = verts[quads[i + 2]];
            if (intersectTriangleRay(t, r, out intersection))
            {
                if (Vector3.Distance(intersection, p) > 0.000001f &&
                    Vector3.Distance(intersection, SceneView.lastActiveSceneView.camera.transform.position) < d2Vert)
                {
                    isCovered = true;
                    break;
                }
            }

            t.a = verts[quads[i + 3]];
            t.b = verts[quads[i + 1]];
            t.c = verts[quads[i + 2]];
            if (intersectTriangleRay(t, r, out intersection))
            {
                if (Vector3.Distance(intersection, p) > 0.000001f &&
                    Vector3.Distance(intersection, SceneView.lastActiveSceneView.camera.transform.position) < d2Vert)
                {
                    isCovered = true;
                    break;
                }
            }
        }

        return isCovered;
    }

    public bool isFaceCovered(int face)
    {
        return isFaceCovered(face, quadCenter(face));
    }

    public bool isFaceCovered(int face, Vector3 pointOnFace)
    {
        bool isCovered = false;
        Vector3 p = pointOnFace;
        Ray r = new Ray(SceneView.lastActiveSceneView.camera.transform.position, (p - SceneView.lastActiveSceneView.camera.transform.position).normalized);
        r = HandleUtility.GUIPointToWorldRay(HandleUtility.WorldToGUIPoint(p));
        float dd = (SceneView.lastActiveSceneView.camera.transform.position - p).sqrMagnitude;

        Vector3 intersection = Vector3.zero;

        Triangle t = new Triangle(Vector3.zero, Vector3.zero, Vector3.zero);
        for (int i = 0; i < quads.Count; i += 4)
        {
            if (4 * face != i)
            {
                t.a = verts[quads[i + 0]];
                t.b = verts[quads[i + 1]];
                t.c = verts[quads[i + 2]];
                if (intersectTriangleRay(t, r, out intersection))
                {
                    float d2 = (intersection - SceneView.lastActiveSceneView.camera.transform.position).sqrMagnitude;
                    if (d2 < dd)
                    {
                        isCovered = true;
                        break;
                    }
                }

                t.a = verts[quads[i + 3]];
                t.b = verts[quads[i + 1]];
                t.c = verts[quads[i + 2]];
                if (intersectTriangleRay(t, r, out intersection))
                {

                    float d2 = (intersection - SceneView.lastActiveSceneView.camera.transform.position).sqrMagnitude;
                    if (d2 < dd)
                    {
                        isCovered = true;
                        break;
                    }
                }
            }
        }

        return isCovered;
    }

    // Used for extrusion
    public void addQuadBetweenFaces(int v0, int v1, int v2, int v3, Vector3 n, int faceReference, bool isFaceSelected, bool attachQuads = false)
    {
        int v2Temp = v2;
        int v3Temp = v3;
        if (v2 == -1)
        {
            v2 = v0;
        }
        if (v3 == -1)
        {
            v3 = v1;
        }
        Vector3 nCenter = Vector3.zero;

        if (faceReference >= 0)
        {
            nCenter = quadCenter(faceReference);
        }


        // "n" refers to the wrong face
        int vertCount = verts.Count;

        verts.Add(verts[v0]);
        verts.Add(verts[v1]);
        verts.Add(verts[v2]);
        verts.Add(verts[v3]);

        colours.Add(colours[v0]);
        colours.Add(colours[v1]);
        colours.Add(colours[v2]);
        colours.Add(colours[v3]);

        if (attachQuads)
        {
            if (isFaceSelected)
            {
                connectedVerts[v0].Add(vertCount + 0);
                connectedVerts[v1].Add(vertCount + 1);
                connectedVerts.Add(new ListWrapper(new List<int>(new int[] { v0 })));
                connectedVerts.Add(new ListWrapper(new List<int>(new int[] { v1 })));
            }
            else
            {
                connectedVerts.Add(new ListWrapper(new List<int>(new int[] { })));
                connectedVerts.Add(new ListWrapper(new List<int>(new int[] { })));
            }
            if (v2Temp >= 0 && v3Temp >= 0)
            {
                connectedVerts[v2].Add(vertCount + 2);
                connectedVerts[v3].Add(vertCount + 3);
                connectedVerts.Add(new ListWrapper(new List<int>(new int[] { v2 })));
                connectedVerts.Add(new ListWrapper(new List<int>(new int[] { v3 })));
            }
            else
            {
                connectedVerts[v2].Add(vertCount + 2);
                connectedVerts[v3].Add(vertCount + 3);
                connectedVerts.Add(new ListWrapper(new List<int>(new int[] { })));
                connectedVerts.Add(new ListWrapper(new List<int>(new int[] { })));
            }
        }
        else
        {
            connectedVerts.Add(new ListWrapper(new List<int>(new int[] { })));
            connectedVerts.Add(new ListWrapper(new List<int>(new int[] { })));
            connectedVerts.Add(new ListWrapper(new List<int>(new int[] { })));
            connectedVerts.Add(new ListWrapper(new List<int>(new int[] { })));
        }

        tris.Add(vertCount + 0);
        tris.Add(vertCount + 1);
        tris.Add(vertCount + 2);
        tris.Add(vertCount + 3);
        tris.Add(vertCount + 2);
        tris.Add(vertCount + 1);

        quads.Add(vertCount + 0);
        quads.Add(vertCount + 1);
        quads.Add(vertCount + 2);
        quads.Add(vertCount + 3);

        quadMaterial.Add(0);

        Vector3 a = verts[v0] + n;
        Vector3 b = verts[v1] + n;
        Vector3 c = verts[v2];
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 proofNormal = Vector3.Cross(ab, ac);
        if ((v0 == v2 || v1 == v3) &&
            !isFaceSelected)
        {
            float dot = Vector3.Dot(SceneView.lastActiveSceneView.camera.transform.forward, proofNormal);

            Vector3 s = transform.lossyScale;
            if (s.x * s.y * s.z < 0)
            {
                dot *= -1;
            }
            if (dot > 0)
            {
                proofNormal *= -1;
                int tc = tris.Count;
                int temp = tris[tc - 1];
                tris[tc - 1] = tris[tc - 2];
                tris[tc - 2] = temp;

                temp = tris[tc - 4];
                tris[tc - 4] = tris[tc - 5];
                tris[tc - 5] = temp;

                int qc = quads.Count;
                temp = quads[qc - 3];
                quads[qc - 3] = quads[qc - 2];
                quads[qc - 2] = temp;

            }
        }
        else// if (geometryBasedFlip)
        {
            Vector3 compareNormal = (verts[v1] + verts[v0]) / 2 - nCenter;

            if (Vector3.Dot(proofNormal, compareNormal) < 0)
            {
            }
            Vector3 midPoint = (verts[v0] + n + verts[v1] + n) * 0.5f;
            Vector3 centerOfNewFace = (verts[v0] + n + verts[v1] + n + verts[v2] + verts[v3]) * 0.25f;

            Vector3 crossReference = Vector3.Cross(midPoint - (nCenter + n), n);
            Vector3 crossNew = Vector3.Cross(midPoint - centerOfNewFace, proofNormal);

            float dot = Vector3.Dot(crossReference, crossNew);

            //Vector3 quessNormal = (vertNormals[v0] + vertNormals[v1] + vertNormals[v2] + n + vertNormals[v3] + n);
            //dot = Vector3.Dot(quessNormal, proofNormal);
            Vector3 s = transform.lossyScale;
            if (s.x * s.y * s.z < 0)
            {
                dot *= -1;
            }
            if (dot > 0)
            {
                proofNormal *= -1;
                int tc = tris.Count;
                int temp = tris[tc - 1];
                tris[tc - 1] = tris[tc - 2];
                tris[tc - 2] = temp;

                temp = tris[tc - 4];
                tris[tc - 4] = tris[tc - 5];
                tris[tc - 5] = temp;

                int qc = quads.Count;
                temp = quads[qc - 3];
                quads[qc - 3] = quads[qc - 2];
                quads[qc - 2] = temp;
            }
        }


        vertNormals.Add(Vector3.zero);
        vertNormals.Add(Vector3.zero);
        vertNormals.Add(Vector3.zero);
        vertNormals.Add(Vector3.zero);

        meshNormals.Add(Vector3.zero);
        meshNormals.Add(Vector3.zero);
        meshNormals.Add(Vector3.zero);
        meshNormals.Add(Vector3.zero);

        faceNormals.Add(proofNormal);
        halfEdges.Add(new MeshEditFaceEdges());

        // Resize all data structures representing this mesh to fit the new mesh size.
        
        int quadReference = (quads.IndexOf(v0) / 4);
        int uv0 = v0, uv1 = v1, uv2 = -1, uv3 = -1;
        getOppositeSideOfQuad(quadReference, uv0, uv1, out uv2, out uv3);

        for (int i = 0; i < uvMaps.Count; i++)
        {
            uvMaps[i].resizeUVLength(vertCount + 4, new int[] { uv0, uv1, uv2, uv3 });//, newVerts);
        }

        customTextureUVMap.Add(Vector2.zero);
        customTextureUVMap.Add(Vector2.zero);
        customTextureUVMap.Add(Vector2.zero);
        customTextureUVMap.Add(Vector2.zero);
    }

    // Used for add quad
    public void addQuadBetweenFaces(int v0, int v1, int v2, int v3, Vector3 normal = default(Vector3), int[] uvs = null)
    {
        Vector3 crossA = Vector3.zero;
        Vector3 crossB = Vector3.zero;
        int[] newQuad = { v0, v1, v2, v3 };
        int[] bestQuad = new int[4];
        float crossLength = float.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            crossA = Vector3.Cross(
                verts[newQuad[1]] - verts[newQuad[0]],
                verts[newQuad[2]] - verts[newQuad[0]]);
            crossB = Vector3.Cross(
                verts[newQuad[2]] - verts[newQuad[3]],
                verts[newQuad[1]] - verts[newQuad[3]]);

            if (Vector3.Dot(crossA, crossB) > 0)
            {
                float l = Vector3.Cross(crossA, crossB).magnitude;

                if (l < crossLength)
                {
                    crossLength = l;
                    bestQuad = new int[] { newQuad[0], newQuad[1], newQuad[2], newQuad[3] };
                }
            }

            // Rotate the last three items in newQuad 
            int temp = newQuad[1];
            newQuad[1] = newQuad[2];
            newQuad[2] = newQuad[3];
            newQuad[3] = temp;

        }

        v0 = bestQuad[0];
        v1 = bestQuad[1];
        v2 = bestQuad[2];
        v3 = bestQuad[3];

        Vector3 nCenter = Vector3.zero;
        bool findNormal = (normal == default(Vector3));

        // "n" refers to the wrong face
        int vertCount = verts.Count;

        verts.Add(verts[v0]);
        verts.Add(verts[v1]);
        verts.Add(verts[v2]);
        verts.Add(verts[v3]);

        colours.Add(colours[v0]);
        colours.Add(colours[v1]);
        colours.Add(colours[v2]);
        colours.Add(colours[v3]);

        connectedVerts[v0].Add(vertCount + 0);
        connectedVerts[v1].Add(vertCount + 1);
        connectedVerts.Add(new ListWrapper(new List<int>(new int[] { v0 })));
        connectedVerts.Add(new ListWrapper(new List<int>(new int[] { v1 })));
        connectedVerts[v2].Add(vertCount + 2);
        connectedVerts[v3].Add(vertCount + 3);
        connectedVerts.Add(new ListWrapper(new List<int>(new int[] { v2 })));
        connectedVerts.Add(new ListWrapper(new List<int>(new int[] { v3 })));
        for (int i = 0; i < 4; i++)
        {
            int vert = v0;
            if (i == 1) { vert = v1; }
            if (i == 2) { vert = v2; }
            if (i == 3) { vert = v3; }

            for (int ii = 0; ii < connectedVerts[vert].Count; ii++)
            {
                connectedVerts[vertCount + i].Add(connectedVerts[vert].list[ii]);
                connectedVerts[connectedVerts[vert].list[ii]].Add(vertCount + i);
            }
        }

        tris.Add(vertCount + 0);
        tris.Add(vertCount + 1);
        tris.Add(vertCount + 2);
        tris.Add(vertCount + 3);
        tris.Add(vertCount + 2);
        tris.Add(vertCount + 1);

        // Flip to face the camera
        Vector3 defaultNormal = Vector3.Cross(
            verts[v1] - verts[v0],
            verts[v2] - verts[v0]);
        if (findNormal)
        {
            normal = SceneView.lastActiveSceneView.camera.transform.forward;
        }
        else
        {
            normal *= -1;
        }

        quads.Add(vertCount + 0);
        quads.Add(vertCount + 1);
        quads.Add(vertCount + 2);
        quads.Add(vertCount + 3);

        quadMaterial.Add(0);

        float dot = Vector3.Dot(normal, defaultNormal);
        Vector3 s = transform.lossyScale;
        if (s.x * s.y * s.z < 0)
        {
            dot *= -1;
        }
        if (dot > 0)
        {
            // TODO: This could be causing problems. The normal is defined by the triangle but nothing's stopping it from being reconstructed in the wrong way. This operation should
            // flip the quad order as well, and instead of flipping the two end verts of the triangle, it should swap their directions altogether
            defaultNormal *= -1;
            int tc = tris.Count;
            int temp = tris[tc - 1];
            tris[tc - 1] = tris[tc - 2];
            tris[tc - 2] = temp;

            temp = tris[tc - 4];
            tris[tc - 4] = tris[tc - 5];
            tris[tc - 5] = temp;

            int qc = quads.Count;
            temp = quads[qc - 3];
            quads[qc - 3] = quads[qc - 2];
            quads[qc - 2] = temp;

        }

        vertNormals.Add(defaultNormal);
        vertNormals.Add(defaultNormal);
        vertNormals.Add(defaultNormal);
        vertNormals.Add(defaultNormal);

        meshNormals.Add(defaultNormal);
        meshNormals.Add(defaultNormal);
        meshNormals.Add(defaultNormal);
        meshNormals.Add(defaultNormal);

        faceNormals.Add(defaultNormal);
        halfEdges.Add(new MeshEditFaceEdges());

        // Resize all data structures representing this mesh to fit the new mesh size.


        int quadReference = (quads.IndexOf(v0) / 4);
        int uv0 = v0, uv1 = v1, uv2 = -1, uv3 = -1;
        getOppositeSideOfQuad(quadReference, uv0, uv1, out uv2, out uv3);

        for (int i = 0; i < uvMaps.Count; i++)
        {
            uvMaps[i].resizeUVLength(vertCount + 4, uvs);
        }

        customTextureUVMap.Add(Vector2.zero);
        customTextureUVMap.Add(Vector2.zero);
        customTextureUVMap.Add(Vector2.zero);
        customTextureUVMap.Add(Vector2.zero);

        // Update adjacency
        int adjacentIndex = adjacentFaces.Count;
        adjacentFaces.Add(new ListWrapper(new int[] { -1, -1, -1, -1 }));
        //getfa

    }
    public void addQuadBetweenFaces(int v0, int v1, int v2)
    {
        Vector3 vDir = Vector3.zero;
        Vector3 newVert = Vector3.zero;

        int
            elbow = -1,
            hand = -1,
            shoulder = -1;

        if (areVertsSharingAnEdge(v0, v1))
        {
            shoulder = v2;

            hand = v1;
            elbow = v0;

        }
        else if (areVertsSharingAnEdge(v0, v2))
        {
            shoulder = v1;

            hand = v2;
            elbow = v0;

        }
        else if (areVertsSharingAnEdge(v1, v2))
        {
            shoulder = v0;

            hand = v2;
            elbow = v1;

        }

        vDir = verts[hand] - verts[elbow];

        newVert = verts[shoulder] + vDir;
        if (Vector3.Dot(verts[hand] - newVert, vDir) < 0)
        {
            newVert = verts[shoulder] - vDir;
            int temp = hand;
            hand = elbow;
            elbow = temp;
        }

        // "n" refers to the wrong face
        int vertCount = verts.Count;

        verts.Add(verts[elbow]);
        verts.Add(verts[hand]);
        verts.Add(verts[shoulder]);
        verts.Add(newVert);

        colours.Add(colours[elbow]);
        colours.Add(colours[hand]);
        colours.Add(colours[shoulder]);
        colours.Add(Color.white);

        connectedVerts[elbow].Add(vertCount + 0);
        connectedVerts[hand].Add(vertCount + 1);
        connectedVerts.Add(new ListWrapper(new List<int>(new int[] { elbow })));
        connectedVerts.Add(new ListWrapper(new List<int>(new int[] { hand })));
        connectedVerts[shoulder].Add(vertCount + 2);
        connectedVerts.Add(new ListWrapper(new List<int>(new int[] { shoulder })));
        connectedVerts.Add(new ListWrapper());
        for (int i = 0; i < 3; i++)
        {
            int vert = elbow;
            if (i == 1) { vert = hand; }
            if (i == 2) { vert = shoulder; }

            for (int ii = 0; ii < connectedVerts[vert].Count; ii++)
            {
                connectedVerts[vertCount + i].Add(connectedVerts[vert].list[ii]);
                connectedVerts[connectedVerts[vert].list[ii]].Add(vertCount + i);
            }
        }

        tris.Add(vertCount + 0);
        tris.Add(vertCount + 1);
        tris.Add(vertCount + 2);
        tris.Add(vertCount + 3);
        tris.Add(vertCount + 2);
        tris.Add(vertCount + 1);

        quads.Add(vertCount + 0);
        quads.Add(vertCount + 1);
        quads.Add(vertCount + 2);
        quads.Add(vertCount + 3);

        quadMaterial.Add(0);

        // Flip to face the camera
        Vector3 normal = Vector3.Cross(
            verts[hand] - verts[elbow],
            verts[shoulder] - verts[elbow]);

        float dot = Vector3.Dot(SceneView.lastActiveSceneView.camera.transform.forward, normal);
        Vector3 s = transform.lossyScale;
        if (s.x * s.y * s.z < 0)
        {
            dot *= -1;
        }
        if (dot > 0)
        {
            normal *= -1;
            int tc = tris.Count;
            int temp = tris[tc - 1];
            tris[tc - 1] = tris[tc - 2];
            tris[tc - 2] = temp;

            temp = tris[tc - 4];
            tris[tc - 4] = tris[tc - 5];
            tris[tc - 5] = temp;

            int qc = quads.Count;
            temp = quads[qc - 3];
            quads[qc - 3] = quads[qc - 2];
            quads[qc - 2] = temp;
        }


        vertNormals.Add(normal);
        vertNormals.Add(normal);
        vertNormals.Add(normal);
        vertNormals.Add(normal);

        meshNormals.Add(normal);
        meshNormals.Add(normal);
        meshNormals.Add(normal);
        meshNormals.Add(normal);

        faceNormals.Add(normal);
        halfEdges.Add(new MeshEditFaceEdges());

        // Resize all data structures representing this mesh to fit the new mesh size.
        for (int i = 0; i < uvMaps.Count; i++)
        {
            uvMaps[i].resizeUVLength(vertCount + 4);//, newVerts);
        }
        customTextureUVMap.Add(Vector2.zero);
        customTextureUVMap.Add(Vector2.zero);
        customTextureUVMap.Add(Vector2.zero);
        customTextureUVMap.Add(Vector2.zero);
    }
    public List<int> addQuadBetweenFaces(int v0, int v1)
    {
        #region Set v0 and v1 to reference the same face
        int rootFace = getFaceFromVert(v0);
        bool foundFace = false;
        if (rootFace != getFaceFromVert(v1))
        {
            for (int i = -1; i < connectedVerts[v1].list.Count; i++)
            {
                if (!foundFace)
                {
                    int vi = v1;
                    if (i >= 0)
                    {
                        vi = connectedVerts[v1].list[i];
                    }

                    rootFace = getFaceFromVert(vi);

                    for (int ii = -1; ii < connectedVerts[v0].list.Count; ii++)
                    {
                        int vii = v0;
                        if (ii >= 0)
                        {
                            vii = connectedVerts[v0].list[ii];
                        }

                        int face = getFaceFromVert(vii);
                        if (face == rootFace)
                        {
                            v1 = vi;
                            v0 = vii;
                            foundFace = true;
                            break;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        }
        if (!foundFace)
        {
            Debug.Log("Error: Can't create face when two selected verts aren't a part of the same face.");
            return null;
        }
        #endregion

        // For each vert, find the first touching face that is missing exactly one adjacent face (but only on the edge that touched the original vert)


        Vector3 averageNormal = faceNormals[rootFace].normalized;

        int c = -1;
        #region Find appropriate vert on v0 (c)
        int potentialFaces0 = 0;
        for (int i = 0; i < connectedVerts[v0].Count; i++)
        {
            int face = getFaceFromVert(connectedVerts[v0].list[i]);
            if (face == rootFace)
            {
                continue;
            }

            int edge = -1;
            int potentialEdges = 0;

            for (int f = 0; f < adjacentFaces[face].Count; f++)
            {
                if (adjacentFaces[face].list[f] == -1)
                {
                    bool foundC = false;
                    if (isShared(quads[face * 4 + quadEdgePatternClockwise[f * 2 + 0]], v0))
                    {
                        foundC = true;
                        c = quads[face * 4 + quadEdgePatternClockwise[f * 2 + 1]];
                    }
                    else if (isShared(quads[face * 4 + quadEdgePatternClockwise[f * 2 + 1]], v0))
                    {
                        foundC = true;
                        c = quads[face * 4 + quadEdgePatternClockwise[f * 2 + 0]];
                    }

                    if (foundC)
                    {
                        potentialEdges++;
                        edge = f;
                    }
                }
            }
            if (potentialEdges == 1)
            {
                potentialFaces0++;
                averageNormal += faceNormals[face].normalized;

                if (potentialFaces0 > 1)
                {
                    Debug.Log("Error: Can't create face when two selected verts aren't a part of the same face.");
                    return null;
                }
            }
        }
        if (potentialFaces0 > 1 || c == -1)
        {
            return null;
        }
        #endregion
        int d = -1;
        #region Find appropriate vert on v1 (d)
        int potentialFaces1 = 0;
        for (int i = 0; i < connectedVerts[v1].Count; i++)
        {
            int face = getFaceFromVert(connectedVerts[v1].list[i]);
            if (face == rootFace)
            {
                continue;
            }

            int edge = -1;
            int potentialEdges = 0;

            for (int f = 0; f < adjacentFaces[face].Count; f++)
            {
                if (adjacentFaces[face].list[f] == -1)
                {
                    bool foundD = false;
                    if (isShared(quads[face * 4 + quadEdgePatternClockwise[f * 2 + 0]], v1))
                    {
                        foundD = true;
                        d = quads[face * 4 + quadEdgePatternClockwise[f * 2 + 1]];
                    }
                    else if (isShared(quads[face * 4 + quadEdgePatternClockwise[f * 2 + 1]], v1))
                    {
                        foundD = true;
                        d = quads[face * 4 + quadEdgePatternClockwise[f * 2 + 0]];
                    }

                    if (foundD)
                    {
                        potentialEdges++;
                        edge = f;
                    }
                }
            }
            if (potentialEdges == 1)
            {
                potentialFaces1++;
                averageNormal += faceNormals[face].normalized;
                if (potentialFaces1 > 1)
                {
                    return null;
                }
            }
        }
        if (potentialFaces1 > 1 || d == -1)
        {
            return null;
        }
        #endregion

        if (averageNormal.sqrMagnitude > 0)
        {
            averageNormal /= 3;

            addQuadBetweenFaces(v0, v1, c, d, averageNormal);
        }
        else
        {
            addQuadBetweenFaces(v0, v1, c, d);
        }

        List<int> selectedVerts = new List<int>(new int[] { c, d });

        return selectedVerts;
    }
    public void getOppositeSideOfQuad(int face, int a, int b, out int c, out int d)
    {
        c = a;
        d = b;
        if (quads[face * 4 + 0] == a &&
            quads[face * 4 + 1] == b)
        {
            c = quads[face * 4 + 2];
            d = quads[face * 4 + 3];
        }
        else if (
            quads[face * 4 + 1] == a &&
            quads[face * 4 + 0] == b)
        {
            c = quads[face * 4 + 3];
            d = quads[face * 4 + 2];
        }
        else if (
            quads[face * 4 + 0] == a &&
            quads[face * 4 + 2] == b)
        {
            c = quads[face * 4 + 1];
            d = quads[face * 4 + 3];
        }
        else if (
            quads[face * 4 + 2] == a &&
            quads[face * 4 + 0] == b)
        {
            c = quads[face * 4 + 3];
            d = quads[face * 4 + 1];
        }
        else if (
            quads[face * 4 + 1] == a &&
            quads[face * 4 + 3] == b)
        {
            c = quads[face * 4 + 0];
            d = quads[face * 4 + 2];
        }
        else if (
            quads[face * 4 + 3] == a &&
            quads[face * 4 + 1] == b)
        {
            c = quads[face * 4 + 2];
            d = quads[face * 4 + 0];
        }
        else if (
            quads[face * 4 + 3] == a &&
            quads[face * 4 + 2] == b)
        {
            c = quads[face * 4 + 1];
            d = quads[face * 4 + 0];
        }
        else if (
            quads[face * 4 + 2] == a &&
            quads[face * 4 + 3] == b)
        {
            c = quads[face * 4 + 0];
            d = quads[face * 4 + 1];
        }

    }

    public void getOppositeSideOfQuadRelative(int face, int a, int b, out int c, out int d)
    {
        c = a;
        d = b;
        if (0 == a &&
            1 == b)
        {
            c = 2;
            d = 3;
        }
        else if (
            1 == a &&
            0 == b)
        {
            c = 3;
            d = 2;
        }
        else if (
            0 == a &&
            2 == b)
        {
            c = 1;
            d = 3;
        }
        else if (
            2 == a &&
            0 == b)
        {
            c = 3;
            d = 1;
        }
        else if (
            1 == a &&
            3 == b)
        {
            c = 0;
            d = 2;
        }
        else if (
           3 == a &&
            1 == b)
        {
            c = 2;
            d = 0;
        }
        else if (
            3 == a &&
            2 == b)
        {
            c = 1;
            d = 0;
        }
        else if (
            2 == a &&
            3 == b)
        {
            c = 0;
            d = 1;
        }

    }

    public void addMesh(Mesh mesh, Vector3 position)
    {
        Vector3 p = gameObject.transform.position;
        Quaternion r = gameObject.transform.rotation;
        Vector3 s = gameObject.transform.lossyScale;

        int newVerts = mesh.vertices.Length;
        int vertCount = verts.Count;
        int oldFaceCount = adjacentFaces.Count;

        Vector3 v;
        for (int i = 0; i < newVerts; i++)
        {
            v = mesh.vertices[i] + position;
            // Verts
            verts.Add(v);
            // Colours
            colours.Add(mesh.colors[i]);
            // Vert Normals
            vertNormals.Add(mesh.normals[i]);
            meshNormals.Add(mesh.normals[i]);
            // Custom Texture UVs
            customTextureUVMap.Add(mesh.uv[i]);

            // Quads
            quads.Add(vertCount + i);

            // ConnectedVerts
            ListWrapper connected = new ListWrapper();
            for (int ii = 0; ii < newVerts; ii++)
            {
                if (ii != i)
                {
                    if (mesh.vertices[i] == mesh.vertices[ii])
                    {
                        connected.Add(vertCount + ii);
                    }
                }
            }

            connectedVerts.Add(connected);
        }
        // AdjacentFaces
        rebuildAdjacentFaces(vertCount / 4);

        // Tris
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            tris.Add(mesh.triangles[i + 0] + vertCount);
            tris.Add(mesh.triangles[i + 1] + vertCount);
            tris.Add(mesh.triangles[i + 2] + vertCount);

            // FaceNormals
            if (i % 6 == 0)
            {
                Vector3 ab = mesh.vertices[mesh.triangles[i + 1]] - mesh.vertices[mesh.triangles[i]];
                Vector3 ac = mesh.vertices[mesh.triangles[i + 2]] - mesh.vertices[mesh.triangles[i]];

                faceNormals.Add(Vector3.Cross(ab, ac).normalized);
                halfEdges.Add(new MeshEditFaceEdges());
                quadMaterial.Add(0);
            }
        }

        // UVs
        for (int ii = 0; ii < uvMaps.Count; ii++)
        {
            uvMaps[ii].resizeUVLength(verts.Count);
        }
        

        pushNewGeometry();
    }

    // Check for any incompatible data arrays or uninitialised properties in the mesh
    public void verifyMeshData()
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;

        if (verts == null || verts.Count != mesh.vertices.Length)
        {

        }
    }

    public static bool intersectTriangleRay(Triangle t, Ray r, out Vector3 intersection)
    {
        Vector3 rOpposite = r.origin + r.direction * 10000.0f;
        Vector3 n;
        Vector3 intersectionTemp;

        intersection = Vector3.zero;

        n = Vector3.Cross(t.b - t.a, t.c - t.a);
        n = n.normalized;

        float d1 = Vector3.Dot((r.origin - t.a), (n));
        float d2 = Vector3.Dot((rOpposite - t.a), (n));

        if ((d1 * d2) >= 0.0f)
        {
            return false;
        }

        if (d1 == d2)
        {
            return false;
        }

        intersectionTemp = r.origin + (rOpposite - r.origin) * (-d1 / (d2 - d1));

        Vector3 cross = Vector3.Cross(n, (t.b - t.a));
        if (Vector3.Dot(cross, (intersectionTemp - t.a)) < 0.0f)
        {
            return false;
        }

        cross = Vector3.Cross(n, (t.c - t.b));
        if (Vector3.Dot(cross, (intersectionTemp - t.b)) < 0.0f)
        {
            return false;
        }

        cross = Vector3.Cross(n, (t.a - t.c));
        if (Vector3.Dot(cross, (intersectionTemp - t.a)) < 0.0f)
        {
            return false;
        }

        intersection = intersectionTemp;

        return true;
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
    public static Vector2 closestPoint(Vector2 a, Vector2 b, Vector2 o, bool constrain = true)
    {
        float d = Vector2.SqrMagnitude(b - a);

        if (d > 0)
        {
            Vector2 ao = o - a;
            Vector2 ab = b - a;
            float dot = Vector2.Dot(ao, ab) / d;

            if (constrain)
            {
                if (dot <= 0) { return a; }
                else if (dot >= 1) { return b; }
            }

            return a + ab * dot;

        }
        else
        {
            return a;
        }
    }

    void OnDisable()
    {
        //Debug.Log("Disabled");
    }

    void OnDestroy()
    {
        //Debug.Log("Destroyed");
    }

    public void OnRenderObject()
    {
        // Debug.Log("OnRenderObject...");
    }
    public void OnPostRender()
    {
        // Debug.Log("OnPostRenderObject...");
    }

    [NonSerialized]
    private Tool tempTool;
    [NonSerialized]
    private bool wasToolHidden;
    public void clearSavedTool()
    {
        wasToolHidden = false;
    }
    public void pushSavedTool()
    {
        wasToolHidden = true;
        tempTool = Tools.current;
        Tools.current = Tool.None;
    }
    public void popSavedTool()
    {
        if (wasToolHidden)
        {
            Tools.current = tempTool;
            wasToolHidden = false;
        }
        if (Tools.current == Tool.None)
        {
            Tools.current = Tool.Move;
        }
    }
        

    public void updateEditMode(int newEditMode)
    {
        if (newEditMode != editMode)
        {
            if (newEditMode != 0 && editMode == 0)
            {
                pushSavedTool();
            }

            editorInfo.editOperation = MeshEdit.EditorInfo.MeshEditOperation.Standard;

            editMode = newEditMode;

            if (selectedFaces != null &&
                selectedVerts != null)
            {
                selectedFaces = new bool[selectedFaces.Length];
                selectedVerts = new bool[selectedVerts.Length];
            }

            if (selectedTileset >= MeshEditWindow.tilesetsAvailable.Count)
            {
                selectedTileset = -1;
            }

            if (editMode != 0)
            {
                verifyMeshData();

                if (transformsApplied)
                {
                    transformsApplied = false;
                    pushNewGeometry();
                }
            }
            else if (editMode == 0)
            {
                checkTransforms();
                
                popSavedTool();
            }

            if (editMode == 2)
            {

                if (!hasDefaultUVs)
                {
                    MeshEditWindow.loadTilesets();
                }
            }

            if (editMode == 1)
            {
                selectedFaces = new bool[quads.Count / 4];
                selectedVerts = new bool[verts.Count];

            }
        }
    }

    public void selectionConvertToVerts()
    {
        // Convert faces into verts
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            for (int ii = 0; ii < 4; ii++)
            {
                selectedVerts[quads[i * 4 + ii]] = selectedFaces[i];
            }

        }
        if (selectedUVs != null)
        {
            for (int i = 0; i < selectedUVFaces.Length; i++)
            {
                for (int ii = 0; ii < 4; ii++)
                {
                    selectedUVs[quads[i * 4 + ii]] = selectedUVFaces[i];
                }
            }
        }
    }

    public void selectionConvertToFaces()
    {
        // Convert verts into faces
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedVerts[quads[i * 4 + 0]] &&
                selectedVerts[quads[i * 4 + 1]] &&
                selectedVerts[quads[i * 4 + 2]] &&
                selectedVerts[quads[i * 4 + 3]])
            {
                selectedFaces[i] = true;
            }
        }
        if (selectedUVFaces != null)
        {
            for (int i = 0; i < selectedUVFaces.Length; i++)
            {
                if (selectedUVs[quads[i * 4 + 0]] &&
                    selectedUVs[quads[i * 4 + 1]] &&
                    selectedUVs[quads[i * 4 + 2]] &&
                    selectedUVs[quads[i * 4 + 3]])
                {
                    selectedUVFaces[i] = true;
                }
            }
        }
    }
    
    private Vector2[] angleBasedFlattening()
    {
        Vector2[] uvs = new Vector2[verts.Count];

        

        return uvs;
    }

    // Mesh Parameterization in Theory and Practice:
    //https://www.inf.usi.ch/hormann/parameterization/index.html
    // Classic ABF method (2002 Sturler)
    // http://www.math.vt.edu/people/sturler/Publications/UIUCDCS-R-2001-2257.pdf
    // ABF++ (Sheffa)
    // https://www.cs.ubc.ca/~sheffa/papers/abf_plus_plus.pdf

    public void unwrapSelection(bool[] selectedVerts, bool[] selectedFaces)
    {
        int nodeCount = 0;
        int triangleCount = 0;

        for (int i = 0; i < selectedVerts.Length; i++)
        {
            if (selectedVerts[i])
            {
                nodeCount++;
            }
        }
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedFaces[i])
            {
                triangleCount += 2;
            }
        }

        Vector2[] nodes = new Vector2[nodeCount];
        int[][] triangleAngles = new int[triangleCount][];
        int[][] triangleEdges = new int[triangleCount][];
        int[][] triangleNodes = new int[triangleCount][];

        float[] optimalAngles = new float[triangleCount * 3];
        float[] meshAngles = new float[triangleCount * 3];

        for (int i = 0; i < selectedVerts.Length; i++)
        {
            if (selectedVerts[i])
            {
                nodeCount++;
            }
        }
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedFaces[i])
            {
                triangleCount += 2;
            }
        }
    }

    private void angleBasedFlatteningGetOptimalAngleFromNode(
        Vector2[] nodes, bool[] isInterior, int[][] anglesAtNode,
        int edgeCount, int[][] edgeTriangles, int[] edgesANode, int[] edgesBNode,
        float[] angles, int[] nodeAtAngle, int[] triangleAtAngle,
        float[] angles3D, ref float[] optimalAngles)
    {
        float piTimesTwo = Mathf.PI * 2;

        for (int i = 0; i < nodes.Length; i++)
        {
            if (isInterior[i])
            {
                // Use the mesh angles as a reference for the optimal angles of exterior nodes

                for (int j = 0; j < anglesAtNode[i].Length; j++)
                {
                    int a = anglesAtNode[i][j];

                    optimalAngles[a] = angles3D[a];
                }
            }
            else
            {
                // Calculate optimal angles surrounding an interior node

                float sumOfAnglesAroundNode = 0;

                for (int j = 0; j < anglesAtNode[i].Length; j++)
                {
                    int a = anglesAtNode[i][j];

                    sumOfAnglesAroundNode += angles3D[a];
                }

                sumOfAnglesAroundNode = piTimesTwo / sumOfAnglesAroundNode;

                for (int j = 0; j < anglesAtNode[i].Length; j++)
                {
                    int a = anglesAtNode[i][j];

                    optimalAngles[a] = angles3D[a] * sumOfAnglesAroundNode;
                }
            }
        }
    }


    private void resolveABFPPNodeFromAngles(int triangle, int resolvingAngle, float[][] pointsX, float[][] pointsY, float[][] angles, float[][] weights, bool[] isInterior, int[] nodes)
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            if (isInterior[i])
            {
                //p = (1/SUM Wtk) (SUM of all t k where v) WtkPtkNext
            }
            else
            {

                float m00 = 0;
                float m01 = 0;
                float m10 = 0;
                float m11 = 0;
                float m20 = 0;
                float m21 = 0;
                float m30 = 0;
                float m31 = 0;

                float div = 0;

                for (int t = 0; t < angles.Length; t++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int kNext = (k + 1) % 3;
                        int kPrev = (k + 4) % 3;

                        float nextAngle = angles[i][kNext];
                        float prevAngle = angles[i][kPrev];

                        float cotNext = 1.0f / Mathf.Tan(nextAngle);
                        float cotPrev = 1.0f / Mathf.Tan(prevAngle);

                        m00 += cotNext;
                        m01 += -1;
                        m10 += 1;
                        m11 += cotNext;
                        m20 += cotPrev;
                        m21 += 1;
                        m30 -= -1;
                        m31 -= cotPrev;

                        div += cotNext + cotPrev;
                    }
                }
                // This can be delayed until the end to only require two divisions
                /*
                m00 /= div;
                m01 /= div;
                m10 /= div;
                m11 /= div;
                m20 /= div;
                m21 /= div;
                m30 /= div;
                m31 /= div;
                */
                //float pxNext = pointsX[]

            }
        }
    }
    private void constructUVNodes(
        Vector2[] nodes, 
        int triangleCount, int[][] trianglePoints, int[][] triangleAngles, int[][] triangleEdges,
        int edgeCount, int[][] edgeTriangles, int[] edgesANode, int[] edgesBNode,
        float[] angles, int[] nodeAtAngle, int[] triangleAtAngle)
    {
        /* 
            —Choose a mesh edge e1 = (N1a, N1b).
            —Map N1a to (0, 0, 0).
            —Map N1b to (1, 0, 0).
            —Push edge 1 on the stack S.
        */
        Stack<int> solvedEdges = new Stack<int>();
        float[] edgeLength = new float[edgeCount];
        bool[] markedFaces = new bool[triangleCount];

        nodes[edgesANode[0]] = new Vector2(0, 0);
        nodes[edgesBNode[0]] = new Vector2(1, 0);
        edgeLength[0] = 1;


        solvedEdges.Push(0);

        while(solvedEdges.Count > 0)
        {
            // —While S not empty, pop an edge e = (Na, Nb).For each face fi = (Na, Nb, Nc) containing e:
            int edge = solvedEdges.Pop();


            for (int i = 0; i < edgeTriangles[edge].Length; i++)
            {
                int t = edgeTriangles[edge][i];

                // —If face[i] is marked as set continue.
                if (markedFaces[t])
                {
                    continue;
                }
                else
                {
                    //—If Nc is not yet projected to the z = 0 plane, compute it’s position based on Na, Nb and the face angles α1i, α2i and α3i.
                    //—Mark fi as set, push(Na, Nc) and(Nb, Nc) on the stack.

                    markedFaces[t] = true;

                    for (int e = 0; e < 3; e++)
                    {
                        if (triangleEdges[t][e] != edge)
                        {
                            solvedEdges.Push(triangleEdges[t][e]);
                        }
                    }
                }
            }



        }

    }



    private struct SparseElement
    {
        int x;
        int y;
        float value;
        public SparseElement(int x, int y, float value) {
            this.x = x;
            this.y = y;
            this.value = value;
        }
    }
    // https://www.cs.cmu.edu/~quake-papers/painless-conjugate-gradient.pdf
    // https://members.loria.fr/Bruno.Levy/papers/LSCM_SIGGRAPH_2002.pdf

    [SerializeField, HideInInspector]
    public bool[] pinnedUVs;

    private class SimpleMesh
    {
        List<int> verts;
        List<Vector2> uvs;
        List<int> triangles;
        List<int> pinnedUVs;

        public static List<SimpleMesh> getSimpleMeshes (MeshEdit solid, bool[] selectedFaces)
        {
            return new List<SimpleMesh>();
        }
    }

    private List<int> connectedVertsOverSeams(int face, int vert)
    {
        List<int> connected = new List<int>();

        // To remain connected over a seam, the connection must not travel THROUGH an existing seam. 
        // If a connected vert can travel around the seam to reach another connected vert, then it is okay.
        // Travel over the connected points via adjacent faces, not directly.

        // Essentially it's flood-fill but with verts
        connected.Add(vert);

        Stack<int> checkedVerts = new Stack<int>();

        checkedVerts.Push(vert);

        int anchorVert = vert;
        int faceOfForwardDirection = -1;

        int nextFace = -1;
        int lastFace = -1;
        int currentFace = face;

        bool nextFaceAvailable = true;

        int cc = 0;
        
        while (nextFaceAvailable)
        {

            cc++;
            if (cc > 100)
            {
                Debug.LogError("Loop never ended!");
                break;
            }
            // The anchorVert updates as it goes around
            int qq = -1;
            nextFace = getNextFaceAroundVert(currentFace, ref anchorVert, ref qq, lastFace);

            // Save for the check in the reverse-direction
            if (faceOfForwardDirection == -1)
            {
                faceOfForwardDirection = nextFace;
            }

            // Check for a blocking seam, or an unselectedFace
            if (nextFace == -1)
            {
                nextFaceAvailable = false;
            }
            else if (nextFace == face)
            {
                nextFaceAvailable = false;
            }
            else if (!selectedFaces[nextFace])
            {
                nextFaceAvailable = false;
            }
            else if (isSeamBetweenFaces(currentFace, nextFace))
            {
                nextFaceAvailable = false;
            }
            else
            {
                // A viable connected vert has been found, add it to the list
                connected.Add(anchorVert);
                nextFaceAvailable = true;
            }
            
            lastFace = currentFace;
            currentFace = nextFace;
        }
        // If a loop wasn't made, check the opposite direction
        if (nextFace != face && faceOfForwardDirection != -1)
        {
            anchorVert = vert;
            lastFace = faceOfForwardDirection;
            currentFace = face;
            nextFaceAvailable = true;
            cc = 0;

            while (nextFaceAvailable)
            {
                cc++;
                if (cc > 100)
                {
                    Debug.LogError("Loop never ended!");
                    break;
                }

                // The anchorVert updates as it goes around
                int qq = -1;
                nextFace = getNextFaceAroundVert(currentFace, ref anchorVert, ref qq, lastFace);


                // Check for a blocking seam, or an unselectedFace
                if (nextFace == -1)
                {
                    nextFaceAvailable = false;
                }
                else if (nextFace == face)
                {
                    nextFaceAvailable = false;
                }
                else if (!selectedFaces[nextFace])
                {
                    nextFaceAvailable = false;
                }
                else if (isSeamBetweenFaces(currentFace, nextFace))
                {
                    nextFaceAvailable = false;
                }
                else
                {
                    // A viable connected vert has been found, add it to the list
                    connected.Add(anchorVert);
                    nextFaceAvailable = true;
                }

                lastFace = currentFace;
                currentFace = nextFace;
            }
        }

        return connected;
    }

    private int getNextFaceAroundVert(int face, ref int cornerVert, ref int qOnNextFace, int lastFace = -1)
    {
        for (int i = 0; i< adjacentFaces[face].list.Count; i++)
        {
            int nextFace = adjacentFaces[face].list[i];
            if (nextFace >= 0)
            {
                for (int q = 0; q < 4; q++)
                {
                    int v = quads[nextFace * 4 + q];

                    if (isShared(v, cornerVert))
                    {
                        if (lastFace >= 0 && lastFace == nextFace)
                        {
                            break;
                        }
                        qOnNextFace = q;
                        cornerVert = v;
                        return nextFace;
                    }
                }
            }
        }
        return -1;
    }

    private bool isSeamBetweenFaces(int faceA, int faceB)
    {
        int side = adjacentFaces[faceA].list.IndexOf(faceB);

        if (side >= 0)
        {
            if (halfEdges[faceA].edges[side].isSeam)
            {
                return true;
            }
        }

        side = adjacentFaces[faceB].list.IndexOf(faceA);

        if (side >= 0)
        {
            if (halfEdges[faceB].edges[side].isSeam)
            {
                return true;
            }
        }

        return false;
    }

    public void lscmUnwrap(bool[] selectedVerts, bool[] selectedFaces)
    {
        beginTimer("Full LSCM Method");
        beginTimer("Setup");
        Undo.RegisterCompleteObjectUndo(this, "Unwrap Selection");

        if (pinnedUVs == null || pinnedUVs.Length != mesh.uv.Length)
        {
            //Debug.LogError("PinnedUVs not defined! Oh no!");
            pinnedUVs = new bool[mesh.uv.Length];
        }

        Vector2[] uvs = new Vector2[mesh.uv.Length];

        Vector2 uvMapPivot = Vector2.zero;

        bool usePresetPinAsUVPivot = false;
        bool allowUVScaling = true;

        for (int i = 0; i < selectedVerts.Length; i++)
        {
            uvs[i] = mesh.uv[i];
        }

        endTimer();

        beginTimer("Divide selection into islands");
        #region Divide the selected faces into separate islands
        List<bool[]> islandSelectedVerts = new List<bool[]>();
        List<bool[]> islandSelectedFaces = new List<bool[]>();
        List<float> areaOfSelections = new List<float>();
        List<bool> isIslandPinned = new List<bool>();
        List<Rect> islandBoundingBox = new List<Rect>();

        float totalSelectionArea = 0.0f;

        bool[] checkedFaces = new bool[selectedFaces.Length];

        Stack<int> facesToCheck = new Stack<int>();
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedFaces[i] && !checkedFaces[i])
            {
                bool[] currentIslandSelectedFaces = new bool[selectedFaces.Length];
                bool[] currentIslandSelectedVerts = new bool[selectedVerts.Length];
                float area = 0.0f;

                facesToCheck.Push(i);

                while (facesToCheck.Count > 0)
                {
                    int face = facesToCheck.Pop();

                    checkedFaces[face] = true;

                    area += areaOfFace(face);

                    currentIslandSelectedFaces[face] = true;
                    currentIslandSelectedVerts[quads[face * 4 + 0]] = true;
                    currentIslandSelectedVerts[quads[face * 4 + 1]] = true;
                    currentIslandSelectedVerts[quads[face * 4 + 2]] = true;
                    currentIslandSelectedVerts[quads[face * 4 + 3]] = true;

                    for (int f = 0; f < adjacentFaces[face].Count; f++)
                    {
                        int adjacentFace = adjacentFaces[face].list[f];
                        if (adjacentFace >= 0 &&
                            selectedFaces[adjacentFace] && 
                            !checkedFaces[adjacentFace] &&
                            !halfEdges[face].edges[f].isSeam)
                        {
                            facesToCheck.Push(adjacentFace);
                        }
                    }
                }

                islandSelectedFaces.Add(currentIslandSelectedFaces);
                islandSelectedVerts.Add(currentIslandSelectedVerts);
                areaOfSelections.Add(area);
                totalSelectionArea += area;
            }
        }
        
        #endregion
        endTimer();

        for (int k = 0; k < islandSelectedFaces.Count; k++)
        {
            selectedVerts = islandSelectedVerts[k];
            selectedFaces = islandSelectedFaces[k];

            #region Simplify Mesh
            beginTimer("Setup part 2");
            bool[] tempPinnedUVs = new bool[pinnedUVs.Length];
            List<int> simpleVerts = new List<int>();
            List<int> simpleVertPointer = new List<int>();
            List<bool> isSimpleVertPinned = new List<bool>();

            List<int> simpleVertsFree = new List<int>();
            List<int> simpleVertsPinned = new List<int>();

            int[] vertMap = new int[selectedVerts.Length];

            bool[] selectedVertsTemp = new bool[selectedVerts.Length];
            for (int i = 0; i < selectedVerts.Length; i++)
            {
                vertMap[i] = -1;
                selectedVertsTemp[i] = selectedVerts[i];

            }

            List<int> simpleTris = new List<int>();
            int pinnedCount = 0;

            int firstPinnedUVIndex = -1;
            int lastPinnedUVIndex = -1;

            endTimer();
            beginTimer("Create simplified mesh");
            #region construct simple vert & tri map
            for (int i = 0; i < selectedFaces.Length; i++)
            {
                if (selectedFaces[i])
                {
                    for (int f = 0; f < 4; f++)
                    {
                        int v = quads[i * 4 + f];

                        if (selectedVertsTemp[v])
                        {
                            simpleVerts.Add(v);

                            vertMap[v] = v;

                            selectedVertsTemp[v] = false;

                            tempPinnedUVs[v] = pinnedUVs[v];

                            List<int> interconnectedVerts = connectedVertsOverSeams(i, v);

                            string icVertsDebug = "ICV[" + v + "]: ";

                            for (int j = 0; j < interconnectedVerts.Count; j++)
                            {
                                //icVertsDebug += interconnectedVerts[j] + ", ";
                                vertMap[interconnectedVerts[j]] = v;

                                selectedVertsTemp[interconnectedVerts[j]] = false;
                            }
                            //Debug.Log(icVertsDebug);
                        }
                    }
                }
            }



            /*
            Debug.Log("Simple Verts Selected: ");
            string toString = "";
            for (int i = 0; i < simpleVerts.Count; i++)
            {
                toString += simpleVerts[i] + ", ";
            }
            Debug.Log("Simple Verts Selected: " + toString);
            toString = "";*/

            for (int i = 0; i < selectedFaces.Length; i++)
            {
                if (selectedFaces[i])
                {
                    simpleTris.Add(simpleVerts.IndexOf(vertMap[quads[i * 4 + 0]]));
                    simpleTris.Add(simpleVerts.IndexOf(vertMap[quads[i * 4 + 1]]));
                    simpleTris.Add(simpleVerts.IndexOf(vertMap[quads[i * 4 + 2]]));

                    simpleTris.Add(simpleVerts.IndexOf(vertMap[quads[i * 4 + 3]]));
                    simpleTris.Add(simpleVerts.IndexOf(vertMap[quads[i * 4 + 2]]));
                    simpleTris.Add(simpleVerts.IndexOf(vertMap[quads[i * 4 + 1]]));


                }
            }
            /*
            for (int i = 0; i < simpleTris.Count; i++)
            {
                toString += simpleTris[i] + ", ";
            }

            Debug.Log("Simple Tris: " + toString);
            */
            #endregion
            endTimer();

            beginTimer("Find UV pins");
            #region Find UV pins and create fixed UV points if insufficient fixed points exist

            // Find existing Pins
            for (int i = 0; i < simpleVerts.Count; i++)
            {
                int v = simpleVerts[i];

                if (pinnedUVs[v])
                {
                    uvs[v] = mesh.uv[v];
                    if (firstPinnedUVIndex == -1)
                    {
                        //Debug.Log("Found Natural pin 1: " + v + " -> " + vertMap[v] + " = " + i);
                        firstPinnedUVIndex = i;
                    }
                    else
                    {
                        //Debug.Log("Found Natural pin 2: " + v + " -> " + vertMap[v] + " = " + i);
                        lastPinnedUVIndex = i;
                    }

                    pinnedCount++;
                }
            }


            usePresetPinAsUVPivot = true;
            allowUVScaling = true;
            // If there are ZERO pinned uvs, create a new set from scratch

            if (pinnedCount == 0)
            {
                isIslandPinned.Add(false);
            }
            else
            {
                isIslandPinned.Add(true);
            }

            if (pinnedCount < 2)
            {
                if (firstPinnedUVIndex == -1)
                {
                    usePresetPinAsUVPivot = false;
                    firstPinnedUVIndex = 0;
                    tempPinnedUVs[simpleVerts[firstPinnedUVIndex]] = true;
                    uvs[simpleVerts[firstPinnedUVIndex]] = new Vector2(0.0f, 0.0f);

                    for (int v = 0; v < connectedVerts[simpleVerts[firstPinnedUVIndex]].Count; v++)
                    {
                        int vv = connectedVerts[simpleVerts[firstPinnedUVIndex]].list[v];
                        int face = quads.IndexOf(vv) / 4;

                        if (selectedFaces[face])
                        {
                            uvs[vv] = new Vector2(0.0f, 0.0f);
                        }
                    }

                    //Debug.Log("Fixed uv: " + firstPinnedUVIndex);
                }
                else if (lastPinnedUVIndex == -1)
                {
                    usePresetPinAsUVPivot = true;
                }

                if (lastPinnedUVIndex == -1)
                {
                    //Debug.Log("Second pinned UV missing. Fisrt: " + firstPinnedUVIndex + " = " + simpleVerts[firstPinnedUVIndex]);
                    // Debug.Log("simpleVerts: " + simpleVerts.Count);
                    // Debug.Log("First Vert: " + firstPinnedUVIndex);
                    // Debug.Log("First Vert Vert Index: " + simpleVerts[firstPinnedUVIndex]);
                    // The uv map is completely un-pinned and can be simply transformed to fit the containing UV space.

                    Vector3 firstVertPos = verts[simpleVerts[firstPinnedUVIndex]];
                    float distToSecondVert = 0;

                    for (int i = 0; i < simpleVerts.Count; i++)
                    {
                        if (selectedVerts[simpleVerts[i]])
                        {
                            if (firstPinnedUVIndex != i)
                            {
                                float dd = (verts[simpleVerts[i]] - firstVertPos).sqrMagnitude;
                                if (dd > distToSecondVert)
                                {
                                    distToSecondVert = dd;
                                    lastPinnedUVIndex = i;
                                }
                            }
                        }
                    }

                    if (lastPinnedUVIndex >= 0 && firstPinnedUVIndex != lastPinnedUVIndex)
                    {
                        //Debug.Log("Fixed uv: " + lastPinnedUVIndex);
                        uvs[simpleVerts[lastPinnedUVIndex]] = uvs[simpleVerts[firstPinnedUVIndex]] + new Vector2(1.0f, 0.0f);
                        tempPinnedUVs[simpleVerts[lastPinnedUVIndex]] = true;

                        for (int v = 0; v < connectedVerts[simpleVerts[lastPinnedUVIndex]].Count; v++)
                        {
                            int vv = connectedVerts[simpleVerts[lastPinnedUVIndex]].list[v];
                            int face = quads.IndexOf(vv) / 4;

                            if (selectedFaces[face])
                            {
                                uvs[vv] = uvs[simpleVerts[lastPinnedUVIndex]];
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Error: Cannot unwrap when there are less than two selected vertices.");
                        return;
                    }
                }
            }


            else
            {
                //Debug.Log("Two or more pinned UVs found.");
                // The map is pre-fixed at two points and cannot be transformed to fit the container.
                usePresetPinAsUVPivot = false;
                allowUVScaling = false;

            }
            #endregion
            endTimer();

            beginTimer("Map the pinned and free UVs");
            #region Map the pins to separate simple lists
            int fixedCount = 0;
            int freeCount = 0;
            for (int i = 0; i < simpleVerts.Count; i++)
            {
                if (tempPinnedUVs[simpleVerts[i]])
                {
                    simpleVertPointer.Add(fixedCount);
                    fixedCount++;
                    simpleVertsPinned.Add(simpleVerts[i]);
                    isSimpleVertPinned.Add(true);
                }
                else
                {
                    simpleVertPointer.Add(freeCount);
                    freeCount++;
                    simpleVertsFree.Add(simpleVerts[i]);
                    isSimpleVertPinned.Add(false);
                }
            }
            #endregion
            endTimer();

            #endregion

            #region Define Matrices
            int triangleCount = simpleTris.Count / 3;

            int pinnedUVCount = 0;
            int freeUVCount = 0;

            for (int i = 0; i < tempPinnedUVs.Length; i++)
            {
                if (tempPinnedUVs[i])
                {
                    pinnedUVCount++;
                }
            }
            freeUVCount = simpleVerts.Count - pinnedUVCount;


            float[,] sparseMatrixFree = new float[2 * freeUVCount, 2 * triangleCount];
            float[,] sparseMatrixPinned = new float[2 * pinnedUVCount, 2 * triangleCount];
            float[,] pinnedUVValues = new float[1, pinnedUVCount * 2];
            #endregion

            beginTimer("Set Triangle Coefficients");
            #region set triangle coefficients
            Vector2[] coefficient = new Vector2[3];


            int t = 0;
            for (int i = 0; i < simpleTris.Count; i += 3)
            {
                // Flatten the triangle made by the 3D mesh 


                Vector3 triangleSideA = verts[simpleVerts[simpleTris[i + 1]]] - verts[simpleVerts[simpleTris[i + 0]]];
                Vector3 triangleSideB = verts[simpleVerts[simpleTris[i + 2]]] - verts[simpleVerts[simpleTris[i + 1]]];
                Vector3 triangleSideC = verts[simpleVerts[simpleTris[i + 0]]] - verts[simpleVerts[simpleTris[i + 2]]];
                float sideA = triangleSideA.magnitude;
                float sideB = triangleSideB.magnitude;
                float sideC = triangleSideC.magnitude;
                
                float angle = -Vector3.Angle(-triangleSideA, triangleSideC) * Mathf.Deg2Rad;
                
                float x1, x2, x3, y1, y2, y3;
                x1 = 0; 
                x2 = sideA;
                x3 = sideC * Mathf.Cos(angle);
                y1 = 0;
                y2 = 0;
                y3 = sideC * Mathf.Sin(angle);

                float dT = Mathf.Sqrt( Mathf.Abs((x1 * y1 - y1 * x2) + (x2 * y3 - y2 * x3) + (x3 * y1 - y3 * x1)));

                coefficient[0].x = (x3 - x2) / dT;
                coefficient[0].y = (y3 - y2) / dT;
                coefficient[1].x = (x1 - x3) / dT;
                coefficient[1].y = (y1 - y3) / dT;
                coefficient[2].x = (x2 - x1) / dT;
                coefficient[2].y = (y2 - y1) / dT;

                // Fill the matrices
                for (int ii = 0; ii < 3; ii++)
                {
                    Vector3 c = coefficient[ii];

                    if (isSimpleVertPinned[simpleTris[i + ii]])
                    {
                        int v = simpleVertPointer[simpleTris[i + ii]];

                        sparseMatrixPinned[v, t] = c.x;
                        sparseMatrixPinned[v, triangleCount + t] = c.y;
                        sparseMatrixPinned[pinnedUVCount + v, t] = -c.y;
                        sparseMatrixPinned[pinnedUVCount + v, triangleCount + t] = c.x;

                        pinnedUVValues[0, v] = uvs[simpleVertsPinned[v]].x;
                        pinnedUVValues[0, pinnedUVCount + v] = uvs[simpleVertsPinned[v]].y;

                    }
                    else
                    {
                        int v = simpleVertPointer[simpleTris[i + ii]];

                        sparseMatrixFree[v, t] = c.x;
                        sparseMatrixFree[v, triangleCount + t] = c.y;
                        sparseMatrixFree[freeUVCount + v, t] = -c.y;
                        sparseMatrixFree[freeUVCount + v, triangleCount + t] = c.x;
                    }
                }

                t++;
            }


            #endregion
            endTimer();

            beginTimer("Define matrices");


            #region Solve matrices

            //mxLog(sparseMatrixPinned, "SMPinned: ");
            //mxLog(pinnedUVValues, "UVsPinned: ");
            float[,] b = mxMultiply(sparseMatrixPinned, pinnedUVValues);
            //mxLog(b, "b: ");
            b = mxMultiply(-1, b);


            float[,] x0 = new float[1, freeUVCount * 2];

            //mxLog(sparseMatrixFree, "Sparse Matrix Free: ");


            float[,] sparseTranspose = mxTranspose(sparseMatrixFree);
            float[,] aTA = mxMultiply(sparseTranspose, sparseMatrixFree);
            float[,] aTB = mxMultiply(sparseTranspose, b);
            endTimer();

            beginTimer("SolveConjugateGradient method");
            
            x0 = solveConjugateGradient(aTA, aTB, x0, false);
            endTimer();

            beginTimer("Set to UVs");

            for (int i = 0; i < freeUVCount; i++)
            {
                int v = simpleVertsFree[i];

                uvs[v].x = x0[0, i];
                uvs[v].y = x0[0, i + freeUVCount];

                for (int j = 0; j < vertMap.Length; j++)
                {
                    if (vertMap[j] == v)
                    {
                        uvs[j].x = x0[0, i];
                        uvs[j].y = x0[0, i + freeUVCount];

                    }
                }
            }
            #endregion
            endTimer();

            beginTimer("Transform unwrapped verts");
            #region transform UVs to best fit the UV space, if possible


            if (allowUVScaling)
            {
                #region Rotate

                // Graham Scan for the Convex Hull around the simple meshes' UVs
                // https://en.wikipedia.org/wiki/Graham_scan

                int point0 = 0;
                Vector2 p = uvs[vertMap[simpleVerts[point0]]];
                Vector2 center = Vector2.zero;
                // Find the bottom-leftmost point
                for (int i = 0; i < simpleVerts.Count; i++)
                {
                    center += uvs[vertMap[simpleVerts[i]]];
                    if (uvs[vertMap[simpleVerts[i]]].y < p.y)
                    {
                        point0 = i;
                        p = uvs[vertMap[simpleVerts[i]]];
                    }
                    else if (uvs[vertMap[simpleVerts[i]]].y == p.y &&
                        uvs[vertMap[simpleVerts[i]]].x < p.x)
                    {
                        point0 = i;
                        p = uvs[vertMap[simpleVerts[i]]];
                    }
                }
                center /= simpleVerts.Count;
                // Sort by angle away from point0, clockwise. I'm gonna do an insertion sort.
                List<float> pointAngles = new List<float>();
                List<int> uvsSorted = new List<int>();

                pointAngles.Add(0);
                uvsSorted.Add(vertMap[simpleVerts[point0]]);

                Vector2 toPoint0 = p - center;
                float originAngle = (Mathf.Atan2(toPoint0.y, toPoint0.x));

                for (int i = 0; i < simpleVerts.Count; i++)
                {
                    if (i != point0)
                    {
                        Vector2 next = uvs[vertMap[simpleVerts[i]]] - center;
                        float angle = (Mathf.Atan2(next.y, next.x) - originAngle + Mathf.PI * 2) % (Mathf.PI * 2);
                        if (angle > 0)
                        {
                            bool foundInsertPos = false;
                            for (int ii = 1; ii < uvsSorted.Count; ii++)
                            {
                                if (pointAngles[ii] > angle)
                                {
                                    continue;
                                }
                                else
                                {
                                    if (pointAngles[ii - 1] == angle)
                                    {
                                        if (next.sqrMagnitude > (uvs[uvsSorted[ii - 1]] - center).sqrMagnitude)
                                        {
                                            uvsSorted[ii - 1] = i;
                                        }
                                    }
                                    else
                                    {
                                        uvsSorted.Insert(ii, vertMap[simpleVerts[i]]);
                                        pointAngles.Insert(ii, angle);
                                    }

                                    foundInsertPos = true;
                                    break;
                                }
                            }

                            if (!foundInsertPos)
                            {
                                uvsSorted.Add(vertMap[simpleVerts[i]]);
                                pointAngles.Add(angle);
                            }
                        }
                    }
                }


                List<int> convexHull = new List<int>();
               
                // Now for the ACTUAL convex hull bit
                for (int i = 0; i < uvsSorted.Count; i++)
                {
                    p = uvs[uvsSorted[i]];
                    while (convexHull.Count > 1 &&
                         MeshEditWindow.cross(
                             uvs[convexHull[convexHull.Count - 2]] - p,
                             uvs[convexHull[convexHull.Count - 1]] - p) > 0)
                    {
                        convexHull.RemoveAt(convexHull.Count - 1);
                    }

                    convexHull.Add(uvsSorted[i]);
                }

                // Now find the angle that occupies the smallest area
                float smallestArea = float.MaxValue;
                int sideWithSmallestBoundingBox = 0;
                float rotationForOptimalBoundingBox = 0;
                Rect smallestRectangle = Rect.zero;

                for (int i = 0; i < convexHull.Count; i++)
                {
                    int ii = (i + 1) % convexHull.Count;

                    Vector2 o = uvs[convexHull[i]];
                    Vector2 xAxis = (uvs[convexHull[ii]] - uvs[convexHull[i]]).normalized;
                    Vector2 yAxis = new Vector2(-xAxis.y, xAxis.x);

                    float left = float.MaxValue;
                    float right = float.MinValue;
                    float bottom = float.MaxValue;
                    float top = float.MinValue;

                    for (int iii = 0; iii < convexHull.Count; iii++)
                    {
                        Vector2 a = (uvs[convexHull[iii]] - center);

                        float x = Vector2.Dot(a, xAxis);
                        float y = Vector2.Dot(a, yAxis);

                        if (x < left)
                        {
                            left = x;
                        }
                        if (x > right)
                        {
                            right = x;
                        }

                        if (y < bottom)
                        {
                            bottom = y;
                        }
                        if (y > top)
                        {
                            top = y;
                        }
                    }

                    float bbWidth = right - left;
                    float bbHeight = top - bottom;


                    float area = bbWidth * bbHeight;
                    
                    if (area < smallestArea ||
                        (area == smallestArea && (right - left) < smallestRectangle.width))
                    {
                        smallestArea = area;
                        sideWithSmallestBoundingBox = i;

                        if (bbWidth <= bbHeight)
                        {
                            rotationForOptimalBoundingBox = Mathf.Rad2Deg * Mathf.Atan2(xAxis.y, xAxis.x);

                            smallestRectangle = new Rect(center.x + left, center.y + bottom, bbWidth, bbHeight);
                        }
                        else
                        {
                            rotationForOptimalBoundingBox = Mathf.Rad2Deg * Mathf.Atan2(xAxis.y, xAxis.x) + 90.0f;

                            smallestRectangle = new Rect(center.x + bottom, center.y - (bbWidth + left), bbHeight, bbWidth);
                        }
                    }
                }

                
                islandBoundingBox.Add(smallestRectangle);

                Vector2 rotationOrigin = center;
                
                if (usePresetPinAsUVPivot)
                {
                    rotationOrigin = new Vector2(pinnedUVValues[0, 0], pinnedUVValues[0, pinnedUVCount]);
                }

                for (int i = 0; i < faceNormals.Count; i++)
                {
                    if (selectedFaces[i])
                    {
                        for (int ii = 0; ii < 4; ii++)
                        {
                            int iii = i * 4 + ii;

                            uvs[iii] = MeshEditWindow.rotate(uvs[iii], -rotationForOptimalBoundingBox, rotationOrigin);
                        }
                    }
                }
                #endregion
            }
            else
            {
                islandBoundingBox.Add(Rect.zero);
                
                float xMin = float.MaxValue;
                float xMax = float.MinValue;
                float yMin = xMin;
                float yMax = xMax;
                for (int i = 0; i < uvs.Length; i++)
                {
                    int face = quads.IndexOf(i) / 4;

                    if (selectedFaces[face])
                    {
                        if (uvs[i].x > xMax)
                        {
                            xMax = uvs[i].x;
                        }
                        if (uvs[i].x < xMin)
                        {
                            xMin = uvs[i].x;
                        }
                        if (uvs[i].y > yMax)
                        {
                            yMax = uvs[i].y;
                        }
                        if (uvs[i].y < yMin)
                        {
                            yMin = uvs[i].y;
                        }
                    }
                }

                float xSize = xMax - xMin;
                float ySize = yMax - yMin;

                float size = Mathf.Max(xSize, ySize);
                //size = 0;
                if (size != 0)
                {
                    xMin /= size;
                    xMax /= size;
                    yMin /= size;
                    yMax /= size;

                    for (int f = 0; f < faceNormals.Count; f++)
                    {
                        if (selectedFaces[f])
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                int i = quads[f * 4 + j];

                                if (usePresetPinAsUVPivot)
                                {
                                    uvs[i].x -= pinnedUVValues[0, 0];
                                    uvs[i].y -= pinnedUVValues[0, pinnedUVCount];
                                }

                                uvs[i] = (uvs[i] / size);
                                
                                if (!usePresetPinAsUVPivot)
                                {
                                    uvs[i].x -= xMin;
                                    uvs[i].y -= yMin;
                                }
                                else
                                {
                                    uvs[i].x += pinnedUVValues[0, 0];
                                    uvs[i].y += pinnedUVValues[0, pinnedUVCount];
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            endTimer();
        }

        #region Island packing
        // Scale the islands to their appropriate size relative to one another
        List<float> areaOfUnwrappedIslands = new List<float>();
        float totalAreaOfUnwrappedIslands = 0.0f;
        for (int k = 0; k < isIslandPinned.Count; k++)
        {
            selectedVerts = islandSelectedVerts[k];
            selectedFaces = islandSelectedFaces[k];

            float area = 0.0f;

            for (int i = 0; i < selectedFaces.Length; i++)
            {
                if (selectedFaces[i])
                {
                    int uvA = quads[i * 4 + 0];
                    int uvB = quads[i * 4 + 1];
                    int uvC = quads[i * 4 + 2];
                    int uvD = quads[i * 4 + 3];
                    area += areaOf2DFace(uvs[uvA], uvs[uvB], uvs[uvC], uvs[uvD]);
                }
            }
            areaOfUnwrappedIslands.Add(area);
            totalAreaOfUnwrappedIslands += area;
        }

        for (int k = 0; k < isIslandPinned.Count; k++)
        {
            selectedVerts = islandSelectedVerts[k];
            selectedFaces = islandSelectedFaces[k];

            float selectionProportion = areaOfSelections[k] / totalSelectionArea;

            float idealUnwrappedArea = selectionProportion * totalAreaOfUnwrappedIslands;

            float areaScalingFactor = idealUnwrappedArea / areaOfUnwrappedIslands[k];

            // When linearly scaling a shape, the area is multiplied by the scaling factor, squared

            float scalingFactor = Mathf.Sqrt(areaScalingFactor);
            
            islandBoundingBox[k] = new Rect(
                islandBoundingBox[k].x * scalingFactor,
                islandBoundingBox[k].y * scalingFactor,
                islandBoundingBox[k].width * scalingFactor,
                islandBoundingBox[k].height * scalingFactor);
            
            for (int i = 0; i < selectedFaces.Length; i++)
            {
                if (selectedFaces[i])
                {
                    uvs[quads[i * 4 + 0]] *= scalingFactor;
                    uvs[quads[i * 4 + 1]] *= scalingFactor;
                    uvs[quads[i * 4 + 2]] *= scalingFactor;
                    uvs[quads[i * 4 + 3]] *= scalingFactor;
                }
            }
        }

        // Pack the islands based on their minimum bounding box
        // Sort the rectangles descending by size
        List<int> sortedIslands = new List<int>();
        for (int i = 0; i < isIslandPinned.Count; i++)
        {
            if (!isIslandPinned[i])
            {
                float area = islandBoundingBox[i].size.y;
                bool foundInsertionPos = false;
                for (int ii = 0; ii < sortedIslands.Count; ii++)
                {
                    int iii = sortedIslands[ii];
                    float islandArea = islandBoundingBox[iii].size.y;

                    if (islandArea <= area)
                    {
                        foundInsertionPos = true;
                        sortedIslands.Insert(ii, i);
                        break;
                    }
                }
                if (!foundInsertionPos)
                {
                    sortedIslands.Add(i);
                }
            }
        }



        
        float packMargin = 0.1f;

        List<Rect> islandBBInPackedArea = new List<Rect>();

        float bestSquareRatio = float.MaxValue;

        float bestWidth = 0.0f;
        float bestHeight = 0.0f;

        List<Rect> bestSpacePacking = null;



        // This pack assumes that the islands are aligned with their longest diameter upright, sorted by bb height.
        //  So long flat pieces aranged in landscape will not pack properly
        for (int i = 0; i < sortedIslands.Count; i++)
        {
            float areaWidth = 0.0f;
            float areaHeight = 0.0f;

            List<Rect> occupiedSpaceInPackArea = new List<Rect>();

            bool canBBFit = true;

            for (int ii = 0; ii < sortedIslands.Count; ii++)
            {
                Rect bb = islandBoundingBox[sortedIslands[ii]];

                if (ii < sortedIslands.Count - i)
                {
                    // Align to the bottom of the space
                    occupiedSpaceInPackArea.Add(new Rect(areaWidth, 0.0f, bb.width + packMargin, bb.height + packMargin));

                    areaWidth += bb.width + packMargin;
                    areaHeight = Mathf.Max(areaHeight, bb.height + packMargin);
                }
                else
                {
                    // Tetris the bounding box into the lowest possible space
                    float lowestY = float.MaxValue;
                    Rect bestRect = Rect.zero;

                    Rect bbToPack = new Rect(0.0f, 0.0f, bb.width + packMargin, bb.height + packMargin);

                    bool wasAnyBestRectFound = false;
                   

                    if (bbToPack.width > areaWidth)
                    {
                        canBBFit = false;
                        break;
                    }

                    for (int iii = 0; iii < occupiedSpaceInPackArea.Count; iii++)
                    {
                        // Check the bounding box we want to add against the top of all existing bounding boxes
                        
                        bbToPack.x = occupiedSpaceInPackArea[iii].x;
                        bbToPack.y = occupiedSpaceInPackArea[iii].y + occupiedSpaceInPackArea[iii].height;

                        if (bbToPack.xMax <= areaWidth)
                        {

                            bool doesNewRectFit = true;
                            // Check whether placing the rectangle at that spot would intersect with any other blocks
                            for (int iv = 0; iv < occupiedSpaceInPackArea.Count; iv++)
                            {
                                if (iv != iii)
                                {
                                    Rect compare = occupiedSpaceInPackArea[iv];

                                    if (compare.Overlaps(bbToPack))
                                    {
                                        doesNewRectFit = false;
                                        break;
                                    }
                                }

                            }

                            if (doesNewRectFit &&
                                bbToPack.yMax < lowestY)
                            {
                                wasAnyBestRectFound = true;
                                lowestY = bbToPack.yMax;
                                bestRect = bbToPack;
                            }
                        }
                    }

                    if (wasAnyBestRectFound)
                    {
                        occupiedSpaceInPackArea.Add(bestRect);
                        areaHeight = Mathf.Max(areaHeight, bestRect.yMax);
                    }
                    else
                    {
                        canBBFit = false;
                        break;
                    }
                }


                if (!canBBFit)
                {
                    break;
                }
            }

            if (canBBFit)
            {
                float ratio = areaHeight / areaWidth;
                if (ratio > 1)
                {
                    ratio = 1.0f / ratio;
                }

                // The best ratio of width to height is the one which can fit the most of itself into a square
                if (bestSquareRatio > 1.0f / ratio)
                {
                    bestSquareRatio = 1.0f / ratio;
                    bestSpacePacking = occupiedSpaceInPackArea;

                    bestHeight = areaHeight;
                    bestWidth = areaWidth;
                }
                else
                {
                    // The width/height changes parabolically(?), so as soon as the packed area's ratio stops being less than the best ratio, there won't be a better layout.
                    break;
                }
            }
            // Compare to saved pack areas
        }
        if (bestSpacePacking == null)
        {
            Debug.Log("Error: No space packing layout was available");
            return;
        }
        
        for (int k = 0; k < sortedIslands.Count; k++)
        {
            selectedVerts = islandSelectedVerts[sortedIslands[k]];
            selectedFaces = islandSelectedFaces[sortedIslands[k]];

            Rect bb = islandBoundingBox[sortedIslands[k]];

            Vector2 bottomLeft = new Vector2(bestSpacePacking[k].xMin, bestSpacePacking[k].yMin);

            for (int f = 0; f < faceNormals.Count; f++)
            {
                if (selectedFaces[f])
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int ii = quads[f * 4 + i];
                        uvs[ii].x -= bb.xMin;
                        uvs[ii].y -= bb.yMin;
                        uvs[ii].x += bottomLeft.x;
                        uvs[ii].y += bottomLeft.y;
                    }
                }
            }
        }

        float scale = (1.0f / (Mathf.Max(bestWidth, bestHeight) - packMargin));
        
        for (int k = 0; k < sortedIslands.Count; k++)
        {
            selectedVerts = islandSelectedVerts[k];
            selectedFaces = islandSelectedFaces[k];

            for (int f = 0; f < faceNormals.Count; f++)
            {
                if (selectedFaces[f])
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int ii = quads[f * 4 + i];
                        uvs[ii].x *= scale;
                        uvs[ii].y *= scale;
                    }
                }
            }
        }

        /* Linear pack sorted by height
        for (int k = 0; k < sortedIslands.Count; k++)
        {
            selectedVerts = islandSelectedVerts[sortedIslands[k]];
            selectedFaces = islandSelectedFaces[sortedIslands[k]];

            Rect bb = islandBoundingBox[sortedIslands[k]];
            
            for (int f = 0; f < faceNormals.Count; f++)
            {
                if (selectedFaces[f])
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int ii = quads[f * 4 + i];
                        uvs[ii].x -= bb.xMin;
                        uvs[ii].y -= bb.yMin;

                        uvs[ii].x += packX;
                    }
                }
            }

            packX += bb.width;
            if (k < sortedIslands.Count - 1)
            {
                packX += packMargin;
            }
        }

        float scale = (1.0f / packX); 

        for (int k = 0; k < sortedIslands.Count; k++)
        {
            selectedVerts = islandSelectedVerts[k];
            selectedFaces = islandSelectedFaces[k];
            
            for (int f = 0; f < faceNormals.Count; f++)
            {
                if (selectedFaces[f])
                {
                    for (int i = 0; i < 4; i++)
                    {
                        int ii = quads[f * 4 + i];
                        uvs[ii].x *= scale;
                        uvs[ii].y *= scale;
                    }
                }
            }
        }
        */


        #endregion

        endTimer();
        beginTimer("Push new UVs");
        // Set and push the UVs out
        

        setUVsThroughUVEditor(uvs);

        pushLocalMeshToGameObject();
        endTimer();
    }

    public void setUVsThroughUVEditor(Vector2[] newUVs)
    {
        int f = 0;
        for (int i = 0; i < quads.Count; i += 4)
        {
            if (materialUVMap[quadMaterial[f]] == -1)
            {
                customTextureUVMap[quads[i + 0]] = newUVs[quads[i + 0]];
                customTextureUVMap[quads[i + 1]] = newUVs[quads[i + 1]];
                customTextureUVMap[quads[i + 2]] = newUVs[quads[i + 2]];
                customTextureUVMap[quads[i + 3]] = newUVs[quads[i + 3]];
            }
            else
            {
                UVData uvData = uvMaps[materialUVMap[quadMaterial[f]]];
                uvData._newUvs[quads[i + 0]] = newUVs[quads[i + 0]];
                uvData._newUvs[quads[i + 1]] = newUVs[quads[i + 1]];
                uvData._newUvs[quads[i + 2]] = newUVs[quads[i + 2]];
                uvData._newUvs[quads[i + 3]] = newUVs[quads[i + 3]];
                uvData._uvs[quads[i + 0]] = newUVs[quads[i + 0]];
                uvData._uvs[quads[i + 1]] = newUVs[quads[i + 1]];
                uvData._uvs[quads[i + 2]] = newUVs[quads[i + 2]];
                uvData._uvs[quads[i + 3]] = newUVs[quads[i + 3]];
            }
            f++;
        }

        mesh.uv = newUVs;
    }

    private float areaOfFace(int face)
    {
        Vector3 a = verts[face * 4 + MeshEdit.quadEdgePatternClockwise[0]];
        Vector3 ab = verts[face * 4 + MeshEdit.quadEdgePatternClockwise[2]] - a;
        Vector3 ac = verts[face * 4 + MeshEdit.quadEdgePatternClockwise[4]] - a;
        Vector3 d = verts[face * 4 + MeshEdit.quadEdgePatternClockwise[6]];
        Vector3 dc = verts[face * 4 + MeshEdit.quadEdgePatternClockwise[4]] - d;
        Vector3 db = verts[face * 4 + MeshEdit.quadEdgePatternClockwise[2]] - d;
        float areaOfTriangleA = Vector3.Cross(ab, ac).magnitude;
        float areaOfTriangleB = Vector3.Cross(dc, db).magnitude;

        return (areaOfTriangleA + areaOfTriangleB) / 2.0f;
    }

    private float areaOf2DFace(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        Vector2 ab = b - a;
        Vector2 ac = c - a;
        Vector2 dc = c - d;
        Vector2 db = b - d;
        MeshEditWindow.cross(ab, ac);
        float areaOfTriangleA = Mathf.Abs(MeshEditWindow.cross(ab, ac));
        float areaOfTriangleB = Mathf.Abs(MeshEditWindow.cross(dc, db));

        return (areaOfTriangleA + areaOfTriangleB) / 2.0f;
    }

    private Vector3[] getTriangleCoefficients(List<Vector3> verts, List<int> simpleVerts, List<int> simpleTris)
    {
        return null;
    }
    
    public float[,] mxMultiply(float[,] a, float[,] b)
    {
        int widthA = a.GetLength(0);
        int heightA = a.GetLength(1);

        int widthB = b.GetLength(0);
        int heightB = b.GetLength(1);

        if (widthA != heightB)
        {
            Debug.LogError("mxMultiplyError! A [" + heightA + " x " + widthA + "] x B [" + heightB + " x " + widthB + "] is not defined.");
            return null;
        }

        int commonLength = heightB;

        float[,] result = new float[widthB, heightA];

        for (int y = 0; y < heightA; y++)
        {
            for (int x = 0; x < widthB; x++)
            {
                float sum = 0;
                for (int i = 0; i < commonLength; i++)
                {
                    sum += a[i, y] * b[x, i];
                }

                result[x, y] = sum;
            }

        }

        return result;
    }
    public double[,] mxMultiply(double[,] a, double[,] b)
    {
        int widthA = a.GetLength(0);
        int heightA = a.GetLength(1);

        int widthB = b.GetLength(0);
        int heightB = b.GetLength(1);

        if (widthA != heightB)
        {
            Debug.LogError("mxMultiplyError! A width (" + widthA + ") and B height (" + heightB + ") are not equal.");
            return null;
        }

        int commonLength = heightB;

        double[,] result = new double[widthB, heightA];

        for (int y = 0; y < heightA; y++)
        {
            for (int x = 0; x < widthB; x++)
            {
                double sum = 0;
                for (int i = 0; i < commonLength; i++)
                {
                    sum += a[i, y] * b[x, i];
                }

                result[x, y] = sum;
            }

        }

        return result;
    }
    public float[,] mxMultiply(float a, float[,] b)
    {
        int width = b.GetLength(0);
        int height = b.GetLength(1);

        float[,] result = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = a * b[x, y];
            }

        }

        return result;
    }
    public double[,] mxMultiply(double a, double[,] b)
    {
        int width = b.GetLength(0);
        int height = b.GetLength(1);

        double[,] result = new double[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = a * b[x, y];
            }

        }

        return result;
    }
    public float[,] mxSubtract(float[,] a, float[,] b)
    {
        int widthA = a.GetLength(0);
        int heightA = a.GetLength(1);

        int widthB = b.GetLength(0);
        int heightB = b.GetLength(1);

        if (widthA != widthB ||
            heightA != heightB)
        {
            return null;
        }

        float[,] value = new float[widthA, heightA];

        for (int y = 0; y < heightA; y++)
        {
            for (int x = 0; x < widthA; x++)
            {
                value[x, y] = a[x, y] - b[x, y];
            }
        }

        return value;
    }
    public double[,] mxSubtract(double[,] a, double[,] b)
    {
        int widthA = a.GetLength(0);
        int heightA = a.GetLength(1);

        int widthB = b.GetLength(0);
        int heightB = b.GetLength(1);

        if (widthA != widthB ||
            heightA != heightB)
        {
            return null;
        }

        double[,] value = new double[widthA, heightA];

        for (int y = 0; y < heightA; y++)
        {
            for (int x = 0; x < widthA; x++)
            {
                value[x, y] = a[x, y] - b[x, y];
            }
        }

        return value;
    }
    public float[,] mxAdd(float[,] a, float[,] b)
    {
        int widthA = a.GetLength(0);
        int heightA = a.GetLength(1);

        int widthB = b.GetLength(0);
        int heightB = b.GetLength(1);

        if (widthA != widthB ||
            heightA != heightB)
        {
            return null;
        }

        float[,] value = new float[widthA, heightA];

        for (int y = 0; y < heightA; y++)
        {
            for (int x = 0; x < widthA; x++)
            {
                value[x, y] = a[x, y] + b[x, y];
            }
        }

        return value;
    }
    public double[,] mxAdd(double[,] a, double[,] b)
    {
        int widthA = a.GetLength(0);
        int heightA = a.GetLength(1);

        int widthB = b.GetLength(0);
        int heightB = b.GetLength(1);

        if (widthA != widthB ||
            heightA != heightB)
        {
            return null;
        }

        double[,] value = new double[widthA, heightA];

        for (int y = 0; y < heightA; y++)
        {
            for (int x = 0; x < widthA; x++)
            {
                value[x, y] = a[x, y] + b[x, y];
            }
        }

        return value;
    }
    public T[,] mxCopy<T>(T[,] a)
    {
        int widthA = a.GetLength(0);
        int heightA = a.GetLength(1);
        
        T[,] value = new T[widthA, heightA];

        for (int y = 0; y < heightA; y++)
        {
            for (int x = 0; x < widthA; x++)
            {
                value[x, y] = a[x, y];
            }
        }

        return value;
    }
    public float[,] mxTranspose(float[,] matrix)
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        float[,] t = new float[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                t[y, x] = matrix[x, y];
            }
        }

        return t;
    }
    public T[,] mxTranspose<T>(T[,] matrix)
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        T[,] t = new T[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                t[y, x] = matrix[x, y];
            }
        }

        return t;
    }

    public T[,] mxInverse<T>(T[,] matrix)
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        T[,] t = new T[height, width];

        return t;
    }
    public float mxDeterminant<T>(T[,] matrix)
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        return 0.0f;
    }

    public void mxLog(float[,] matrix, string name = "", string floatFormat = "f4")
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        string row = name + " Matrix [" + height + ", " + width + "]\n";
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                row += " " + matrix[x, y].ToString(floatFormat) + "  ";
            }
            row += "\n";
        }
        Debug.Log(row);
    }

    public void mxLog(double[,] matrix, string name = "", string floatFormat = "f8")
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        string row = name + " Matrix [" + height + ", " + width + "]\n";
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                row += " " + matrix[x, y].ToString(floatFormat) + "  ";
            }
            row += "\n";
        }
        Debug.Log(row);
    }

    public double[,] mxCastToDouble(float[,] matrix)
    {
        int width = matrix.GetLength(0);
        int height = matrix.GetLength(1);

        double[,] value = new double[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                value[x, y] = (double)matrix[x, y];
            }
        }

        return value;
    }
    public float[,] solveConjugateGradient(float[,] A, float[,] b, float[,] x, bool logSteps = false)
    {
        beginTimer("SolveConjugateGradient method");
        if (logSteps)
        {
            Debug.Log("####Logging Conjugate Gradient...");

            mxLog(A, "A =");
            mxLog(b, "b =");
            mxLog(x, "x =");
        }
        float[,] Ax = mxMultiply(A, x);

        if (logSteps)
        {
            mxLog(Ax, "A * x = Ax =");
        }

        float[,] r = mxSubtract(b, Ax);

        if (logSteps)
        {
            mxLog(r, "r = b - Ax =");
        }

        float[,] p = mxCopy(r);

        float[,] rsold = mxMultiply(mxTranspose(r), r);

        for (int k = 0; k < b.Length; k++)
        {
            beginTimer("Iteration " + k);
            // Define a(k)
            float[,] Ap = mxMultiply(A, p);
            if (logSteps)
            {
                mxLog(rsold, "rsold: ", "f16");
                mxLog(mxMultiply(mxTranspose(p), Ap), "div: ");
            }
            float alphaK = rsold[0, 0] / mxMultiply(mxTranspose(p), Ap)[0, 0];

            // Define x(k + 1)
            x = mxAdd(x, mxMultiply(alphaK, p));
            // Define r(k + 1)
            r = mxSubtract(r, mxMultiply(alphaK, Ap));

            float[,] rsnew = mxMultiply(mxTranspose(r), r);

            // if r(k + 1) is small enough, break and return
            float e = 0.000001f;
            if (Mathf.Sqrt(rsnew[0, 0]) < e)
            {
                endTimer();
                break;
            }

            p = mxAdd(r, mxMultiply(rsnew[0, 0] / rsold[0, 0], p));
            rsold = mxCopy(rsnew);

            endTimer();
        }

        endTimer();
        return x;
    }


    public double[,] solveConjugateGradient(double[,] A, double[,] b, double[,] x, bool logSteps = false)
    {
        if (logSteps)
        {
            Debug.Log("####Logging Conjugate Gradient...");

            mxLog(A, "A =");
            mxLog(b, "b =");
            mxLog(x, "x =");
        }
        double[,] Ax = mxMultiply(A, x);

        if (logSteps)
        {
            mxLog(Ax, "A * x = Ax =");
        }

        double[,] r = mxSubtract(b, Ax);

        if (logSteps)
        {
            mxLog(r, "r = b - Ax =");
        }

        double[,] p = mxCopy(r);

        double[,] rsold = mxMultiply(mxTranspose(r), r);

        for (int k = 0; k < b.Length; k++)
        {
            // Define a(k)
            double[,] Ap = mxMultiply(A, p);
            mxLog(rsold, "rsold: ");
            mxLog(mxMultiply(mxTranspose(p), Ap), "div: ");
            double alphaK = rsold[0, 0] / mxMultiply(mxTranspose(p), Ap)[0, 0];

            // Define x(k + 1)
            x = mxAdd(x, mxMultiply(alphaK, p));
            // Define r(k + 1)
            r = mxSubtract(r, mxMultiply(alphaK, Ap));

            double[,] rsnew = mxMultiply(mxTranspose(r), r);

            // if r(k + 1) is small enough, break and return
            if (Math.Sqrt(rsnew[0, 0]) < 1e-10)
            {
                break;
            }

            p = mxAdd(r, mxMultiply(rsnew[0, 0] / rsold[0, 0], p));
            rsold = mxCopy(rsnew);
        }

        return x;
    }
    public void testMatrixOps()
    {

        float[,] A = new float[,] { { 0, 2, 4, 6 }, { 1, 3, 5, 7 } };
        float[,] B = new float[,] { { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
        mxLog(A, "A: ");
        mxLog(B, "B: ");
        mxLog(mxMultiply(A, B), "A x B: ");
        mxLog(mxTranspose(A), "At: ");
        mxLog(mxSubtract(mxTranspose(A), B), "At - B");
        mxLog(mxAdd(mxTranspose(A), B), "At + B");
        mxLog(mxMultiply(4, A), "4A");
        Debug.Log("ReferenceTest");
        float[,] C = A;
        A[0, 0] = 1000;
        mxLog(C, "C: ");



        A = new float[,] { { 0, 2, 4 }, { 1, 3, 5 } };
        B = new float[,] { { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
        mxLog(A, "A: ");
        mxLog(B, "B: ");
        mxLog(mxMultiply(A, B), "A x B: ");
    }
    public void testConjugateGradientSolver()
    {
        //                        x y  
        float[,] A = new float[,] { { 8, 1, 7, 999 }, { 1, 0, 0, 0 }, { 7, 0, 0, 0 }, { 7, 0, 0, 0 } };

        float[,] b = new float[,] { {1, 2, 3, 4} };
        //float[,] x0 = new float[,] { { 0.00403f, 0.3226f, 0.0461f, 0.0461f } };
        float[,] x0 = new float[,] { { 0, 0, 0, 0 } };

        /*
        A = new float[,] { { 4, 1 }, { 1, 3 } };
        b = new float[,] { { 1, 2 } };
        x0 = new float[,] { { 0, 0 } };*/
        //A = mxTranspose(A);
        float[,] aT = mxTranspose(A);
        
        Debug.Log("Conjugate Gradient Result T: ");
        // CGNR Method for solving MxN matrices (Ax = b) -> (At*Ax = At*b). Slows calculation time, sensitive to roundoff errors, requires a pre-conditioner!
        mxLog(solveConjugateGradient(mxMultiply(aT, A), mxMultiply(aT, b), x0, false));
        /*
        double[,] Adouble = mxCastToDouble(A);
        double[,] ATdouble = mxCastToDouble(aT);
        double[,] bDouble = mxCastToDouble(b);
        double[,] x0Double = mxCastToDouble(x0);

        x0Double = solveConjugateGradient(mxMultiply(ATdouble, Adouble), mxMultiply(ATdouble, bDouble), x0Double, false);
        mxLog(solveConjugateGradient(mxMultiply(ATdouble, Adouble), mxMultiply(ATdouble, bDouble), x0Double, false), "Doubled2: ");
        */
        /*
        Debug.Log("Conjugate Gradient Result with x0 = [0, 0]: ");
        mxLog(solveConjugateGradient(A, b, new float[,] { { 0, 0 } }, true));*/
        Debug.Log("Conjugate Gradient Result of padded Matrices: ");




        // Test case for warped square
        // verts are 0, 1, 2, 3. 0 and 1 are pinned
        float coeff45 = 2.0f / Mathf.Sqrt(0.5f);
        float coeff123A = 2.0f / Mathf.Sqrt(2);
        float coeff123B = 2.0f;
        Vector3 t0v0 = new Vector3(0, coeff45, 0);
        Vector3 t0v1 = new Vector3(-coeff45, -coeff45, 0);
        Vector3 t0v2 = new Vector3(coeff45, 0, 0);
        Vector3 t1v1 = new Vector3(0, coeff123A, 0);
        Vector3 t1v2 = new Vector3(-coeff123B, 0, 0);
        Vector3 t1v3 = new Vector3(coeff123B, -coeff123A, 0);
        // A accounts for verts 2 & 3 accross triangles 0 and 1
        

        float[,] B = new float[,]
        {
            { t0v0.x, 0,        t0v0.y, 0 },
            { t0v1.x, t1v1.x,   t0v1.y, t1v1.y },

            { -t0v0.y, 0,       t0v0.x, 0 },
            { -t0v1.y, -t1v1.y, t0v1.x, t1v1.x }
        };
        A = new float[,]
        {
            { t0v2.x, t1v2.x,   t0v2.y, t1v2.y },
            { 0, t1v3.x,        0, t1v3.y },

            { -t0v2.y, -t1v2.y,   t0v2.x, t1v2.x },
            { 0, -t1v3.y,        0, t1v3.x },

        };

        float[,] pinUVs = new float[,] { { 0, 1, 0, 0 } };
        b = mxMultiply(B, pinUVs);
        x0 = new float[,] { { 0, 0, 0, 0 } };

        mxLog(A, "Test case A: ");
        mxLog(b, "Test case b: ");

        mxLog(solveConjugateGradient(A, b, x0, false), "Test Case solution: ");
        mxLog(mxMultiply(0.5f, solveConjugateGradient(A, b, x0, false)), "Test Case solution div 2: ");
        aT = mxTranspose(A);
        mxLog(solveConjugateGradient(mxMultiply(aT, A), mxMultiply(aT, b), x0, false), "Test Case solution with transposed T: ");
        mxLog(solveConjugateGradient(mxMultiply(aT, A), mxMultiply(aT, b), x0, false), "Test Case solution with padded spaces: ");





        List<Vector2> pinnedUVs = new List<Vector2>() { new Vector2(0, 0), new Vector2(1.0f, 0.0f) };
        List<int> simpleVerts = new List<int>() { 0, 1, 2, 3 };
        List<int> simpleTris = new List<int>() { 0, 1, 2, 3, 2, 1 };
        bool[] isPinned = new bool[] { false, false, true, true };
        int tCount = 2;
        int freeVerts = 2;
        int pinnedVerts = 2;
        List<Vector3> verts = new List<Vector3>() { new Vector3(0, 0, 0), new Vector3(1, 0, 0) , new Vector3(0, 1, 0) , new Vector3(1, 1, 0) };

        Vector3[] tc = getTriangleCoefficients(verts, simpleVerts, simpleTris);

        for (int i =0; i < tc.Length; i+= 3)
        {
            string log = tc[0].ToString("f4") + ", " + tc[1].ToString("f4") +", "+ tc[2].ToString("f4");
            Debug.Log("tc[" + i + " - " + (i + 2) + "]: " + log);
        }
        /*

                    sparseMatrixPinned[i, vertIndex] = coefficient[i * 3 + j].x;
                    sparseMatrixPinned[triangleCount + i, pinnedUVCount + vertIndex] = coefficient[i * 3 + j].x;
                    sparseMatrixPinned[i, pinnedUVCount + vertIndex] = -coefficient[i * 3 + j].y;
                    sparseMatrixPinned[triangleCount + i, vertIndex] = coefficient[i * 3 + j].y;

            */
        A = new float[tCount * 2, freeVerts * 2];
        float[,] pinned = new float[tCount * 2, pinnedVerts * 2];

        b = new float[1, pinnedVerts * 2];
        for (int i = 0; i < tCount; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int v = simpleTris[i * 3 + j];
                if (!isPinned[v])
                {
                    A[i, v] = tc[i * 3 + j].x;
                    A[tCount + i, freeVerts + v] = tc[i * 3 + j].x;
                    A[i, freeVerts + v] = -tc[i * 3 + j].y;
                    A[tCount + i, v] = tc[i * 3 + j].y;
                }
                else
                {
                    v -= 2;

                    pinned[i, v] = tc[i * 3 + j].x;
                    pinned[tCount + i, pinnedVerts + v] = tc[i * 3 + j].x;
                    pinned[i, pinnedVerts + v] = -tc[i * 3 + j].y;
                    pinned[tCount + i, v] = tc[i * 3 + j].y;

                    b[0, v] = pinnedUVs[v].x;
                    b[0, pinnedVerts + v] = pinnedUVs[v].y;
                }
            }
        }
        b = mxMultiply(pinned, b);
        b = mxMultiply(-1, b);

        x0 = new float[1, freeVerts * 2];

        x0 = solveConjugateGradient(A, b, x0, true);
        mxLog(x0, "UVs: ");

        
    }

    public double[] Solve(
        double epsilon,
        double[] b,
        double[] x0,
        double[,] A,
        int n)
    {
        double alpha0 = 0, pq = 0;
        double beta0 = 0;
        double rho0 = 0, rho1 = 0;
        double[] p0 = new double[n];
        double[] p1 = new double[n];
        double[] q0 = new double[n];
        double[] q1 = new double[n];
        double[] r0 = new double[n];
        double[] r1 = new double[n];
        double[] x1 = new double[n];
        double[] z0 = new double[n];
        double[] z1 = new double[n];
        int k = 1;

        double residual = 0;

        for (int i = 0; i < n; i++)
        {
            double sum = 0;

            for (int j = 0; j < n; j++)
                sum += A[i, j] * x0[j];

            r0[i] = sum - b[i];
            residual += r0[i] * r0[i];
        }

        residual = Math.Sqrt(residual / n);

        if (residual < epsilon)
            return x0;

        while (true)
        {

            // choose the point-preconditioner 

            for (int i = 0; i < n; i++)
                z0[i] = r0[i] / A[i, i];

            rho1 = 0;

            for (int i = 0; i < n; i++)
                rho1 += r0[i] * z0[i];

            if (k == 1)
            {
                for (int i = 0; i < n; i++)
                    p0[i] = -z0[i];

                rho0 = rho1;
            }

            else
            {
                beta0 = rho1 / rho0;

                for (int i = 0; i < n; i++)
                {
                    p1[i] = -z0[i] - beta0 * p0[i];
                    p0[i] = p1[i];
                }

                rho0 = rho1;
            }

            for (int i = 0; i < n; i++)
            {
                double sum = 0;

                for (int j = 0; j < n; j++)
                    sum += A[i, j] * p0[j];

                q1[i] = sum;
            }

            pq = 0;

            for (int i = 0; i < n; i++)
                pq += p0[i] * q1[i];

            alpha0 = rho0 / pq;

            if (k == 1)
            {
                for (int i = 0; i < n; i++)
                    x1[i] = x0[i] + alpha0 * p0[i];
            }

            else
            {
                for (int i = 0; i < n; i++)
                    x1[i] = x0[i] + alpha0 * p1[i];
            }

            for (int i = 0; i < n; i++)
                r1[i] = r0[i] + alpha0 * q1[i];

            residual = 0;

            for (int i = 0; i < n; i++)
            {
                double sum = 0;

                for (int j = 0; j < n; j++)
                    sum += A[i, j] * x1[j];

                r0[i] = sum - b[i];
                x0[i] = x1[i];
                residual += Math.Pow(r0[i], 2);
            }

            residual /= n;

            if (residual < epsilon)
                break;

            k++;
        }

        return x0;
    }

    public void pushNewGeometry()
    {
        // PushNewGeometry requires that the following lists be valid:
        //  verts, vertNomrals, tris, colours
        beginTimer("PushNewGeometry");
        List<Vector3> vertsNormalised = new List<Vector3>();

        Vector3 p = gameObject.transform.position;
        Quaternion r = gameObject.transform.rotation;
        Vector3 s = gameObject.transform.lossyScale;

        //checkTransforms();
        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 v = verts[i];
            v -= p;

            v = Quaternion.Inverse(r) * v;

            s.x = Mathf.Max(Mathf.Abs(s.x), minimumScale) * Mathf.Sign(s.x);
            s.y = Mathf.Max(Mathf.Abs(s.y), minimumScale) * Mathf.Sign(s.y);
            s.z = Mathf.Max(Mathf.Abs(s.z), minimumScale) * Mathf.Sign(s.z);

            v.x /= s.x;
            v.y /= s.y;
            v.z /= s.z;

            if (float.IsNaN(v.x))
            {
                Debug.Log("NANWarning!!" + "verts[i].x = " + verts[i].x + ", p.x = " + p.x + ", s.x " + s.x.ToString());
            }

            vertsNormalised.Add(v);
        }

        if (_mesh == null)
        {
            _mesh = new Mesh();
        }

        _mesh.triangles = new int[0];

        _mesh.vertices = vertsNormalised.ToArray();
        _mesh.normals = meshNormals.ToArray();

        //        _mesh.triangles = tris.ToArray();
        setTriangles();

        if (materialUVMap != null && materialUVMap.Count > 0)
        {
            if (materialUVMap[selectedMaterial] >= 0 &&
                materialUVMap[selectedMaterial] < uvMaps.Count)
            {
                //_mesh.uv = uvMaps[materialUVMap[selectedMaterial]].uvs;
            }
            if (_mesh.uv == null)
            {
                _mesh.uv = new Vector2[_mesh.vertices.Length];
            }
            else
            {
                _mesh.uv = getCombinedUVSFromMaterials();
            }
        }

        _mesh.colors = colours.ToArray();

        //recalculateNormals(_mesh);
        _mesh.RecalculateBounds();

        pushLocalMeshToGameObject();

        endTimer();
    }

    public Vector2[] getCombinedUVSFromMaterials()
    {
        Vector2[] newUVs = new Vector2[quads.Count];


        for (int i = 0; i < quadMaterial.Count; i++)
        {
            if (materialUVMap[quadMaterial[i]] >= 0 &&
                uvMaps[materialUVMap[quadMaterial[i]]].uvs != null &&
                uvMaps[materialUVMap[quadMaterial[i]]].uvs.Length == mesh.uv.Length)
            {
                newUVs[quads[i * 4 + 0]] = uvMaps[materialUVMap[quadMaterial[i]]].uvs[quads[i * 4 + 0]];
                newUVs[quads[i * 4 + 1]] = uvMaps[materialUVMap[quadMaterial[i]]].uvs[quads[i * 4 + 1]];
                newUVs[quads[i * 4 + 2]] = uvMaps[materialUVMap[quadMaterial[i]]].uvs[quads[i * 4 + 2]];
                newUVs[quads[i * 4 + 3]] = uvMaps[materialUVMap[quadMaterial[i]]].uvs[quads[i * 4 + 3]];
            }
            else if (materialUVMap[quadMaterial[i]] == -1)
            {
                newUVs[quads[i * 4 + 0]] = customTextureUVMap[quads[i * 4 + 0]];
                newUVs[quads[i * 4 + 1]] = customTextureUVMap[quads[i * 4 + 1]];
                newUVs[quads[i * 4 + 2]] = customTextureUVMap[quads[i * 4 + 2]];
                newUVs[quads[i * 4 + 3]] = customTextureUVMap[quads[i * 4 + 3]];
            }
            else if ( mesh.uv.Length == quads.Count)
            {
                newUVs[quads[i * 4 + 0]] = mesh.uv[quads[i * 4 + 0]];
                newUVs[quads[i * 4 + 1]] = mesh.uv[quads[i * 4 + 1]];
                newUVs[quads[i * 4 + 2]] = mesh.uv[quads[i * 4 + 2]];
                newUVs[quads[i * 4 + 3]] = mesh.uv[quads[i * 4 + 3]];
            }
        }

        return newUVs;
    }

    public void pushUVData()
    {
        mesh.uv = getCombinedUVSFromMaterials();

        pushLocalMeshToGameObject();
    }

    public void pushColour()
    {
        mesh.colors = colours.ToArray();

        pushLocalMeshToGameObject();
    }

    public void pushLocalMeshToGameObject()
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh = this.mesh;
        }
    }
    public void pushModifiedMeshToGameObject()
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh = this.meshModified;
        }
    }

    
    public string getCopyString(bool[] selectedFaces)
    {
        // Things you have to copy:
        // Verts, in the order that they will be arranged into quads and triangles
        // ConnectedVerts
        // AdjacentFaces
        // Vert colour
        // UV coords for each map

        // Tris?
        string vertString = "v";
        string connectedVertsString = ""; // x
        string materialString = "m";
        string colourString = "c";
        string customTextureUVString = "t";
        string[] uvString = new string[uvMaps.Count]; // u



        // Mask the existing "connectedVerts" over only the verts in faces that are selected
        List<ListWrapper> newConnectedFaces = new List<ListWrapper>();

        // Get the list of new vertices, in the order they should be in the copied data.
        int[] vertMap = new int[verts.Count];
        int c = 0;
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedFaces[i])
            {
                vertMap[quads[i * 4 + 0]] = c;
                c++;
                vertMap[quads[i * 4 + 1]] = c;
                c++;
                vertMap[quads[i * 4 + 2]] = c;
                c++;
                vertMap[quads[i * 4 + 3]] = c;
                c++;
            }
        }
        
        for (int i = 0; i < selectedFaces.Length; i++)
        {
            if (selectedFaces[i])
            {
                // Materials on Quad
                materialString += quadMaterial[i] + ",";

                for (int q = 0; q < 4; q++)
                {
                    // TODO: COMPLETELY NORMALISE THESE POSITIONS BEFORE SAVIG THEM! rOTATION AND (Maybe not?)SCALE ARE IMPORTANT AS WELL

                    // Verts
                    vertString += (verts[quads[i * 4 + q]].x - transform.position.x) + ",";
                    vertString += (verts[quads[i * 4 + q]].y - transform.position.y) + ",";
                    vertString += (verts[quads[i * 4 + q]].z - transform.position.z) + ",";

                    // Custom Texture UVs
                    customTextureUVString += (customTextureUVMap[quads[i * 4 + q]].x) + ",";
                    customTextureUVString += (customTextureUVMap[quads[i * 4 + q]].y) + ",";

                    // Colours
                    colourString += colours[quads[i * 4 + q]].r + ",";
                    colourString += colours[quads[i * 4 + q]].g + ",";
                    colourString += colours[quads[i * 4 + q]].b + ",";
                    colourString += colours[quads[i * 4 + q]].a + ",";

                    // Connected Verts
                    connectedVertsString += "x";
                    ListWrapper connectedVertsCulled = new ListWrapper();
                    for (int j = 0; j < connectedVerts[quads[i * 4 + q]].Count; j++)
                    {
                        int vert = connectedVerts[quads[i * 4 + q]].list[j];
                        int face = getFaceFromVert(vert);

                        if (selectedFaces[face])
                        {
                            connectedVertsCulled.Add(vertMap[vert]);
                        }

                    }

                    for (int j = 0; j < connectedVertsCulled.Count; j++)
                    {
                        connectedVertsString += connectedVertsCulled.list[j] + ",";
                    }

                    // Uvs
                    for (int u = 0; u < _uvMaps.Count; u++)
                    {

                        uvString[u] += _uvMaps[u].uvs[quads[i * 4 + q]].x + ",";
                        uvString[u] += _uvMaps[u].uvs[quads[i * 4 + q]].y + ",";
                        uvString[u] += _uvMaps[u].uvAnimationLength[quads[i * 4 + q]] + ",";
                        uvString[u] += _uvMaps[u].uvAnimationIndex[quads[i * 4 + q]] + ",";
                        uvString[u] += _uvMaps[u].uvAnimationStartPosition[quads[i * 4 + q]] + ",";
                        uvString[u] += _uvMaps[u].uvAnimationQuadPoint[quads[i * 4 + q]] + ",";

                    }
                }
            }
        }
        string data = vertString + colourString + customTextureUVString + connectedVertsString + materialString;// + uvString;
        for (int i = 0; i < _uvMaps.Count; i++)
        {
            // Double all commas in the name and reduce on output.
            string uvName = "u" + _uvMaps[i].name.Replace(",", ",,") + ",";
            data += uvName + uvString[i];
        }
        return data;
    }
    public bool pasteData(string copyData)
    {
        List<Vector3> newVerts = new List<Vector3>();
        List<Color> newColours = new List<Color>();
        List<ListWrapper> newConnectedVerts = new List<ListWrapper>();
        List<int> newMaterials = new List<int>();

        // UV Info
        List<string> names = new List<string>();
        List<Vector2> newCustomTextureUVs = new List<Vector2>();
        List<List<Vector2>> newUVs = new List<List<Vector2>>();
        List<List<int>> newAnimationStartPoint = new List<List<int>>();
        List<List<int>> newAnimationLength = new List<List<int>>();
        List<List<int>> newAnimationIndex = new List<List<int>>();
        List<List<int>> newQuadPoints = new List<List<int>>();
        // Datatype = 0: newVerts
        //            1: newColours
        //            2: newConnectedVerts
        int dataType = 0;
        int argument = 0;


        int vertCount = verts.Count;
        int faceCount = quads.Count / 4;
        try
        {
            if (copyData[0] != 'v')
            {
                return false;
            }
            int currentIndex = 1;

            string val = "";
            float x = 0.0f, y = 0.0f, z = 0.0f, w = 0.0f;
            while (currentIndex < copyData.Length)
            {
                if (copyData[currentIndex] == 'c')
                {
                    dataType = 1;
                    val = "";
                    currentIndex++;
                    continue;
                }
                else if (copyData[currentIndex] == 'x')
                {
                    dataType = 2;
                    val = "";
                    currentIndex++;
                    newConnectedVerts.Add(new ListWrapper());
                    continue;
                }
                else if (copyData[currentIndex] == 'm')
                {
                    dataType = 3;
                    val = "";
                    currentIndex++;
                    continue;
                }
                else if (copyData[currentIndex] == 't')
                {
                    dataType = 4;
                    val = "";
                    currentIndex++;
                    continue;
                }
                else if (copyData[currentIndex] == 'u')
                {
                    // Move on to the next loop
                    break;
                }
                if (copyData[currentIndex] == ',')
                {
                    // Add val to the vector
                    if (argument == 0)
                    {
                        if (dataType == 2)
                        {
                            int vert = -1;
                            if (int.TryParse(val, out vert))
                            {
                                newConnectedVerts[newConnectedVerts.Count - 1].Add(vert + vertCount);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else if (!float.TryParse(val, out x))
                        {
                            return false;
                        }
                    }
                    else if (argument == 1)
                    {
                        if (!float.TryParse(val, out y))
                        {
                            return false;
                        }
                    }
                    else if (argument == 2)
                    {
                        if (!float.TryParse(val, out z))
                        {
                            return false;
                        }
                    }
                    else if (argument == 3 && dataType == 1)
                    {
                        if (!float.TryParse(val, out w))
                        {
                            return false;
                        }
                    }
                    // Increment argument & index
                    if (dataType == 0 && argument == 2)
                    {
                        newVerts.Add(new Vector3(x, y, z));
                        argument = 0;

                    }
                    else if (dataType == 1 && argument == 3)
                    {
                        newColours.Add(new Color(x, y, z, w));
                        argument = 0;

                    }
                    else if (dataType == 3)
                    {
                        newMaterials.Add((int)x);
                        argument = 0;
                    }
                    else if (dataType == 4 && argument == 1)
                    {
                        newCustomTextureUVs.Add(new Vector2(x, y));
                        argument = 0;
                    }
                    else if (dataType != 2)
                    {
                        argument++;
                    }

                    val = "";
                    currentIndex++;
                    continue;

                }

                val += copyData[currentIndex];

                // Increment position
                currentIndex++;
            }

            // UVs
            val = "";
            argument = 0;
            Vector2 newVert = Vector2.zero;

            while (currentIndex < copyData.Length)
            {
                if (copyData[currentIndex] == 'u')
                {
                    newUVs.Add(new List<Vector2>());
                    newAnimationIndex.Add(new List<int>());
                    newQuadPoints.Add(new List<int>());
                    newAnimationStartPoint.Add(new List<int>());
                    newAnimationLength.Add(new List<int>());

                    currentIndex++;
                    string name = "";
                    // You don't have to check for an infinite loop as long as an error will be produced if you go over. Since there's an error catcher that will exit the method.
                    // Two commas: fine
                    // One comma: line break
                    while (currentIndex < copyData.Length)
                    {
                        // Read all duplicated commas
                        bool foundComma = false;
                        int commaCount = 0;
                        string commaString = "";
                        while (currentIndex < copyData.Length &&
                            copyData[currentIndex] == ',')
                        {
                            foundComma = true;
                            commaCount++;
                            currentIndex++;
                        }
                        while (commaCount > 1)
                        {
                            commaCount -= 2;
                            commaString += ",";
                        }

                        // Add new characters to name
                        if (foundComma)
                        {
                            name += commaString;
                        }
                        else
                        {

                            name += copyData[currentIndex];
                            currentIndex++;
                        }

                        // An excess comma means the end of the name segment
                        if (commaCount > 0)
                        {
                            break;
                        }
                    }

                    names.Add(name);
                    argument = 0;
                    continue;
                }
                else if (copyData[currentIndex] == ',')
                {
                    if (argument == 0)
                    {
                        // Vert x
                        float vx = 0;
                        if (!float.TryParse(val, out vx))
                        {
                            return false;
                        }
                        newVert.x = vx;
                    }
                    else if (argument == 1)
                    {
                        // Vert y
                        float vy = 0;
                        if (!float.TryParse(val, out vy))
                        {
                            return false;
                        }
                        newVert.y = vy;
                        newUVs[newUVs.Count - 1].Add(newVert);
                    }
                    else if (argument == 2)
                    {

                        int animStartPoint = 0;
                        if (!int.TryParse(val, out animStartPoint))
                        {
                            return false;
                        }
                        newAnimationStartPoint[newUVs.Count - 1].Add(animStartPoint);
                    }
                    else if (argument == 3)
                    {

                        int animIndex = 0;
                        if (!int.TryParse(val, out animIndex))
                        {
                            return false;
                        }
                        newAnimationIndex[newUVs.Count - 1].Add(animIndex);
                    }
                    else if (argument == 4)
                    {

                        int animLength = 0;
                        if (!int.TryParse(val, out animLength))
                        {
                            return false;
                        }
                        newAnimationLength[newUVs.Count - 1].Add(animLength);
                    }
                    else if (argument == 5)
                    {

                        int animQuadPoint = 0;
                        if (!int.TryParse(val, out animQuadPoint))
                        {
                            return false;
                        }
                        newQuadPoints[newUVs.Count - 1].Add(animQuadPoint);

                        val = "";
                        argument = 0;
                        currentIndex++;
                        continue;
                    }
                    val = "";
                    argument++;
                    currentIndex++;
                    continue;
                }

                val += copyData[currentIndex];
                currentIndex++;
            }
        }
        catch (Exception e)
        {
            if (debugMode != DebugShowInfo.Off)
            {
                Debug.Log("error: " + e.ToString());
            }
            return false;
        }

        // Materials
        for (int i = 0; i < newMaterials.Count; i++)
        {
            // mask out any materials that aren't in the mesh
            if (newMaterials[i] < materials.Count)
            {
                quadMaterial.Add(newMaterials[i]);
            }
            else
            {
                quadMaterial.Add(0);
            }
        }

        for (int i = 0; i < newVerts.Count; i += 4)
        {
            verts.Add(newVerts[i + 0] + transform.position);
            verts.Add(newVerts[i + 1] + transform.position);
            verts.Add(newVerts[i + 2] + transform.position);
            verts.Add(newVerts[i + 3] + transform.position);

            quads.Add(vertCount + i + 0);
            quads.Add(vertCount + i + 1);
            quads.Add(vertCount + i + 2);
            quads.Add(vertCount + i + 3);
            
            tris.Add(vertCount + i + 0);
            tris.Add(vertCount + i + 1);
            tris.Add(vertCount + i + 2);
            tris.Add(vertCount + i + 3);
            tris.Add(vertCount + i + 2);
            tris.Add(vertCount + i + 1);

            // Connected Verts
            connectedVerts.Add(newConnectedVerts[i + 0]);
            connectedVerts.Add(newConnectedVerts[i + 1]);
            connectedVerts.Add(newConnectedVerts[i + 2]);
            connectedVerts.Add(newConnectedVerts[i + 3]);

            Vector3 n = Vector3.Cross(
                newVerts[1] - newVerts[0], newVerts[2] - newVerts[0]).normalized;

            vertNormals.Add(n);
            vertNormals.Add(n);
            vertNormals.Add(n);
            vertNormals.Add(n);

            meshNormals.Add(n);
            meshNormals.Add(n);
            meshNormals.Add(n);
            meshNormals.Add(n);

            customTextureUVMap.Add(newCustomTextureUVs[i + 0]);
            customTextureUVMap.Add(newCustomTextureUVs[i + 1]);
            customTextureUVMap.Add(newCustomTextureUVs[i + 2]);
            customTextureUVMap.Add(newCustomTextureUVs[i + 3]);

            faceNormals.Add(n);
            halfEdges.Add(new MeshEditFaceEdges());

            colours.Add(newColours[i + 0]);
            colours.Add(newColours[i + 1]);
            colours.Add(newColours[i + 2]);
            colours.Add(newColours[i + 3]);

            // UVs
        }
        // There's no guarantee that the model you copy your verts into will be the same as the one you copied them from
        for (int i = 0; i < uvMaps.Count; i++)
        {
            int indexOfNewUVs = names.IndexOf(uvMaps[i].name);
            if (indexOfNewUVs >= 0)
            {
                int startPoint = uvMaps[i].uvs.Length;
                uvMaps[i].resizeUVLength(verts.Count);

                for (int j = 0; j < newUVs[indexOfNewUVs].Count; j++)
                {
                    uvMaps[i]._newUvs[j + startPoint] = newUVs[indexOfNewUVs][j];
                    uvMaps[i]._uvs[j + startPoint] = newUVs[indexOfNewUVs][j];
                    uvMaps[i]._uvAnimationIndex[j + startPoint] = newAnimationIndex[indexOfNewUVs][j];
                    // TODO: Fix the copy-animation system
                    uvMaps[i]._uvAnimationLength[j + startPoint] = 1;//  newAnimationLength[indexOfNewUVs][j];
                    uvMaps[i]._uvAnimationStartPosition[j + startPoint] = newAnimationStartPoint[indexOfNewUVs][j];
                    uvMaps[i]._uvAnimationQuadPoint[j + startPoint] = newQuadPoints[indexOfNewUVs][j];
                }
            }
            else
            {
                uvMaps[i].resizeUVLength(verts.Count);
            }
        }

        // We do not copy the adjacent faces, the list of a list of a list format would be too intensive and complicated
        rebuildAdjacentFaces(faceCount);
        pushNewGeometry();


        return true;
    }

    #region Edge Control Methods
    // A full edge is comprised of 1 or more half edges

    /* Half edges run along the inside of every face on the mesh:
     * 
     *  |  -------------------→  |    
     * _|________________________|____
     *  |  --------[0]--------→  |  --
     *  | ↑   half-edge ^      | | ↑  
     *  | |                    | | |  
     *  | |                    | | |  
     *  | [3]     face f     [1] | |  
     *  | |                    | | |  
     *  | |                    | | |  
     *  | |                    ↓ | |  
     *  |  ←-------[2]--------   |  ←-
     * _|________________________|____
     *  |  ←------------------   |    
     * 
     */

    [System.Serializable]
    public class MeshEditHalfEdge
    {
        [SerializeField, HideInInspector]
        public bool isSeam;
        [SerializeField, HideInInspector]
        public bool isSharp;
    }
    [System.Serializable]
    public class MeshEditFaceEdges
    {
        [SerializeField, HideInInspector]
        public MeshEditHalfEdge[] edges;

        public MeshEditFaceEdges()
        {
            edges = new MeshEditHalfEdge[4];
            edges[0] = new MeshEditHalfEdge();
            edges[1] = new MeshEditHalfEdge();
            edges[2] = new MeshEditHalfEdge();
            edges[3] = new MeshEditHalfEdge();
        }

        internal void rotateEdges()
        {
            bool tempSharp = edges[0].isSharp;
            bool tempSeam = edges[0].isSeam;

            for (int i = 0; i < edges.Length - 1; i++)
            {
                edges[i].isSharp = edges[i + 1].isSharp;
                edges[i].isSeam = edges[i + 1].isSeam;
            }
            edges[edges.Length - 1].isSharp = tempSharp;
            edges[edges.Length - 1].isSeam = tempSeam;
        }

        internal void flipEdges()
        {
            bool tempSharp = edges[0].isSharp;
            bool tempSeam = edges[0].isSeam;

            edges[0].isSharp = edges[3].isSharp;
            edges[0].isSeam = edges[3].isSeam;
            edges[3].isSharp = tempSharp;
            edges[3].isSeam = tempSeam;
            
            tempSharp = edges[1].isSharp;
            tempSeam = edges[1].isSeam;

            edges[1].isSharp = edges[2].isSharp;
            edges[1].isSeam = edges[2].isSeam;
            edges[2].isSharp = tempSharp;
            edges[2].isSeam = tempSeam;
        }

        public MeshEditFaceEdges deepCopy()
        {
            MeshEditFaceEdges copy = new MeshEditFaceEdges();

            for(int i = 0; i < 4; i++)
            {
                copy.edges[i].isSeam = edges[i].isSeam;
                copy.edges[i].isSharp = edges[i].isSharp;
            }
            return copy;
        }
    }

    // Indexed first by face, then by edge


    [SerializeField, HideInInspector]
    public List<MeshEditFaceEdges> halfEdges;
    
    public void defineHalfEdgesAtFace(int face)
    {
        MeshEditHalfEdge[] newHalfEdges = new MeshEditHalfEdge[4];

        for (int i = 0; i < adjacentFaces[face].Count; i++)
        {
            halfEdges[face].edges[i] = new MeshEditHalfEdge();
        }

        for (int i = 0; i < adjacentFaces[face].Count; i++)
        {
            MeshEditHalfEdge halfEdge = halfEdges[face].edges[i];
            
            halfEdge.isSeam = false;
            halfEdge.isSharp = false;
            
        }
    }

    public void defineHalfEdges(List<int> faces)
    {
        // Define basic properties first...
        for (int k = 0; k < faces.Count; k++)
        {
            int f = faces[k];
            MeshEditFaceEdges newHalfEdges = new MeshEditFaceEdges();

            for (int i = 0; i < 4; i++)
            {
                newHalfEdges.edges[i] = new MeshEditHalfEdge();
            }

            for (int i = 0; i < 4; i++)
            {
                MeshEditHalfEdge halfEdge = newHalfEdges.edges[i];
                
                halfEdge.isSeam = false;
                halfEdge.isSharp = false;
            }

            halfEdges[f] = newHalfEdges;
        }
    }

    // When extruding, deleting, adding etc, run through all the faces where a lone edge has a value and extend it to the adjacent face on that side(s)
    public void bleedInwardAllEdgeValues(int skippedFaces)
    {
        for (int i = skippedFaces; i < adjacentFaces.Count; i++)
        {
            for (int j = 0; j < adjacentFaces[i].list.Count; j++)
            {
                // Find the touching edge and copy its properties
                int oppositeFace = adjacentFaces[i].list[j];

                if (oppositeFace >= 0)
                {
                    int oppositeSide = adjacentFaces[oppositeFace].list.IndexOf(i);

                    halfEdges[i].edges[j].isSeam = halfEdges[oppositeFace].edges[oppositeSide].isSeam;
                    halfEdges[i].edges[j].isSharp = halfEdges[oppositeFace].edges[oppositeSide].isSharp;
                }
            }
        }

    }

    public void defineAllHalfEdges()
    {
        halfEdges = new List<MeshEditFaceEdges>();
        List<int> facesToAdd = new List<int>();

        for (int i = 0; i < adjacentFaces.Count; i++)
        {
            halfEdges.Add(new MeshEditFaceEdges());
            facesToAdd.Add(i);
        }

        defineHalfEdges(facesToAdd);
    }

    public void setFullEdgeSharp(int face, int side, bool isSharp)
    {
        halfEdges[face].edges[side].isSharp = isSharp;

        // TODO: Make this work with lists of adjacent faces
        int oppositeFace = adjacentFaces[face].list[side];
        if (oppositeFace > -1)
        {
            int oppositeSide = adjacentFaces[oppositeFace].list.IndexOf(face);

            halfEdges[oppositeFace].edges[oppositeSide].isSharp = isSharp;
        }
    }

    public void setFullEdgeSeam(int face, int side, bool isSeam)
    {
        halfEdges[face].edges[side].isSeam = isSeam;

        // TODO: Make this work with lists of adjacent faces
        int oppositeFace = adjacentFaces[face].list[side];
        if (oppositeFace > -1)
        {
            int oppositeSide = adjacentFaces[oppositeFace].list.IndexOf(face);

            halfEdges[oppositeFace].edges[oppositeSide].isSeam = isSeam;
        }
    }

    public void setSelectedEdgesSharp(bool[] selectedVerts, bool[] selectedFaces)
    {
        bool areAllSharpsActive = true;

        for (int i = 0; i < selectedFaces.Length; i++)
        {
            for (int j = 0; j < 4; j += 1)
            {
                bool isEdgeSelected =
                    selectedVerts[quads[i * 4 + quadEdgePatternClockwise[j * 2 + 0]]] &&
                    selectedVerts[quads[i * 4 + quadEdgePatternClockwise[j * 2 + 1]]];

                if (selectedFaces[i] || isEdgeSelected)
                {
                    areAllSharpsActive = areAllSharpsActive && halfEdges[i].edges[j].isSharp;
                    setFullEdgeSharp(i, j, true);
                }
            }
        }
        if (areAllSharpsActive)
        {
            for (int i = 0; i < selectedFaces.Length; i++)
            {
                for (int j = 0; j < 4; j += 1)
                {
                    bool isEdgeSelected =
                        selectedVerts[quads[i * 4 + quadEdgePatternClockwise[j * 2 + 0]]] &&
                        selectedVerts[quads[i * 4 + quadEdgePatternClockwise[j * 2 + 1]]];

                    if (selectedFaces[i] || isEdgeSelected)
                    {
                        setFullEdgeSharp(i, j, false);
                    }
                }
            }
        }
    }

    public void setSelectedEdgesSeam(bool[] selectedVerts, bool[] selectedFaces)
    {
        bool areAllSeamsActive = true;

        for (int i = 0; i < selectedFaces.Length; i++)
        {
            for (int j = 0; j < 4; j += 1)
            {
                bool isEdgeSelected =
                    selectedVerts[quads[i * 4 + quadEdgePatternClockwise[j * 2 + 0]]] &&
                    selectedVerts[quads[i * 4 + quadEdgePatternClockwise[j * 2 + 1]]];

                if (selectedFaces[i] || isEdgeSelected)
                {
                    areAllSeamsActive = areAllSeamsActive && halfEdges[i].edges[j].isSeam;

                    setFullEdgeSeam(i, j, true);
                }
            }
        }

        if (areAllSeamsActive)
        {

            for (int i = 0; i < selectedFaces.Length; i++)
            {
                for (int j = 0; j < 4; j += 1)
                {
                    bool isEdgeSelected =
                        selectedVerts[quads[i * 4 + quadEdgePatternClockwise[j * 2 + 0]]] &&
                        selectedVerts[quads[i * 4 + quadEdgePatternClockwise[j * 2 + 1]]];

                    if (selectedFaces[i] || isEdgeSelected)
                    {
                        //Debug.Log("UnSetting Seam");
                        setFullEdgeSeam(i, j, false);
                    }
                }
            }
        }
    }
    

    #endregion

    #region Diagnostic Methods
    [NonSerialized, HideInInspector]
    public Stack<System.Diagnostics.Stopwatch> timers;
    [NonSerialized, HideInInspector]
    public Stack<string> timerDescriptions;

    public void beginTimer(string description)
    {
        if (debugMode == DebugShowInfo.OperationTime)
        {
            if (timers == null)
            {
                timers = new Stack<System.Diagnostics.Stopwatch>();
                timerDescriptions = new Stack<string>();
            }

            var timer = System.Diagnostics.Stopwatch.StartNew();
            timers.Push(timer);
            timerDescriptions.Push(description);
        }
    }
    public void endTimer()
    {
#if UNITY_EDITOR
        if (debugMode == DebugShowInfo.OperationTime)
        {
            timers.Peek().Stop();


            Debug.Log(timerDescriptions.Pop() + ": " + timers.Peek().ElapsedMilliseconds + "ms, " + timers.Pop().ElapsedTicks + "t");

        }
#endif
    }
    #endregion

#endif

}
