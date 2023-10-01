using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.AI.Navigation;

namespace CombatPhase{
    [RequireComponent(typeof(MeshFilter))]
    public class MapGenerator : MonoBehaviour
    {
        // Based off of https://www.youtube.com/watch?v=bd4P5suj-L0 and https://github.com/Pang/ProceduralTerrainScripts
        Mesh mesh;
        private int _mesh_scale = 12;
        public GameObject[] objects;
        [SerializeField]
        private AnimationCurve heightCurve;
        private Vector3[]  _verticies;
        private int[] _triangles;
        private Color[] _colors;
        [SerializeField]
        private Gradient _gradient;
        private float _minTerrainHeight, _maxTerrainHeight;
        public int xSize, zSize;
        public float scale;
        public int octaves;
        public float lacurnarity;
        public int seed;
        private float _lastNoiseHeight;
        public NavMeshSurface surface;

        void Start()
        {
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
            CreateNewMap();
        }

        public void CreateNewMap(){
            CreateMeshShape();
            CreateTriangles();
            ColorMap();
            UpdateMesh();
        }

        private void CreateMeshShape(){
            Vector2[] octaveOffsets = GetOffsetSeed();

            scale = scale <= 0 ? 0.0001f : scale;

            // create verticies
            _verticies = new Vector3[(xSize+1) * (zSize+1)];

            for(int i = 0, z = 0; z <= zSize; z++){
                for(int x = 0; x <= xSize; x++){
                    // Set vertex height
                    float noiseHeight = GenerateNoiseHeight(z,x,octaveOffsets);
                    SetMinMaxHeights(noiseHeight);
                    _verticies[i] = new Vector3(x, noiseHeight, z);
                    i++;
                }
            }
        }

        private Vector2[] GetOffsetSeed(){
            seed = Random.Range(0, 1000);

            // Change map area
            System.Random prng = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[octaves];

            for(int i = 0; i < octaves; i++){
                float offsetX = prng.Next(-100000, 100000);
                float offsetY = prng.Next(-100000, 100000);
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            return octaveOffsets;
        }

        private float GenerateNoiseHeight(int z, int x, Vector2[] octaveOffsets){
            float amplitude = 20f, frequency = 1f, persistence = 0.5f, noiseHeight = 0f;

            // Loop octaves
            for(int i = 0; i < octaves; i++){
                float mapZ = (float)(z/scale) * frequency + octaveOffsets[i].y;
                float mapX = (float)(x/scale) * frequency + octaveOffsets[i].x;

                // *2-1 creates a flat floor level
                float perlinValue = (Mathf.PerlinNoise(mapZ, mapX)) * 2 - 1;
                noiseHeight += heightCurve.Evaluate(perlinValue) * amplitude;
                frequency *= lacurnarity;
                amplitude *= persistence;
            }

            return noiseHeight;
        }

        private void SetMinMaxHeights(float noiseHeight){
            _maxTerrainHeight = noiseHeight > _maxTerrainHeight ? noiseHeight : _maxTerrainHeight;
            _minTerrainHeight = noiseHeight > _minTerrainHeight ? noiseHeight : _minTerrainHeight;
        }

        private void CreateTriangles(){
            _triangles = new int[xSize * zSize * 6];

            int vert = 0, tris = 0;

            for(int z = 0; z < xSize; z++){
                for(int x = 0; x < xSize; x++){
                    _triangles[tris+0] = vert + 0;
                    _triangles[tris+1] = vert + xSize + 1;
                    _triangles[tris+2] = vert + 1;
                    _triangles[tris+3] = vert + 1;
                    _triangles[tris+4] = vert + xSize + 1;
                    _triangles[tris+5] = vert + xSize + 2;

                    vert++;
                    tris += 6;
                }
                vert++;
            }
        }

        private void ColorMap(){
            _colors = new Color[_verticies.Length];

            for(int i = 0, z = 0; z < _verticies.Length; z++){
                float height = Mathf.InverseLerp(_minTerrainHeight, _maxTerrainHeight, _verticies[i].y);
                _colors[i] = _gradient.Evaluate(height);
                i++;
            }
        }

        private void MapEmbellishments(){
            for(int i = 0; i < _verticies.Length; i++){
                Vector3 worldPt = transform.TransformPoint(mesh.vertices[i]);
                var noiseHeight = worldPt.y;

                // Stop generation if too steep
                if(System.Math.Abs(_lastNoiseHeight - worldPt.y) < 25){
                    if(noiseHeight > 100){
                        if(Random.Range(1,5) == 1){
                            GameObject objectToSpawn = objects[Random.Range(0, objects.Length)];
                            var spawnAboveTerrainBy = noiseHeight * 2;
                            Instantiate(objectToSpawn, new Vector3(mesh.vertices[i].x * _mesh_scale, spawnAboveTerrainBy, mesh.vertices[i].z * _mesh_scale), Quaternion.identity);
                        }
                    }
                }
                _lastNoiseHeight = noiseHeight;
            }
        }

        private void UpdateMesh(){
            mesh.Clear();
            mesh.vertices = _verticies;
            mesh.triangles = _triangles;
            mesh.colors = _colors;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            GetComponent<MeshCollider>().sharedMesh = mesh;
            gameObject.transform.localScale = new Vector3(_mesh_scale, _mesh_scale, _mesh_scale);

            MapEmbellishments();
            //surface.BuildNavMesh();
        }
    }
}

