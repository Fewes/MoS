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

	override public void InitializeCloth(Transform transform, ref List<VeryLett.ClothPoint> points, ref List<VeryLett.ClothLink> links)
	{
		points = new List<VeryLett.ClothPoint>();
		links  = new List<VeryLett.ClothLink>();
        int numPointsX = cellsX + 1;
        int numPointsY = cellsY + 1;

        // Add points
        Vector3 stepY = Vector3.up * (dimY / cellsY);
        Vector3 stepX = Vector3.right * (dimX / cellsX);
        Vector3 origin = transform.position - Vector3.right * (dimX / 2);
        for (int y = 0; y < numPointsY; y++)
        {
            for (int x = 0; x < numPointsX; x++)
            {
                points.Add(new VeryLett.ClothPoint(origin + stepX * x - stepY * y));
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

                links.Add(new VeryLett.ClothLink(points[previousIndex], points[currentIndex]));
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

                    links.Add(new VeryLett.ClothLink(points[previousIndex], points[currentIndex]));
                }
            }
        }
    }
}
