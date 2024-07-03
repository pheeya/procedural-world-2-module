using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace ProcWorld
{
    public class MeshGenerator
    {
        public static MeshData GenerateMeshFromHeightMap(HeightMap _heightmap, float _heightScale, AnimationCurve _heightCurve, int _lod)
        {


            Profiler.BeginThreadProfiling("Terrain Background Thread", "Terrain Thread");

            int increment = _lod == 0 ? 1 : _lod * 2;
            int width = _heightmap.Width;
            int height = _heightmap.Height;


            int vertsPerSide = width - _heightmap.BorderSize * 2;
            int vertSpanSimplified = width - 2 * increment;
            int vertsPerSideSimplified = (vertSpanSimplified - 1) / increment + 1;

            int meshTargetPhysicalSize = vertsPerSide - 1;
            float topLeftX = meshTargetPhysicalSize / -2f;
            float topLeftZ = meshTargetPhysicalSize / 2f;


            int vertsInHeight = (height - 1) / increment + 1 - _heightmap.BorderSize * 2;

            MeshData meshData = new MeshData(vertsPerSideSimplified, vertsPerSideSimplified);




            int vertexIndex = 0;
            int borderVertexIndex = -1;

            List<int> borderTriangles = new();
            List<Vector3> borderVertices = new();
            int[,] vertexIndicesMap = new int[width, height];

            List<int> physicalBorderVertices = new();



            for (int y = 0; y < height; y += increment)
            {
                for (int x = 0; x < width; x += increment)
                {
                    bool isBorder = y < _heightmap.BorderSize || y > (height - _heightmap.BorderSize - 1) || x < _heightmap.BorderSize || x > (width - _heightmap.BorderSize - 1); ;
                    bool isPhysicalBorder = y == increment || y == (height - increment) || x == increment || x == width - increment;
                    if (isBorder)
                    {

                        vertexIndicesMap[x, y] = borderVertexIndex;
                        borderVertexIndex--;
                    }
                    else
                    {

                        vertexIndicesMap[x, y] = vertexIndex;

                        if (isPhysicalBorder)
                        {
                            physicalBorderVertices.Add(vertexIndex);
                        }


                        vertexIndex++;
                    }
                }
            }



            vertexIndex = 0;

            int physicalBorderVertsProcessed = 0;
            for (int y = 0; y < height; y += increment)
            {
                for (int x = 0; x < width; x += increment)
                {
                    int currentIndex = vertexIndicesMap[x, y];

                    bool isBorder = currentIndex < 0;



                    // remove this when reverting back to border aware code
                    // currentIndex = vertexIndex;



                    float normalizedX = x - increment;
                    normalizedX /= (vertSpanSimplified - 1);
                    float normalizedZ = y - increment;
                    normalizedZ /= (vertSpanSimplified - 1);
                    float yPos = _heightCurve.Evaluate(_heightmap.Values[x, y]) * _heightScale;

                    Vector3 pos = new Vector3(topLeftX + normalizedX * meshTargetPhysicalSize, yPos, topLeftZ - normalizedZ * meshTargetPhysicalSize); ;
                    if (isBorder)
                    {
                        borderVertices.Add(pos);
                    }
                    else
                    {

                        bool isphysicalBorder = physicalBorderVertsProcessed < physicalBorderVertices.Count;
                        if (isphysicalBorder)
                        {
                            isphysicalBorder = currentIndex == physicalBorderVertices[physicalBorderVertsProcessed];
                        }
                        if (isphysicalBorder)
                        {
                            meshData.vertices[currentIndex] = pos + Vector3.up * 100f;
                            physicalBorderVertsProcessed++;
                        }
                        else
                        {
                            meshData.vertices[currentIndex] = pos;

                        }
                        meshData.uvs[currentIndex] = new Vector2(x / (float)width, y / (float)height);
                    }
                    if (x < width - 1 && y < height - 1)
                    {
                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + increment, y];
                        int c = vertexIndicesMap[x, y + increment];
                        int d = vertexIndicesMap[x + increment, y + increment];

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


                        // meshData.AddTriangle(currentIndex, currentIndex + vertsInWidth + 1, vertexIndex + vertsInWidth);
                        // meshData.AddTriangle(currentIndex + vertsInWidth + 1, vertexIndex, vertexIndex + 1);
                    }
                    vertexIndex++;
                }
            }


            meshData.normals = CalculateNormals(meshData.vertices, meshData.triangles, borderVertices, borderTriangles);

            Profiler.EndThreadProfiling();


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


        public MeshData(int _meshWidth, int _meshHeight)
        {


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





        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Terrain Chunk Mesh";
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);

            return mesh;
        }

    }
}