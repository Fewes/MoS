﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * TODO: Normals and UVs need to be updated when merging borders in MergeOverlappingVertices
 */

public class ClothFactoryMesh : ClothFactory
{
	[SerializeField]
	Mesh		mesh;
    public bool PinFirstAndLastPoint = true;
    public bool MergeUVBorders = true;

    public class Edge
    {
        public int VertexA;
        public int VertexB;

        public Edge(int vertexa, int vertexb)
        {
            VertexA = vertexa;
            VertexB = vertexb;
        }

        public bool SameAs(Edge other)
        {
            // (1,0) == (1,0) || (1,0) == (0,1)
            return (other.VertexA == this.VertexA && other.VertexB == this.VertexB) ||
                   (other.VertexA == this.VertexB && other.VertexB == this.VertexA);
        }

        public string DebugString()
        {
            return "(" + this.VertexA + ", " + this.VertexB + " )";
        }
    }

    public void RemoveDuplicateEdges(List<ClothFactoryMesh.Edge> edges)
    {
        // Brute force. This is done without swapping or other list optimizations...
        int lastIndex = edges.Count - 1;
        for (int i = 0; i <= lastIndex; i++)
        {
            // Iterate from the back of the list and remove elements as we find duplicates
            for (int j = lastIndex; j > i; j--)
            {
                if (edges[i].SameAs(edges[j]))
                {
                    edges.RemoveAt(j);
                    lastIndex--;
                }
            }
        }
    }

    public bool TrianglesToEdges(ref int[] triangles, List<ClothFactoryMesh.Edge> edges)
    {
        edges.Clear();

        // Generate edges for all triangles
        int offset;
        int numTriangles = triangles.Length / 3;
        for (int i=0; i<numTriangles; i++)
        {
            offset = i * 3;
            edges.Add(new Edge(triangles[offset + 0], triangles[offset + 1]));
            edges.Add(new Edge(triangles[offset + 1], triangles[offset + 2]));
            edges.Add(new Edge(triangles[offset + 2], triangles[offset + 0]));
        }

        RemoveDuplicateEdges(edges);

        return (edges.Count > 0);
    }

    public void ReplaceVertexInTriangleList(int vertex, int replacement, List<int> triangles)
    {
        for (int i=0; i<triangles.Count; i++)
        {
            if (triangles[i] == vertex)
            {
                triangles[i] = replacement;
            }
        }
    }

    public void ReadjustTriangleListAfterRemoval(int removedVertex, int replacement, List<int> triangles)
    {
        if (removedVertex > replacement)
        {
            for (int i = 0; i < triangles.Count; i++)
            {
                if (triangles[i] > removedVertex)
                {
                    triangles[i]--;
                }
            }
        }
        else
        {
            for (int i = 0; i < triangles.Count; i++)
            {
                if (triangles[i] < removedVertex)
                {
                    triangles[i]++;
                }
            }
        }
    }

    public void MergeOverlappingVertices(ref Vector3[] vertices, ref int[] triangles, ref Color32[] colors)
    {
        List<Vector3> newVertices = new List<Vector3>(vertices);
        List<int> newTriangles = new List<int>(triangles);
        List<Color32> newColors = new List<Color32>(colors);

        List<int> duplicates = new List<int>();

        bool adjustColors = colors.Length > 0;

        // Naive implementation which merges vertices in the exact same location
        int lastIndex = newVertices.Count - 1;
        for (int i=0; i<=lastIndex; i++)
        {
            duplicates.Clear();
            Vector3 vertex = newVertices[i];

            // Store duplicates and remove them from the vertex list
            for (int j=lastIndex; j>i; j--)
            {
                if (vertex.Equals(newVertices[j]))
                {
                    duplicates.Add(j);
                    newVertices.RemoveAt(j);

                    if (adjustColors) newColors.RemoveAt(j);

                    lastIndex--;
                }
            }

            for (int j = 0; j < duplicates.Count; j++)
            {
                // If a vertex is removed, parts of the triangle array gets an index with faulty offset +/- 1
                // and must be recalculated
                ReplaceVertexInTriangleList(duplicates[j], i, newTriangles);
                ReadjustTriangleListAfterRemoval(duplicates[j], i, newTriangles);
            }
        }

        vertices = newVertices.ToArray();
        triangles = newTriangles.ToArray();
        colors = newColors.ToArray();
    }

	override public void InitializeCloth(Transform transform, ref List<VeryLett.ClothPoint> points, ref List<VeryLett.ClothLink> links)
	{
		points = new List<VeryLett.ClothPoint>();
		links  = new List<VeryLett.ClothLink>();

        if (mesh && mesh.vertexCount > 0)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Color32[] colors = mesh.colors32;

            if (MergeUVBorders)
            {
                MergeOverlappingVertices(ref vertices, ref triangles, ref colors);
            }

            int i;
            for (i=0; i < vertices.Length; i++)
            {
                points.Add(new VeryLett.ClothPoint(transform.position + vertices[i]));
            }

            for (i=0; i<colors.Length; i++)
            {
                points[i].fixd = (colors[i].r == 255);
            }

            if (PinFirstAndLastPoint)
            {
                points[0].fixd = true;
                points[points.Count-1].fixd = true;
            }

            List<ClothFactoryMesh.Edge> edges = new List<ClothFactoryMesh.Edge>();
            if (TrianglesToEdges(ref triangles, edges))
            {
                foreach (var edge in edges)
                {
                    links.Add(new VeryLett.ClothLink(points[edge.VertexA], points[edge.VertexB]));
                }
            }

            Debug.Log("MeshToCloth: Generating cloth from mesh...");
            Debug.Log("MeshToCloth: Converted vertices -> points: " + vertices.Length + " -> " + points.Count);
            Debug.Log("MeshToCloth: Number of triangles (arraylength): " + (triangles.Length/3) + " ("+ triangles.Length +")");
            Debug.Log("MeshToCloth: Converted edges -> links: " + edges.Count + " -> " + links.Count);
        }
    }
}