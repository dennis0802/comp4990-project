using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;

namespace CombatPhase.ProceduralGeneration {
    public class GenerateTerrain : MonoBehaviour {
        const float viewerMoveThresholdForChunkUpdate = 25f;
        const float sqrviewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

        // Map generator info
        public LODInfo[] detailLevels;
        public static float maxViewDst;
        public Transform viewer;
        public Material mapMaterial;

        public static Vector2 viewerPosition;
        Vector2 viewerPositionOld;
        static MapGenerator mapGenerator;
        int chunkSize;
        int chunksVisibleInViewDst;

        // To track chunks
        Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
        static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

        void Start() {
            mapGenerator = FindObjectOfType<MapGenerator>();

            maxViewDst = detailLevels[detailLevels.Length-1].visibleDstThreshold;
            chunkSize = mapGenerator.mapChunkSize - 1;
            chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst/chunkSize);

            UpdateVisibleChunks();
        }

        void Update(){
            // Check player position and if to update
            viewerPosition = new Vector2(viewer.position.x, viewer.position.z)/mapGenerator.terrainData.uniformScale;
            if((viewerPositionOld-viewerPosition).sqrMagnitude > sqrviewerMoveThresholdForChunkUpdate){
                viewerPositionOld = viewerPosition;
                UpdateVisibleChunks();
            }
        }

