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
        int numPixelsX = cellsX + 1 ;
        int numPixelsY = cellsY + 1;

        // Add points
       for (int y = 0; y < numPixelsY; y++)
        {
            for (int x = 0; x < numPixelsX; x++)
            {
                // for each row , jump to the start of the dimensional square and create points with a set distance to the right.
                points.Add(new VeryLett.ClothPoint(transform.position -Vector3.up * y * (dimY/cellsY) -Vector3.right * (dimX/2) + Vector3.right * x * (dimX/cellsX)));
               // Debug.Log(dimX/cellsX);
            }
        }

        // Add links horizontal
        for (int rowNum = 0; rowNum <= numPixelsY; ++rowNum)
        {
            for (int link = 0; link < cellsX; ++link)
            {
                links.Add(new VeryLett.ClothLink(points[rowNum * numPixelsX + link], points[rowNum * numPixelsX + link + 1]));
            }
        }

        // Add links vertical
        for (int colNum = 0; colNum <= numPixelsX; ++colNum)
        {
            for (int link = 0; link < cellsY; ++link)
            {
                links.Add(new VeryLett.ClothLink(points[colNum * numPixelsY + link * numPixelsX], points[colNum * numPixelsY + link * numPixelsX + numPixelsX])); //Funkar ej
                //links.Add(new VeryLett.ClothLink(points[0], points[4]));  <-- Det funkar inte heller
            }
        }
    }
}
