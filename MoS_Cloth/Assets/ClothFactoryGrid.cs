using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothFactoryGrid : ClothFactory
{
	public float	dimX;
	public float	dimY;
	public int		cellsX;
	public int		cellsY;
    public bool     crossLinks = false;
    public bool     attachTopCorners = false;

	void OnDrawGizmos ()
	{
		var p0 = transform.position - transform.right * dimX * 0.5f;
		var p1 = transform.position + transform.right * dimX * 0.5f;
		var p2 = p0 - transform.up * dimY;
		var p3 = p1 - transform.up * dimY;
		Gizmos.DrawLine(p0, p1);
		Gizmos.DrawLine(p1, p3);
		Gizmos.DrawLine(p3, p2);
		Gizmos.DrawLine(p2, p0);
	}

	override public void InitializeCloth(Transform transform, ref List<VeryLett.ClothPoint> points, ref List<VeryLett.ClothLink> links, ref List<VeryLett.ClothLink> xLinks, ref MeshFilter meshFilter, ref List<VeryLett.ClothPointAttachment> attachedPoints)
	{
		points = new List<VeryLett.ClothPoint>();
		links  = new List<VeryLett.ClothLink>();
        xLinks = new List<VeryLett.ClothLink>();
        int numPointsX = cellsX + 1;
        int numPointsY = cellsY + 1;

		Vector3[] vertices = new Vector3[numPointsX * numPointsY];
		int i = 0;

        // Add points
        Vector3 stepY = transform.up * (dimY / cellsY);
        Vector3 stepX = transform.right * (dimX / cellsX);
        Vector3 origin = transform.position - transform.right * (dimX / 2);
        for (int y = 0; y < numPointsY; y++)
        {
            for (int x = 0; x < numPointsX; x++)
            {
				var pos = origin + stepX * x - stepY * y + Random.onUnitSphere * Mathf.Epsilon;
                points.Add(new VeryLett.ClothPoint(pos, new int[] {i}));
				vertices[i++] = transform.InverseTransformPoint(pos);
            }
        }

        // Add horizontal links
        for (int y = 0; y < numPointsY; y++)
        {
            int currentRow = y * numPointsX;

            for (int x = 1; x < numPointsX; x++)
            {
                int currentIndex = currentRow + x;
                links.Add(new VeryLett.ClothLink(points[currentIndex-1], points[currentIndex]));
            }
        }

        // Add vertical links
        for (int y = 1; y < numPointsY; y++)
        {
            int previousRow = (y-1) * numPointsX;
            int currentRow = y * numPointsX;

            for (int x = 0; x < numPointsX; x++)
            {
                int previousIndex = previousRow + x;
                int currentIndex = currentRow + x;

                links.Add(new VeryLett.ClothLink(points[previousIndex], points[currentIndex]));
            }
        }

        // Cross-link
        // Add first set of vertical links
        for (int y = 1; y < numPointsY; y++)
        {
            int previousRow = (y - 1) * numPointsX;
            int currentRow = y * numPointsX;

            for (int x = 0; x < (numPointsX-1); x++)
            {
                int previousIndex = previousRow + x;
                int currentIndex = currentRow + x + 1;

                xLinks.Add(new VeryLett.ClothLink(points[previousIndex], points[currentIndex]));
            }
        }

        if (crossLinks)
        {
            for (int y = 1; y < numPointsY; y++)
            {
                int previousRow = (y - 1) * numPointsX;
                int currentRow = y * numPointsX;

                for (int x = 0; x < (numPointsX - 1); x++)
                {
                    int previousIndex = previousRow + x + 1;
                    int currentIndex = currentRow + x;

                    xLinks.Add(new VeryLett.ClothLink(points[previousIndex], points[currentIndex]));
                }
            }
        }

        if (attachTopCorners)
        {
            points[0].pinned = true;
            points[cellsX].pinned = true;
        }

		// Initialize the mesh object
		if (!meshFilter)
			meshFilter = gameObject.AddComponent<MeshFilter>();

		int[] triangles = new int[cellsX*cellsY*2*3];
		i = 0;
		for (int y = 0; y < cellsY; y++)
        {
            for (int x = 0; x < cellsX; x++)
            {
				// Triangle 1
				triangles[i++] = numPointsX * y + x;
				triangles[i++] = numPointsX * y + x + 1;
				triangles[i++] = numPointsX * y + x + numPointsX;
				// Triangle 2
				triangles[i++] = numPointsX * y + x + 1;
				triangles[i++] = numPointsX * y + x + numPointsX + 1;
				triangles[i++] = numPointsX * y + x + numPointsX;
			}
		}

		Mesh mesh = new Mesh();
		mesh.name = gameObject.name + "_VeryLettMesh";
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		meshFilter.sharedMesh = mesh;
    }

    override public float GetArea()
    {
        return dimX * dimY;
    }
}