        /// <summary>
        /// Updates all visible terrain chunks on the map
        /// </summary>
        void UpdateVisibleChunks(){
            // Clear the chunks that were visible last update to avoid issues with chunks not unrendering when too far away
            for(int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++){
                terrainChunksVisibleLastUpdate[i].SetVisible(false);
            }
            terrainChunksVisibleLastUpdate.Clear();

            int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x/chunkSize);
            int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y/chunkSize);
            
            // Loop through all chunks to update existing chunks or add new chunks
            for(int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++){
                for(int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++){
                    Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                
                    if(terrainChunkDictionary.ContainsKey(viewedChunkCoord)){
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else{
                        terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                        if(!terrainChunkDictionary[viewedChunkCoord].hasBaked){
                            StartCoroutine(terrainChunkDictionary[viewedChunkCoord].BakeCoroutine());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Helper class to store terrain chunk data
        /// </summary>
        public class TerrainChunk{
            GameObject meshObject;
            Vector2 position;
            Bounds bounds;

            MeshRenderer meshRenderer;
            MeshFilter meshFilter;
            MeshCollider meshCollider;
            NavMeshSurface navMeshSurface;

            LODInfo[] detailLevels;
            LODMesh[] lodMeshes;
            LODMesh collisionLODMesh;

            MapData mapData;
            bool mapDataReceived;
            public bool hasBaked;
            int previousLODIndex = -1;

            /// <summary>
            /// Terrain chunk constructor
            /// </summary>
            /// <param name="coord">The coordinate of the chunk</param>
            /// <param name="size">The size of the chunk</param>
            /// <param name="detailLevels">The detail levels of the chunk</param>
            /// <param name="parent">The parent object to be set as the parent of the chunk</param>
            /// <param name="material">The material to apply on the chunk</param>
            public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material){
                this.detailLevels = detailLevels;
                
                position = coord * size;
                bounds = new Bounds(position, Vector2.one * size);
                Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            
                meshObject = new GameObject("Terrain Chunk");
                meshRenderer = meshObject.AddComponent<MeshRenderer>();
                meshFilter = meshObject.AddComponent<MeshFilter>();
                meshCollider = meshObject.AddComponent<MeshCollider>();
                navMeshSurface = meshObject.AddComponent<NavMeshSurface>();
                meshRenderer.material = material;

                meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
                meshObject.transform.parent = parent;
                meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;

                SetVisible(false);

                // Check if collider should be used
                lodMeshes = new LODMesh[detailLevels.Length];
                for(int i = 0; i < detailLevels.Length; i++){
                    lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                    if(detailLevels[i].useForCollider){
                        collisionLODMesh = lodMeshes[i];
                    }
                }

                mapGenerator.RequestMapData(position, OnMapDataReceived);
            }

            /// <summary>
            /// Update the chunk on receiving map data
            /// </summary>
            /// <param name="mapData">Map data for the chunk, such as colors, noise, and details</param>
            void OnMapDataReceived(MapData mapData){
                this.mapData = mapData;
                mapDataReceived = true;

                Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, mapGenerator.mapChunkSize, mapGenerator.mapChunkSize);
                meshRenderer.material.mainTexture = texture;

                UpdateTerrainChunk();
            }

            /// <summary>
            /// Update the chunk
            /// </summary>
            public void UpdateTerrainChunk(){
                if(mapDataReceived){
                    // Check if viewer within nearest edge
                    float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                    bool visible = viewerDstFromNearestEdge <= maxViewDst;

                    // If already visible, check if additional detailing should be done
                    if(visible){
                        int lodIndex = 0;

                        for(int i = 0; i < detailLevels.Length-1; i++){
                            if(viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold){
                                lodIndex = i + 1;
                            }
                            else{
                                break;
                            }
                        }

                        if(lodIndex != previousLODIndex){
                            LODMesh lodMesh = lodMeshes[lodIndex];
                            if(lodMesh.hasMesh){
                                previousLODIndex = lodIndex;
                                meshFilter.mesh = lodMesh.mesh;
                            }
                            else if(!lodMesh.hasRequestedMesh){
                                lodMesh.RequestMesh(mapData);
                            }
                        }
                        if(lodIndex == 0){
                            if(collisionLODMesh.hasMesh){
                                meshCollider.sharedMesh = collisionLODMesh.mesh;
                            }
                            else if(!collisionLODMesh.hasRequestedMesh){
                                collisionLODMesh.RequestMesh(mapData);
                            }
                        }       
                        terrainChunksVisibleLastUpdate.Add(this);
                    }

                    SetVisible(visible);
                }
            }

            /// <summary>
            /// Set the visibility of the chunk
            /// </summary>
            /// <param name="visible">True if visible, false otherwise</param>
            public void SetVisible(bool visible){
                meshObject.SetActive(visible);
            }

            /// <summary>
            /// Check if the chunk is visible
            /// </summary>
            public bool IsVisible(){
                return meshObject.activeSelf;
            }

            /// <summary>
            /// Coroutine to bake the navmesh
            /// </summary>
            /// <returns>Coroutine to bake navmesh</returns>
            public IEnumerator BakeCoroutine(){
                // Delay required to ensure all terrain is visible before baking
                yield return new WaitForSeconds(1);
                // There is some required cost on the CPU for this to work but delay is about 10-15s for 1-time baking
                navMeshSurface.BuildNavMesh();
                hasBaked = true;
            }
        }

        /// <summary>
        /// Helper class for a level of detail mesh
        /// </summary>
        class LODMesh{
            public Mesh mesh;
            public bool hasRequestedMesh;
            public bool hasMesh;
            int lod;
            System.Action updateCallback;

            /// <summary>
            /// LODMesh constructor
            /// </summary>
            /// <param name="lod">The level of detail of the mesh</param>
            /// <param name="updateCallback">The callback function to use during threading</param>
            public LODMesh(int lod, System.Action updateCallback){
                this.lod = lod;
                this.updateCallback = updateCallback;
            }

            /// <summary>
            /// Update the mesh on receiving mesh data
            /// </summary>
            /// <param name="meshData">Map data for the chunk, such as triangles, height, lighting, and curves</param>
            void OnMeshDataReceived(MeshData meshData){
                mesh = meshData.CreateMesh();
                hasMesh = true;

                updateCallback();
            }

            /// <summary>
            /// Request mesh data for the mesh
            /// </summary>
            /// <param name="mapData">The map data for the current mesh</param>
            public void RequestMesh(MapData mapData){
                hasRequestedMesh = true;
                mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
            }
            
        }

        /// <summary>
        /// Struct for level of detail info
        /// </summary>
        [System.Serializable]
        public struct LODInfo{
            public int lod;
            public float visibleDstThreshold;
            public bool useForCollider;
        }
    }
}