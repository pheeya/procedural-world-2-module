using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator
{
    public static MeshData GenerateMeshFromHeightMap(float[,] _heightmap, float _heightScale, AnimationCurve _heightCurve)
    {
        int width = _heightmap.GetLength(0);
        int height = _heightmap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;


        MeshData meshData = new MeshData(width, height);
        int vertexIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, _heightCurve.Evaluate(_heightmap[x, y]) * _heightScale, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);
                if (x < width - 1 && y < height - 1)
                {
                    int[] triangle = new int[3];
                    triangle[0] = vertexIndex;
                    triangle[1] = vertexIndex + width + 1;
                    triangle[2] = vertexIndex + width;

                    int[] triangle2 = new int[3];
                    triangle2[0] = vertexIndex + width + 1;
                    triangle2[1] = vertexIndex;
                    triangle2[2] = vertexIndex + 1;


                    meshData.AddTriangle(triangle);
                    meshData.AddTriangle(triangle2);

                }
                vertexIndex++;
            }
        }




        return meshData;
    }
}



public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    private int triangleIndex;

    public MeshData(int _meshWidth, int _meshHeight)
    {
        vertices = new Vector3[_meshHeight * _meshWidth];
        triangles = new int[(_meshWidth - 1) * (_meshHeight - 1) * 6];
        uvs = new Vector2[_meshWidth * _meshHeight];
    }

    public void AddTriangle(int[] vertices)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            triangles[triangleIndex] = vertices[i];
            triangleIndex++;
        }
    }



    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }

}