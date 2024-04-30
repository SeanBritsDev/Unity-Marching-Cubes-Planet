using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]

public class MeshGenerator : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    public int xSize = 20;
    public int zSize = 20;
    public float vertexSpacing = 1.0f;
    public bool gizmosDisplay = false;
    public float smoothness = 1.0f;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateShape();
        UpdateMesh();
    }

    void CreateShape() { 
        vertices = new Vector3[(xSize +1 ) * (zSize + 1)];

        int i = 0;
        for (int z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = Mathf.PerlinNoise(x * 0.5f, z * 0.5f) * 1f;
                vertices[i] = new Vector3(x * vertexSpacing, y * vertexSpacing, z * vertexSpacing);
                i++;
            }
        }

        if (smoothness > 0) {
            SmoothTerrain();
        }

        triangles = new int[xSize * zSize * 6];

        int vert = 0;
        int tris = 0;

        for(int z = 0;z <= zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                if (vert < vertices.Length && tris + 5 < triangles.Length)
                {
                    triangles[tris + 0] = vert + 0;
                    triangles[tris + 1] = vert + xSize + 1;
                    triangles[tris + 2] = vert + 1;
                    triangles[tris + 3] = vert + 1;
                    triangles[tris + 4] = vert + xSize + 1;
                    triangles[tris + 5] = vert + xSize + 2;

                    vert++;
                    tris += 6;
                }
            }
            vert++;
        }
    }

    void SmoothTerrain()
    {
        Vector3[] smoothedVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            Vector3 sum = vertex;
            int count = 1;

            // Check neighboring vertices
            if (i - 1 >= 0)
            {
                sum += vertices[i - 1];
                count++;
            }
            if (i + 1 < vertices.Length)
            {
                sum += vertices[i + 1];
                count++;
            }
            if (i - xSize - 1 >= 0)
            {
                sum += vertices[i - xSize - 1];
                count++;
            }
            if (i + xSize + 1 < vertices.Length)
            {
                sum += vertices[i + xSize + 1];
                count++;
            }

            // Calculate average height
            smoothedVertices[i] = Vector3.Lerp(vertex, sum / count, smoothness);
        }

        vertices = smoothedVertices;
    }

    void UpdateMesh() {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void OnDrawGizmos()
    {
        if (vertices == null) {
            return;
        }

        for(int i = 0; i < vertices.Length; i++)
        {
            if (gizmosDisplay) {
                Gizmos.DrawSphere(vertices[i], 0.05f);
            }
            
        }
    }
}
