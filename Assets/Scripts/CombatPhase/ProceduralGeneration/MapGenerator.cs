using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.SceneManagement;
using CombatPhase.ProceduralGeneration.Data;

namespace CombatPhase.ProceduralGeneration {
    public class MapGenerator : MonoBehaviour {
        public enum DrawMode{NoiseMap, ColourMap, Mesh};
        public DrawMode drawMode;

        public CombatPhase.ProceduralGeneration.Data.TerrainData terrainData;
        public NoiseData noiseData;

        [Range(0,6)]
        public int editorPreviewLOD;

        public bool autoUpdate;
        public TerrainType[] regions;

        Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
        Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

        #if UNITY_EDITOR
            void OnValuesUpdated(){
                if(!Application.isPlaying){
                    DrawMapInEditor();
                }
            }
        #endif

        public int mapChunkSize{
            get{
                if(terrainData.useFlatShading){
                    return 95;
                }
                else{
                    return 239;
                }
            }
        }


        /// <summary>
        /// Draw the map into the editor, as noise, color, or mesh.
        /// </summary>
        public void DrawMapInEditor(){
            #if UNITY_EDITOR
            if(SceneManager.GetActiveScene().buildIndex == 3 || Equals(SceneManager.GetActiveScene().name, "Test")){
                MapData mapData = GenerateMapData(Vector2.zero);

                MapDisplay display = FindObjectOfType<MapDisplay>();
                if(drawMode == DrawMode.NoiseMap){
                    display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
                }
                else if(drawMode == DrawMode.ColourMap){
                    display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
                }
                else if(drawMode == DrawMode.Mesh){
                    display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLOD, terrainData.useFlatShading), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
                }
            }
            #endif
        }


        /// <summary>
        /// Request map data
        /// </summary>
        /// <param name="centre">The centre of the noise map</param>
        /// <param name="callback">The callback function in the thread</param>
        public void RequestMapData(Vector2 centre, Action<MapData> callback){
            ThreadStart threadStart = delegate {
                MapDataThread(centre, callback);
            };
            new Thread(threadStart).Start();
        }
        
        /// <summary>
        /// Use a thread to generate the map data.
        /// </summary>
        /// <param name="centre">The centre of the noise map</param>
        /// <param name="callback">The callback function in the thread</param>
        void MapDataThread(Vector2 centre, Action<MapData> callback){
            MapData mapData = GenerateMapData(centre);
            lock (mapDataThreadInfoQueue){
                mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
            }
        }

        /// <summary>
        /// Request mesh data
        /// </summary>
        /// <param name="mapData">The map data for the mesh</param>
        /// <param name="lod">The level of detail for the mesh</param>
        /// <param name="callback">The callback function in the thread</param>
        public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback){
            ThreadStart threadStart = delegate {
                MeshDataThread(mapData, lod, callback);
            };
            new Thread(threadStart).Start();        
        }

        /// <summary>
        /// Use a thread to generate the map data
        /// </summary>
        /// <param name="mapData">The map data for the mesh</param>
        /// <param name="lod">The level of detail for the mesh</param>
        /// <param name="callback">The callback function in the thread</param>
        void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback){
            MeshData meshdata = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
            lock(meshDataThreadInfoQueue){
                meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshdata));
            }
        }

        void Update(){
            if(mapDataThreadInfoQueue.Count > 0){
                for(int i = 0; i < mapDataThreadInfoQueue.Count; i++){
                    MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }

            if(meshDataThreadInfoQueue.Count > 0){
                for(int i = 0; i < meshDataThreadInfoQueue.Count; i++){
                    MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }

        /// <summary>
        /// Generate map data
        /// </summary>
        /// <param name="centre">The centre of the noise map</param>
        /// <returns>The map data as specified from the centre of the noise map</returns>
        private MapData GenerateMapData(Vector2 centre){
            float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistence, noiseData.lacunarity, centre+noiseData.offset, noiseData.normalizeMode);
        
            Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
            for(int y = 0; y < mapChunkSize; y++){
                for(int x = 0; x < mapChunkSize; x++){
                    float currentHeight = noiseMap[x,y];

                    for(int i = 0; i < regions.Length; i++){
                        if(currentHeight >= regions[i].height){
                            colourMap[y * mapChunkSize + x] = regions[i].colour;
                        }
                        else{
                            break;
                        }
                    }
                }
            }

            return new MapData(noiseMap, colourMap);
        }

        #if UNITY_EDITOR
            void OnValidate(){
                if(terrainData != null){
                    terrainData.OnValuesUpdated -= OnValuesUpdated;
                    terrainData.OnValuesUpdated += OnValuesUpdated;
                }
                if(noiseData != null){
                    noiseData.OnValuesUpdated -= OnValuesUpdated;
                    noiseData.OnValuesUpdated += OnValuesUpdated;
                }
            }
        #endif


        /// <summary>
        /// Struct to store map thread of info of a generic type
        /// </summary>
        struct MapThreadInfo<T>{
            public readonly Action<T> callback;
            public readonly T parameter;

            /// <summary>
            /// MapThreadInfo constructor
            /// </summary>
            /// <param name="callback">The callback function for the thread</param>
            /// <param name="parameter">The parameter to pass into the thread</param>
            public MapThreadInfo(Action<T> callback, T parameter){
                this.callback = callback;
                this.parameter = parameter;
            }
        }	
    }

    /// <summary>
    /// Struct to store the terrain types
    /// </summary>
    [System.Serializable]
    public struct TerrainType{
        public float height;
        public string name;
        public Color colour;
    }

    /// <summary>
    /// Struct to store map data
    /// </summary>
    public struct MapData {
        public readonly float[,] heightMap;
        public readonly  Color[] colourMap;

        /// <summary>
        /// MapData constructor
        /// </summary>
        /// <param name="heightMap">The height map for the terrain</param>
        /// <param name="colourMap">The colour map for the terrain</param>
        public MapData(float [,] heightMap, Color[] colourMap){
            this.heightMap = heightMap;
            this.colourMap = colourMap;
        }
    }
}