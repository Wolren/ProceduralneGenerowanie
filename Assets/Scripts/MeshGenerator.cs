using UnityEngine;

/// <summary>
/// GenerateTerrainMesh to funkcja klasy MeshGenerator,
/// która generuje siatkę (Mesh) terenu na podstawie tablicy wysokości (heightMap),
/// ustawień siatki (meshSettings) oraz poziomu szczegółowości (levelOfDetail).
/// 
/// Najpierw funkcja oblicza wartość zmiennej skipIncrement na podstawie levelOfDetail,
/// która określa o ile kroków pomijane są wierzchołki siatki terenu.
/// Następnie obliczane są wartości topLeft i meshData,
/// a także tworzona jest tablica vertexIndicesMap, która przechowuje indeksy wierzchołków.
///
/// Następnie funkcja iteruje po wierzchołkach siatki terenu,
/// ustala, które wierzchołki znajdują się na granicy siatki, które są pomijane,
/// a które są podstawowymi wierzchołkami siatki. Dla każdego podstawowego wierzchołka,
/// funkcja dodaje nowy wierzchołek do siatki z jego współrzędnymi w przestrzeni 3D i teksturze.
/// Następnie funkcja tworzy trójkąty dla każdego prostokąta utworzonego przez 4 wierzchołki.
/// </summary>

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int numVertsPerLine = meshSettings.NumVertsPerLine;
        var topLeft = new Vector2(-1, 1) * meshSettings.MeshWorldSize / 2f;
        var meshData = new MeshData(numVertsPerLine, skipIncrement, meshSettings.użyjCieniowania);
        int[][] vertexIndicesMap = new int[numVertsPerLine][];
        for (int index = 0; index < numVertsPerLine; index++)
        {
            vertexIndicesMap[index] = new int[numVertsPerLine];
        }

        int meshVertexIndex = 0;
        int outOfMeshVertexIndex = -1;
        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                if (isOutOfMeshVertex)
                {
                    vertexIndicesMap[x][y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                }
                else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x][y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                if (isSkippedVertex) continue;
                bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                bool isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && !isOutOfMeshVertex;
                bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;
                int vertexIndex = vertexIndicesMap[x][y];
                var percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - 3);
                var vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.MeshWorldSize;
                float height = heightMap[x, y];
                if (isEdgeConnectionVertex)
                {
                    bool isVertical = x == 2 || x == numVertsPerLine - 3;
                    int dstToMainVertexA = ((isVertical) ? y - 2 : x - 2) % skipIncrement;
                    int dstToMainVertexB = skipIncrement - dstToMainVertexA;
                    float dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;
                    float heightMainVertexA = heightMap[(isVertical) ? x : x - dstToMainVertexA, (isVertical) ? y - dstToMainVertexA : y];
                    float heightMainVertexB = heightMap[(isVertical) ? x : x + dstToMainVertexB, (isVertical) ? y + dstToMainVertexB : y];
                    height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;
                }

                meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);
                bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));
                if (!createTriangle) continue;
                int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;
                int a = vertexIndicesMap[x][y];
                int b = vertexIndicesMap[x + currentIncrement][y];
                int c = vertexIndicesMap[x][y + currentIncrement];
                int d = vertexIndicesMap[x + currentIncrement][y + currentIncrement];
                meshData.AddTriangle(a, d, c);
                meshData.AddTriangle(d, a, b);
            }
        }

        meshData.ProcessMesh();
        return meshData;
    }
}

public class MeshData
{
    Vector3[] m_Vertices;
    readonly int[] m_Triangles;
    Vector2[] m_Uvs;
    Vector3[] m_BakedNormals;
    readonly Vector3[] m_OutOfMeshVertices;
    readonly int[] m_OutOfMeshTriangles;
    int m_TriangleIndex;
    int m_OutOfMeshTriangleIndex;
    readonly bool m_UseFlatShading;

    public MeshData(int numVertsPerLine, int skipIncrement, bool useFlatShading)
    {
        m_UseFlatShading = useFlatShading;
        int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
        int numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
        int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
        int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;
        m_Vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
        m_Uvs = new Vector2[m_Vertices.Length];
        int numMeshEdgeTriangles = 8 * (numVertsPerLine - 4);
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
        m_Triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];
        m_OutOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];
        m_OutOfMeshTriangles = new int[24 * (numVertsPerLine - 2)];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0) m_OutOfMeshVertices[-vertexIndex - 1] = vertexPosition;
        else
        {
            m_Vertices[vertexIndex] = vertexPosition;
            m_Uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            m_OutOfMeshTriangles[m_OutOfMeshTriangleIndex] = a;
            m_OutOfMeshTriangles[m_OutOfMeshTriangleIndex + 1] = b;
            m_OutOfMeshTriangles[m_OutOfMeshTriangleIndex + 2] = c;
            m_OutOfMeshTriangleIndex += 3;
        }
        else
        {
            m_Triangles[m_TriangleIndex] = a;
            m_Triangles[m_TriangleIndex + 1] = b;
            m_Triangles[m_TriangleIndex + 2] = c;
            m_TriangleIndex += 3;
        }
    }

    private Vector3[] CalculateNormals()
    {
        var vertexNormals = new Vector3[m_Vertices.Length];
        int triangleCount = m_Triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = m_Triangles[normalTriangleIndex];
            int vertexIndexB = m_Triangles[normalTriangleIndex + 1];
            int vertexIndexC = m_Triangles[normalTriangleIndex + 2];
            var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = m_OutOfMeshTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = m_OutOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = m_OutOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = m_OutOfMeshTriangles[normalTriangleIndex + 2];
            var triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0) vertexNormals[vertexIndexA] += triangleNormal;
            if (vertexIndexB >= 0) vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0) vertexNormals[vertexIndexC] += triangleNormal;
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        var pointA = (indexA < 0) ? m_OutOfMeshVertices[-indexA - 1] : m_Vertices[indexA];
        var pointB = (indexB < 0) ? m_OutOfMeshVertices[-indexB - 1] : m_Vertices[indexB];
        var pointC = (indexC < 0) ? m_OutOfMeshVertices[-indexC - 1] : m_Vertices[indexC];
        var sideAb = pointB - pointA;
        var sideAc = pointC - pointA;
        return Vector3.Cross(sideAb, sideAc).normalized;
    }

    public void ProcessMesh()
    {
        if (m_UseFlatShading) FlatShading();
        else BakeNormals();
    }

    private void BakeNormals()
    {
        m_BakedNormals = CalculateNormals();
    }

    private void FlatShading()
    {
        var flatShadedVertices = new Vector3[m_Triangles.Length];
        var flatShadedUvs = new Vector2[m_Triangles.Length];
        for (int i = 0; i < m_Triangles.Length; i++)
        {
            flatShadedVertices[i] = m_Vertices[m_Triangles[i]];
            flatShadedUvs[i] = m_Uvs[m_Triangles[i]];
            m_Triangles[i] = i;
        }

        m_Vertices = flatShadedVertices;
        m_Uvs = flatShadedUvs;
    }

    public Mesh CreateMesh()
    {
        var mesh = new Mesh
        {
            vertices = m_Vertices,
            triangles = m_Triangles,
            uv = m_Uvs
        };
        if (m_UseFlatShading) mesh.RecalculateNormals();
        else mesh.normals = m_BakedNormals;
        return mesh;
    }
}