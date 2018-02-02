using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * TODO: Unity duplicates vertices for UV borders, which means meshes become segmented.
 *       - Detect which points are duplicates and create a new vertex list before generating the rest
 *       - Remember to update the triangle IDs to not point at invalid indices
 */

public class ClothFactoryMesh : ClothFactory
{
	[SerializeField]
	Mesh		mesh;
    public bool PinFirstAndLastPoint = true;

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

    public bool TrianglesToEdges(int[] triangles, List<ClothFactoryMesh.Edge> edges)
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

	override public void InitializeCloth(Transform transform, ref List<VeryLett.ClothPoint> points, ref List<VeryLett.ClothLink> links)
	{
		points = new List<VeryLett.ClothPoint>();
		links  = new List<VeryLett.ClothLink>();

        if (mesh && mesh.vertexCount > 0)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            int i = 0;
            while (i < vertices.Length)
            {
                points.Add(new VeryLett.ClothPoint(transform.position + vertices[i]));
                i++;
            }

            if (PinFirstAndLastPoint)
            {
                points[0].fixd = true;
                points[points.Count-1].fixd = true;
            }

            List<ClothFactoryMesh.Edge> edges = new List<ClothFactoryMesh.Edge>();
            if (TrianglesToEdges(triangles, edges))
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
