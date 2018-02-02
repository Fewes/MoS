using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothFactoryGrid : ClothFactory
{
	public float	dimX;
	public float	dimY;
	public int		cellsX;
	public int		cellsY;

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
        int numPointsX = cellsX + 1 ;
        int numPointsY = cellsY + 1;

        // Add points
       for (int y = 0; y < numPointsY; y++)
        {
            for (int x = 0; x < numPointsX; x++)
            {
                // for each row , jump to the start of the dimensional square and create points with a set distance to the right.
                points.Add(new VeryLett.ClothPoint(transform.position -Vector3.up * y * (dimY/cellsY) -Vector3.right * (dimX/2) + Vector3.right * x * (dimX/cellsX)));
               // Debug.Log(dimX/cellsX);
            }
        }

        
        for (int y = 0; y <= cellsY; ++y)
        {
            for (int x = 0; x < cellsX; x++)
            {
                int currentRow = y * numPointsX;
                int currentColumn = x;
                int nextColumn = x + 1;
                int nextRow = currentRow + numPointsX;
                // Add links horizontal
                links.Add(new VeryLett.ClothLink(points[currentRow + currentColumn], points[currentRow + nextColumn]));
                // Add links vertical
                //links.Add(new VeryLett.ClothLink(points[currentRow + currentColumn], points[nextRow + currentColumn]));  <----- Vägrar fungera! 
                print(currentRow + x);
                print(currentRow + (x + 1));
            }
        }

        ////Add links vertical
        //for (int y = 0; y <= numPointsY; ++y)
        //{
        //    for (int x = 0; x < cellsY; ++x)
        //    {
        //        int currentRow = y * numPointsX;
        //        int nextRow = (y + 1) * numPointsX;

        //        links.Add(new VeryLett.ClothLink(points[currentRow + x], points[nextRow + x + 1]));
        //    }
        //}
    }
}
