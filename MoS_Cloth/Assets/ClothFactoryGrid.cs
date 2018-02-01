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

		
	}
}
