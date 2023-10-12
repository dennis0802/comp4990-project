using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CombatPhase.ProceduralGeneration {
    public class MapDisplay : MonoBehaviour
    {
        public Renderer textureRender;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

        /// <summary>
        /// Draw texture onto the map
        /// </summary>
        /// <param name="texture">The texture to be applied</param>
        public void DrawTexture(Texture2D texture){

            textureRender.sharedMaterial.mainTexture = texture;
            textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
        }

        /// <summary>
        /// Draw mesh onto the map
        /// </summary>
        /// <param name="meshData">The mesh data to be drawn</param>
        /// <param name="texture">The texture to be applied</param>
        public void DrawMesh(MeshData meshData, Texture2D texture){
            meshFilter.sharedMesh = meshData.CreateMesh();
            meshFilter.transform.localScale = Vector3.one * FindObjectOfType<MapGenerator>().terrainData.uniformScale;
            meshRenderer.sharedMaterial.mainTexture = texture;
        }
    }
}