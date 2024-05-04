using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubesPlanet : MonoBehaviour
{
    [SerializeField] private int planetGridLength = 32; // The size of the grid that defines the planet's shape
    [SerializeField] private float planetRadius = 15f; // The radius of the planet
    [SerializeField] private float resolution = 1f; // The resolution of the grid
    [SerializeField] private float heightThreshold = 0.5f; // Threshold for determining terrain height
    [SerializeField] private bool visualizeNoise; // Toggle to visualize the noise
    [SerializeField] private bool xRayOn = false; // Toggle to reverse triangle winding order
    [SerializeField] private Vector3 position = Vector3.zero; // Position of the planet

    private List<Vector3> vertices = new List<Vector3>(); // List to store generated vertices
    private List<int> triangles = new List<int>(); // List to store generated triangles
    private float[,,] heights; // Array to store heights of each point in the grid


    private MeshFilter meshFilter;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>(); // Get reference to MeshFilter component
        GeneratePlanet(); // Generate the planet mesh
    }

    void Update()
    {
        // Update logic can be added here if needed
    }

    // Method to generate the planet mesh
    private void GeneratePlanet()
    {
        transform.position = position; // Move the entire planet to the specified position
        SetHeights(); // Calculate heights of grid points
        MarchCubes(); // Generate mesh using marching cubes algorithm
        SetMesh(); // Set generated mesh to MeshFilter component
    }

    // Method to set the mesh to MeshFilter component
    private void SetMesh()
    {
        Mesh mesh = new Mesh(); // Create a new mesh object

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Set index format to UInt32 for large meshes

        mesh.vertices = vertices.ToArray(); // Assign vertices to mesh
        mesh.triangles = triangles.ToArray(); // Assign triangles to mesh
        mesh.RecalculateNormals(); // Recalculate normals for proper shading

        meshFilter.mesh = mesh; // Assign mesh to MeshFilter component
    }


    // Method to calculate heights of grid points
    private void SetHeights()
    {
        heights = new float[planetGridLength + 1, planetGridLength + 1, planetGridLength + 1]; // Initialize heights array

        Vector3 centerOffset = new Vector3((planetGridLength / 2f) * resolution, (planetGridLength / 2f) * resolution, (planetGridLength / 2f) * resolution); // Offset for centering grid

        // Loop through each grid point
        for (int x = 0; x < planetGridLength + 1; x++)
        {
            for (int y = 0; y < planetGridLength + 1; y++)
            {
                for (int z = 0; z < planetGridLength + 1; z++)
                {
                    Vector3 worldPos = new Vector3(x * resolution, y * resolution, z * resolution) - centerOffset + position; // Adjust position
                    float distanceToCenter = Vector3.Distance(worldPos, position);

                    // Check if the point is inside the planet's sphere
                    if (distanceToCenter <= planetRadius)
                    {
                        heights[x, y, z] = planetRadius; 
                    }
                    else
                    {
                        heights[x, y, z] = -1f; // Mark points outside the planet's sphere
                    }
                }
            }
        }
    }

    // Method to generate mesh using marching cubes algorithm
    private void MarchCubes()
    {
        vertices.Clear(); // Clear list of vertices
        triangles.Clear(); // Clear list of triangles

        // Loop through each grid cell
        for (int x = 0; x < planetGridLength; x++)
        {
            for (int y = 0; y < planetGridLength; y++)
            {
                for (int z = 0; z < planetGridLength; z++)
                {
                    float[] cubeCorners = new float[8];

                    // Calculate heights of cube corners
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                        cubeCorners[i] = heights[corner.x, corner.y, corner.z];
                    }

                    MarchCube(new Vector3(x, y, z), cubeCorners); // Perform marching cubes on current cube
                }
            }
        }
    }


    // Method to generate mesh for a single cube using marching cubes algorithm
    private void MarchCube(Vector3 position, float[] cubeCorners)
    {
        int configIndex = GetConfigIndex(cubeCorners); // Get configuration index for the cube

        // Skip cube if it's completely inside or outside the surface
        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }

        int edgeIndex = 0;

        // Loop through each triangle in the cube
        for (int t = 0; t < 5; t++)
        {
            for (int v = 0; v < 3; v++)
            {
                int triTableValue = MarchingTable.Triangles[configIndex, edgeIndex]; // Get triangle index from triangle table

                if (triTableValue == -1)
                {
                    return;
                }

                int edgeStartIndex = MarchingTable.CubeEdges[triTableValue, 0]; // Get start vertex index of the edge
                int edgeEndIndex = MarchingTable.CubeEdges[triTableValue, 1]; // Get end vertex index of the edge

                Vector3 edgeStart = position + MarchingTable.Corners[edgeStartIndex]; // Get start vertex position of the edge
                Vector3 edgeEnd = position + MarchingTable.Corners[edgeEndIndex]; // Get end vertex position of the edge

                // Interpolate vertex position based on neighboring vertices
                float edgeStartValue = cubeCorners[edgeStartIndex];
                float edgeEndValue = cubeCorners[edgeEndIndex];
                float threshold = heightThreshold;

                Vector3 vertex = InterpolateVertexPosition(edgeStart, edgeEnd, edgeStartValue, edgeEndValue, threshold);

                vertices.Add(vertex); // Add vertex to list
                triangles.Add(vertices.Count - 1); // Add index of vertex to triangles list

                edgeIndex++;
            }

            // Reverse the order of vertices to flip the triangle winding order
            int lastIndex = vertices.Count - 1;
            int secondLastIndex = lastIndex - 1;
            int thirdLastIndex = lastIndex - 2;

            if (xRayOn)
            {
                // If X-ray mode is on, reverse the winding order
                triangles[triangles.Count - 3] = thirdLastIndex;
                triangles[triangles.Count - 2] = secondLastIndex;
                triangles[triangles.Count - 1] = lastIndex;
            }
            else
            {
                // Otherwise, adjust triangle indices to maintain proper winding order
                triangles[triangles.Count - 3] = lastIndex;
                triangles[triangles.Count - 2] = secondLastIndex;
                triangles[triangles.Count - 1] = thirdLastIndex;
            }
        }
    }

    // Method to interpolate vertex position based on neighboring vertices
    private Vector3 InterpolateVertexPosition(Vector3 edgeStart, Vector3 edgeEnd, float edgeStartValue, float edgeEndValue, float threshold)
    {
        // Check if the threshold intersects the edge
        if (edgeStartValue < threshold && edgeEndValue < threshold || edgeStartValue >= threshold && edgeEndValue >= threshold)
        {
            // Both vertices are above or below the threshold, return the midpoint
            return (edgeStart + edgeEnd) / 2;
        }
        else
        {
            // Interpolate the vertex position along the edge
            float t = (threshold - edgeStartValue) / (edgeEndValue - edgeStartValue);
            return Vector3.Lerp(edgeStart, edgeEnd, t);
        }
    }



    // Method to get configuration index based on cube corners
    private int GetConfigIndex(float[] cubeCorners)
    {
        int configIndex = 0;

        // Loop through each cube corner
        for (int i = 0; i < 8; i++)
        {
            // Set bit in config index based on whether the corner is above the height threshold
            if (cubeCorners[i] > heightThreshold)
            {
                configIndex |= 1 << i;
            }
        }

        return configIndex; // Return the configuration index
    }

    // Method to visualize Perlin noise in editor
    private void OnDrawGizmosSelected()
    {
        if (!visualizeNoise || !Application.isPlaying)
        {
            return;
        }

        // Calculate a suitable step size based on the size of the grid
        int step = Mathf.Max(1, Mathf.RoundToInt(planetGridLength / 20f));

        // Loop through grid points and draw spheres to visualize noise
        for (int x = 0; x < planetGridLength + 1; x += step)
        {
            for (int y = 0; y < planetGridLength + 1; y += step)
            {
                for (int z = 0; z < planetGridLength + 1; z += step)
                {
                    Vector3 worldPos = new Vector3(x * resolution, y * resolution, z * resolution) + position; // Adjust position

                    // Set color based on noise value
                    if (heights[x, y, z] == -1f) // Outside planet sphere
                    {
                        Gizmos.color = Color.blue;
                    }
                    else
                    {
                        Gizmos.color = new Color(heights[x, y, z], heights[x, y, z], heights[x, y, z]);
                    }

                    Gizmos.DrawSphere(worldPos, 0.2f * resolution); // Draw sphere to visualize noise
                }
            }
        }
    }

}
