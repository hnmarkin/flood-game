using System;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UnityEngine.Tilemaps
{

    /// <summary>
    /// Pipeline Tiles are tiles which take into consideration its orthogonal neighboring tiles and displays a sprite depending on whether the neighboring tile is the same tile.
    /// </summary>
    [Serializable]
    public class WaterTile : TileBase
    {
        [SerializeField] Sprite big;

        public float terrainHeight = 1f;
        public float waterVolume = 0f;  // meter�miles�
        public float absorption = 0f;

        public bool isOverflowing = false;
        public bool spread = true;
        public bool isFlooded = false;

        public bool ishalf = true;
        // Derived property: water height = terrain + (waterVolume / tileArea)
        public float WaterHeight => terrainHeight + (waterVolume);

        // Convenience property for checking if water is present
        //public bool HasWater => waterVolume > 0f;

        // Optionally, store references to neighbors for quick lookup
        public Tile[] neighbors = new Tile[3];
        /// <summary>
        /// The Sprites used for defining the Pipeline.
        /// </summary>
        [SerializeField]
        public Sprite[] m_Sprites;

        public void AddWater(int amount)
        {

            absorption += amount;
            if (absorption > 0f)
            {
                waterVolume += absorption;
                absorption = 0f;
                isFlooded = true;
            }
        }
        public void AddWater(int amount, float Height)
        {
            if (Height < WaterHeight)
            {
                return;
            }
            absorption += amount;
            if (absorption > 0f)
            {
                waterVolume += absorption;
                absorption = 0f;
                isFlooded = true;

            }
        }
        /*
        /// <summary>
        /// This method is called when the tile is refreshed. The PipelineExampleTile will refresh all neighboring tiles to update their rendering data if they are the same tile.
        /// </summary>
        /// <param name="position">Position of the tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            for (int yd = -1; yd <= 1; yd++)
                for (int xd = -1; xd <= 1; xd++)
                {
                    Vector3Int pos = new Vector3Int(position.x + xd, position.y + yd, position.z);
                    if (TileValue(tilemap, pos))
                        tilemap.RefreshTile(pos);
                }
        }


        /// <summary>
        /// Retrieves any tile rendering data from the scripted tile.
        /// </summary>
        /// <param name="position">Position of the tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        /// <param name="tileData">Data to render the tile.</param>
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            UpdateTile(position, tilemap, ref tileData);
        }


        /// <summary>
        /// Checks the orthogonal neighbouring positions of the tile and generates a mask based on whether the neighboring tiles are the same. The mask will determine the according Sprite and transform to be rendered at the given position. The Sprite and Transform is then filled into TileData for the Tilemap to use. The Flags lock the color and transform to the data provided by the tile. The ColliderType is set to the shape of the Sprite used.
        /// </summary>
        private void UpdateTile(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.transform = Matrix4x4.identity;
            tileData.color = Color.white;


            int mask = TileValue(tilemap, position + new Vector3Int(0, 1, 0)) ? 1 : 0;
            mask += TileValue(tilemap, position + new Vector3Int(1, 0, 0)) ? 2 : 0;
            mask += TileValue(tilemap, position + new Vector3Int(0, -1, 0)) ? 4 : 0;
            mask += TileValue(tilemap, position + new Vector3Int(-1, 0, 0)) ? 8 : 0;


            int index = GetIndex((byte)mask);
            if (index >= 0 && index < m_Sprites.Length && TileValue(tilemap, position))
            {
                tileData.sprite = m_Sprites[index];
                tileData.transform = GetTransform((byte)mask);
                tileData.flags = TileFlags.LockTransform | TileFlags.LockColor;
                tileData.colliderType = Tile.ColliderType.Sprite;
            }
        }


        /// <summary>
        /// Determines if the tile at the given position is the same tile as this.
        /// </summary>
        private bool TileValue(ITilemap tileMap, Vector3Int position)
        {
            TileBase tile = tileMap.GetTile(position);
            return (tile != null && tile == this);
        }


        /// <summary>
        /// Determines the index of the Sprite to be used based on the neighbour mask.
        /// </summary>
        private int GetIndex(byte mask)
        {
            switch (mask)
            {
                case 0: return 0;
                case 3:
                case 6:
                case 9:
                case 12: return 1;
                case 1:
                case 2:
                case 4:
                case 5:
                case 10:
                case 8: return 2;
                case 7:
                case 11:
                case 13:
                case 14: return 3;
                case 15: return 4;
            }
            return -1;
        }


        /// <summary>
        /// Determines the Transform to be used based on the neighbour mask.
        /// </summary>
        private Matrix4x4 GetTransform(byte mask)
        {
            switch (mask)
            {
                case 9:
                case 10:
                case 7:
                case 2:
                case 8:
                    return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -90f), Vector3.one);
                case 3:
                case 14:
                    return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -180f), Vector3.one);
                case 6:
                case 13:
                    return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -270f), Vector3.one);
            }
            return Matrix4x4.identity;
        }
    }
    */
#if UNITY_EDITOR
    /// <summary>
    /// Custom Editor for a PipelineExampleTile. This is shown in the Inspector window when a PipelineExampleTile asset is selected.
    /// </summary>
    [CustomEditor(typeof(WaterTile))]
    public class WaterTileEditor : Editor
    {
        private WaterTile tile { get { return (target as WaterTile); } }


        public void OnEnable()
        {
            if (tile.m_Sprites == null || tile.m_Sprites.Length != 5)
                tile.m_Sprites = new Sprite[5];
        }


        /// <summary>
        /// Draws an Inspector for the PipelineExampleTile.
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Place sprites shown based on the number of tiles bordering it.");
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
                //tile.isFlooded = EditorGUILayout.Toggle(true, null);
            tile.m_Sprites[0] = (Sprite)EditorGUILayout.ObjectField("None", tile.m_Sprites[0], typeof(Sprite), false, null);
            tile.m_Sprites[2] = (Sprite)EditorGUILayout.ObjectField("One", tile.m_Sprites[2], typeof(Sprite), false, null);
            tile.m_Sprites[1] = (Sprite)EditorGUILayout.ObjectField("Two", tile.m_Sprites[1], typeof(Sprite), false, null);
            tile.m_Sprites[3] = (Sprite)EditorGUILayout.ObjectField("Three", tile.m_Sprites[3], typeof(Sprite), false, null);
            tile.m_Sprites[4] = (Sprite)EditorGUILayout.ObjectField("Four", tile.m_Sprites[4], typeof(Sprite), false, null);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(tile);
        }


        /// <summary>
        /// The following is a helper that adds a menu item to create a PipelineExampleTile Asset in the project.
        /// </summary>
        [MenuItem("Assets/Create/PipelineExampleTile")]
        public static void CreatePipelineExampleTile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Pipeline Example Tile", "New Pipeline Example Tile", "Asset", "Save Pipeline Example Tile", "Assets");
            if (path == "")
                return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WaterTile>(), path);
        }
    }
#endif
    
    }
}

