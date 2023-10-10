using UnityEngine;
using System.Collections;

public static class MeshGenerator {

    /// <summary>
    /// Generate the terrain mesh
    /// </summary>
    /// <param name="heightMap">A map of heights for the mesh</param>
    /// <param name="heightMultiplier">A factor to multiply into the heights for the mesh</param>
    /// <param name="_heightCurve">Animation curve needed for smooth curves on heights</param>
    /// <param name="levelOfDetail">The level of detail to apply on the mesh</param>
    /// <param name="useFlatShading">True if using flat shading, false otherwise</param>
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail, bool useFlatShading){
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified-1)/-2f;
        float topLeftZ = (meshSizeUnsimplified-1)/2f;

        int verticesPerLine = (meshSize-1)/meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, useFlatShading);

        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        // Check if vertices on the mesh (made of triangles) are border vertices (to another mesh)
        for(int y = 0; y < borderedSize; y+= meshSimplificationIncrement){
            for(int x = 0; x < borderedSize; x+= meshSimplificationIncrement){
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if(isBorderVertex){
                    vertexIndicesMap[x,y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else{
                    vertexIndicesMap[x,y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        // Regular triangles for the mesh
        for(int y = 0; y < borderedSize; y+= meshSimplificationIncrement){
            for(int x = 0; x < borderedSize; x+= meshSimplificationIncrement){
                int vertexIndex = vertexIndicesMap[x, y];
                
                Vector2 percent = new Vector2((x-meshSimplificationIncrement)/(float)meshSize, (y-meshSimplificationIncrement)/(float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x,y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);
               
                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                if(x < borderedSize - 1 && y < borderedSize - 1){
                    int a = vertexIndicesMap[x,y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement,y];
                    int c = vertexIndicesMap[x,y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement,y + meshSimplificationIncrement];

                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIndex++;
            }
        }
        meshData.ProcessMesh();

        return meshData;
    }
}

/// <summary>
/// Helper class to store mesh data
/// </summary>
public class MeshData{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;
    
    Vector3[] borderVertices;
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    bool useFlatShading;

    /// <summary>
    /// Mesh data constructor
    /// </summary>
    /// <param name="verticesPerLine">Number of vertices to use per line on the mesh triangles</param>
    /// <param name="useFlatShading">True if using flat shading, false otherwise</param>
    public MeshData(int verticesPerLine, bool useFlatShading){
        this.useFlatShading = useFlatShading;
        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine-1) * (verticesPerLine-1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    /// <summary>
    /// Add a vertex
    /// </summary>
    /// <param name="vertexPosition">Position to place the vertex</param>
    /// <param name="uv">Texture coordinate for the mesh</param>
    /// <param name="vertexIndex">current vertex index</param>
    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex){
        if(vertexIndex < 0){
            borderVertices[-vertexIndex-1] = vertexPosition;
        }
        else{
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    /// <summary>
    /// Add a triangle
    /// </summary>
    /// <param name="a">Vertex 1</param>
    /// <param name="b">Vertex 2</param>
    /// <param name="c">Vertex 3</param>
    public void AddTriangle(int a, int b, int c){
        if(a < 0 || b < 0 || c < 0){
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex+1] = b;
            borderTriangles[borderTriangleIndex+2] = c;
            borderTriangleIndex += 3;
        }
        else{
            triangles[triangleIndex] = a;
            triangles[triangleIndex+1] = b;
            triangles[triangleIndex+2] = c;
            triangleIndex += 3;
        }
    }

    /// <summary>
    /// Calculate the normals for the mesh
    /// </summary>
    /// <returns>The vertex normals for the mesh</returns>
    Vector3[] CalculateNormals(){
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        for(int i = 0; i < triangleCount; i++){
            int normalTriangleIndex = i*3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length/3;
        for(int i = 0; i < borderTriangleCount; i++){
            int normalTriangleIndex = i*3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            
            if(vertexIndexA >= 0){
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if(vertexIndexB >= 0){
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if(vertexIndexC >= 0){
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        for(int i = 0; i < vertexNormals.Length; i++){
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    /// <summary>
    /// Get the surface normal from the vertices
    /// </summary>
    /// <param name="indexA">Vertex 1</param>
    /// <param name="indexB">Vertex 2</param>
    /// <param name="indexC">Vertex 3</param>
    /// <returns>The surface normal from the indices</returns>
    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC){
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA-1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB-1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC-1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    /// <summary>
    /// Process the mesh as flat shading or baked normals
    /// </summary>
    public void ProcessMesh(){
        if(useFlatShading){
            FlatShading();
        }
        else{
            BakeNormals();
        }
    }

    /// <summary>
    /// Bake the normals for the mesh
    /// </summary>
    private void BakeNormals(){
        bakedNormals = CalculateNormals();
    }

    /// <summary>
    /// Process flat shading for the mesh
    /// </summary>
    void FlatShading(){
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        for(int i = 0; i < triangles.Length; i++){
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUvs;
    }

    /// <summary>
    /// Get the surface normal from the vertices
    /// </summary>
    /// <returns>The mesh created</returns>
    public Mesh CreateMesh(){
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        if(useFlatShading){
            mesh.RecalculateNormals();
        }
        else{
            mesh.normals = bakedNormals;
        }

        return mesh;
    }
}