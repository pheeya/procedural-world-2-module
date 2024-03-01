using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator
{
    public static MeshData GenerateMeshFromHeightMap(HeightMap _heightmap, float _heightScale, AnimationCurve _heightCurve)
    {
        int width = _heightmap.Width;
        int height = _heightmap.Height;
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        MeshData meshData = new MeshData(width - _heightmap.BorderSize, height - _heightmap.BorderSize);
        int vertexIndex = 0;
        int borderVertexIndex = -1;

        List<int> borderTriangles = new();
        List<Vector3> borderVertices = new();
        int[,] vertexIndicesMap = new int[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isBorder = y < _heightmap.BorderSize || y > (height - _heightmap.BorderSize - 1) || x < _heightmap.BorderSize || x > (width - _heightmap.BorderSize - 1); ;
                if (isBorder)
                {

                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {

                    vertexIndicesMap[x, y] = vertexIndex;


                    vertexIndex++;
                }
            }

        }
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int currentIndex = vertexIndicesMap[x, y];

                bool isBorder = currentIndex < 0;

                Vector3 pos = new Vector3(topLeftX + x, _heightCurve.Evaluate(_heightmap.Values[x, y]) * _heightScale, topLeftZ - y); ;
                if (isBorder)
                {
                    borderVertices.Add(pos);
                }
                else
                {
                    meshData.vertices[currentIndex] = pos;
                    meshData.uvs[currentIndex] = new Vector2(x / (float)width, y / (float)height);
                }
                if (x < width - 1 && y < height - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + 1, y];
                    int c = vertexIndicesMap[x, y + 1];
                    int d = vertexIndicesMap[x + 1, y + 1];

                    if (a < 0 || d < 0 || c < 0)
                    {
                        borderTriangles.Add(a);
                        borderTriangles.Add(d);
                        borderTriangles.Add(c);
                    }
                    else
                    {
                        meshData.AddTriangle(a, d, c);
                    }

                    if (a < 0 || d < 0 || b < 0)
                    {
                        borderTriangles.Add(d);
                        borderTriangles.Add(a);
                        borderTriangles.Add(b);
                    }
                    else
                    {
                        meshData.AddTriangle(d, a, b);
                    }


                }
                // vertexIndex++;
            }
        }



        meshData.normals = CalculateNormals(meshData.vertices, meshData.triangles, borderVertices, borderTriangles);



        meshData.UpdateMesh();

        return meshData;
    }
    public static Vector3[] CalculateNormals(List<Vector3> _vertices, List<int> _triangles, List<Vector3> _borderVerts, List<int> _borderTriangles)
    {
        Vector3[] normals = new Vector3[_vertices.Count];
        int triCount = _triangles.Count / 3;
        for (int i = 0; i < triCount; i++)
        {
            int indexInNormalArray = i * 3;

            int vindexA = _triangles[indexInNormalArray];
            int vindexB = _triangles[indexInNormalArray + 1];
            int vindexC = _triangles[indexInNormalArray + 2];

            Vector3 normal = NormalFromPoints(_vertices[vindexA], _vertices[vindexB], _vertices[vindexC]);

            normals[vindexA] += normal;
            normals[vindexB] += normal;
            normals[vindexC] += normal;
        }

        for (int i = 0; i < _borderTriangles.Count / 3; i++)
        {
            int index = i * 3;
            int vindexA = _borderTriangles[index];
            int vindexB = _borderTriangles[index + 1];
            int vindexC = _borderTriangles[index + 2];


            Vector3 vertA = vindexA < 0 ? _borderVerts[-vindexA - 1] : _vertices[vindexA];
            Vector3 vertB = vindexB < 0 ? _borderVerts[-vindexB - 1] : _vertices[vindexB];
            Vector3 vertC = vindexC < 0 ? _borderVerts[-vindexC - 1] : _vertices[vindexC];


            Vector3 normal = NormalFromPoints(vertA, vertB, vertC);
            if (vindexA >= 0)
            {
                normals[vindexA] += normal;
            }
            if (vindexB >= 0)
            {
                normals[vindexB] += normal;

            }
            if (vindexC >= 0)
            {
                normals[vindexC] += normal;

            }

        }

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i].Normalize();
        }

        return normals;
    }

    public static Vector3 NormalFromPoints(Vector3 _a, Vector3 _b, Vector3 _c)
    {

        Vector3 normal = Vector3.Cross(_b - _a, _c - _a).normalized;



        return normal;
    }
}



public class MeshData
{
    public List<Vector3> vertices;
    public List<int> triangles;
    public Vector2[] uvs;

    public Vector3[] normals;

    public Mesh mesh;

    public MeshData(int _meshWidth, int _meshHeight)
    {
        mesh = new Mesh();


        vertices = new(_meshHeight * _meshWidth);
        for (int i = 0; i < vertices.Capacity; i++)
        {
            vertices.Add(Vector3.zero);
        }
        triangles = new((_meshWidth - 1) * (_meshHeight - 1) * 6);




        uvs = new Vector2[_meshWidth * _meshHeight];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
    }





    public Mesh UpdateMesh()
    {
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs;
        mesh.normals = normals;

        return mesh;
    }

}