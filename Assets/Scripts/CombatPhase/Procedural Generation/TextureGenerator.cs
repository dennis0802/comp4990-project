using UnityEngine;
using System.Collections;

public static class TextureGenerator{
    /// <summary>
    /// Generate textures from the colour map
    /// </summary>
    /// <param name="colourMap">A map of colours for the map</param>
    /// <param name="width">Texture width</param>
    /// <param name="height">Texture height</param>
    /// <returns> A texture based on the colour map</returns>
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height){
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Generate textures from the height map
    /// </summary>
    /// <param name="heightMap">A map of heights for the map</param>
    /// <returns> A texture based on the height map</returns>
    public static Texture2D TextureFromHeightMap(float[,] heightMap){
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Texture2D texture = new Texture2D(width, height);
        Color[] colourMap = new Color[width * height];
        for(int y = 0; y < height; y++){
            for(int x = 0 ; x < width; x++){
                colourMap[y*width+x] = Color.Lerp(Color.black, Color.white, heightMap[x,y]);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }
}