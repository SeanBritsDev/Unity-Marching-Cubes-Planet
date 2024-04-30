using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubesPlanet : MonoBehaviour
{
    [SerializeField] private int planetGridLength = 32;
    [SerializeField] private float planetRadius = 15f;
    [SerializeField] private float resolution = 1f;
    [SerializeField] private float noiseScale = 0.4f;
    [SerializeField] private float heightThreshold = 0.5f;
    [SerializeField] private bool visualizeNoise;
    [SerializeField] private bool xRayOn = false;
    [SerializeField] private Vector3 position = Vector3.zero; // Position variable

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private float[,,] heights;

    private MeshFilter meshFilter;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        GeneratePlanet();
    }

    void Update()
    {

    }

    private void GeneratePlanet()
    {
        transform.position = position; // Move the entire planet to the specified position
        SetHeights();
        MarchCubes();
        SetMesh();
    }

    private void SetMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    private void SetHeights()
    {
        heights = new float[planetGridLength + 1, planetGridLength + 1, planetGridLength + 1];

        Vector3 centerOffset = new Vector3((planetGridLength / 2f) * resolution, (planetGridLength / 2f) * resolution, (planetGridLength / 2f) * resolution); // Offset for centering grid

        for (int x = 0; x < planetGridLength + 1; x++)
        {
            for (int y = 0; y < planetGridLength + 1; y++)
            {
                for (int z = 0; z < planetGridLength + 1; z++)
                {
                    Vector3 worldPos = new Vector3(x * resolution, y * resolution, z * resolution) - centerOffset + position; // Adjust position
                    float distanceToCenter = Vector3.Distance(worldPos, position);
                    if (distanceToCenter <= planetRadius)
                    {
                        float currentHeight = Mathf.PerlinNoise(worldPos.x * noiseScale, worldPos.z * noiseScale);
                        float distanceToSurface = Mathf.Abs(worldPos.y - currentHeight * planetGridLength);
                        heights[x, y, z] = distanceToSurface;
                    }
                    else
                    {
                        heights[x, y, z] = -1f; // Mark points outside the planet's sphere
                    }
                }
            }
        }
    }

    private void MarchCubes()
    {
        vertices.Clear();
        triangles.Clear();

        for (int x = 0; x < planetGridLength; x++)
        {
            for (int y = 0; y < planetGridLength; y++)
            {
                for (int z = 0; z < planetGridLength; z++)
                {
                    float[] cubeCorners = new float[8];

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                        cubeCorners[i] = heights[corner.x, corner.y, corner.z];
                    }

                    MarchCube(new Vector3(x, y, z), cubeCorners);

                }
            }
        }
    }

    private void MarchCube(Vector3 position, float[] cubeCorners)
    {
        int configIndex = GetConfigIndex(cubeCorners);

        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }

        int edgeIndex = 0;
        for (int t = 0; t < 5; t++)
        {
            for (int v = 0; v < 3; v++)
            {
                int triTableValue = MarchingTable.Triangles[configIndex, edgeIndex];

                if (triTableValue == -1)
                {
                    return;
                }

                Vector3 edgeStart = position + MarchingTable.Edges[triTableValue, 0];
                Vector3 edgeEnd = position + MarchingTable.Edges[triTableValue, 1];

                // Reverse the winding order by swapping edgeStart and edgeEnd
                Vector3 vertex = (edgeStart + edgeEnd) / 2;

                vertices.Add(vertex);
                triangles.Add(vertices.Count - 1);

                edgeIndex++;
            }

            // Reverse the order of the last added vertices to flip the triangle
            int lastIndex = vertices.Count - 1;
            int secondLastIndex = lastIndex - 1;
            int thirdLastIndex = lastIndex - 2;

            if (xRayOn)
            {
                triangles[triangles.Count - 3] = thirdLastIndex;
                triangles[triangles.Count - 2] = secondLastIndex;
                triangles[triangles.Count - 1] = lastIndex;
            }
            else
            {
                // Adjust triangle indices to reverse winding order
                triangles[triangles.Count - 3] = lastIndex;
                triangles[triangles.Count - 2] = secondLastIndex;
                triangles[triangles.Count - 1] = thirdLastIndex;
            }
        }
    }

    private int GetConfigIndex(float[] cubeCorners)
    {
        int configIndex = 0;

        for (int i = 0; i < 8; i++)
        {
            if (cubeCorners[i] > heightThreshold)
            {
                configIndex |= 1 << i;
            }
        }

        return configIndex;
    }

    private void OnDrawGizmosSelected()
    {
        if (!visualizeNoise || !Application.isPlaying)
        {
            return;
        }

        int step = Mathf.Max(1, planetGridLength / 20); // Adjust the divisor for a suitable step size

        for (int x = 0; x < planetGridLength + 1; x += step)
        {
            for (int y = 0; y < planetGridLength + 1; y += step)
            {
                for (int z = 0; z < planetGridLength + 1; z += step)
                {
                    Vector3 worldPos = new Vector3(x * resolution, y * resolution, z * resolution) + position; // Adjust position
                    if (heights[x, y, z] == -1f) // Outside planet sphere
                    {
                        Gizmos.color = Color.blue;
                    }
                    else
                    {
                        Gizmos.color = new Color(heights[x, y, z], heights[x, y, z], heights[x, y, z]);
                    }

                    Gizmos.DrawSphere(worldPos, 0.2f * resolution);
                }
            }
        }
    }
}
