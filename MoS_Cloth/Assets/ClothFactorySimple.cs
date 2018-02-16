using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothFactorySimple : ClothFactory
{
	override public void InitializeCloth(Transform transform, ref List<VeryLett.ClothPoint> points, ref List<VeryLett.ClothLink> links, ref List<VeryLett.ClothLink> xLinks, ref MeshFilter meshFilter)
	{
		points = new List<VeryLett.ClothPoint>();
		links  = new List<VeryLett.ClothLink>();

		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.00f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.33f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.66f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 1.00f));

		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.00f + Vector3.right * 0.33f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.33f + Vector3.right * 0.33f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.66f + Vector3.right * 0.33f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 1.00f + Vector3.right * 0.33f));

		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.00f + Vector3.right * 0.66f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.33f + Vector3.right * 0.66f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.66f + Vector3.right * 0.66f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 1.00f + Vector3.right * 0.66f));

		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.00f + Vector3.right * 0.99f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.33f + Vector3.right * 0.99f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 0.66f + Vector3.right * 0.99f));
		points.Add(new VeryLett.ClothPoint(transform.position - Vector3.up * 1.00f + Vector3.right * 0.99f));

		points[0].pinned = true;
		//points[4].pinned = true;
		//points[8].pinned = true;
		points[12].pinned = true;

		// Vertical links
		links.Add(new VeryLett.ClothLink(points[0], points[1]));
		links.Add(new VeryLett.ClothLink(points[1], points[2]));
		links.Add(new VeryLett.ClothLink(points[2], points[3]));

		links.Add(new VeryLett.ClothLink(points[4], points[5]));
		links.Add(new VeryLett.ClothLink(points[5], points[6]));
		links.Add(new VeryLett.ClothLink(points[6], points[7]));

		links.Add(new VeryLett.ClothLink(points[8], points[9]));
		links.Add(new VeryLett.ClothLink(points[9], points[10]));
		links.Add(new VeryLett.ClothLink(points[10], points[11]));

		links.Add(new VeryLett.ClothLink(points[12], points[13]));
		links.Add(new VeryLett.ClothLink(points[13], points[14]));
		links.Add(new VeryLett.ClothLink(points[14], points[15]));

		// Horizontal links
		links.Add(new VeryLett.ClothLink(points[0], points[4]));
		links.Add(new VeryLett.ClothLink(points[1], points[5]));
		links.Add(new VeryLett.ClothLink(points[2], points[6]));
		links.Add(new VeryLett.ClothLink(points[3], points[7]));

		links.Add(new VeryLett.ClothLink(points[4], points[8]));
		links.Add(new VeryLett.ClothLink(points[5], points[9]));
		links.Add(new VeryLett.ClothLink(points[6], points[10]));
		links.Add(new VeryLett.ClothLink(points[7], points[11]));

		links.Add(new VeryLett.ClothLink(points[8], points[12]));
		links.Add(new VeryLett.ClothLink(points[9], points[13]));
		links.Add(new VeryLett.ClothLink(points[10], points[14]));
		links.Add(new VeryLett.ClothLink(points[11], points[15]));

		// Cross links
		links.Add(new VeryLett.ClothLink(points[0], points[5]));
		links.Add(new VeryLett.ClothLink(points[1], points[6]));
		links.Add(new VeryLett.ClothLink(points[2], points[7]));
		links.Add(new VeryLett.ClothLink(points[1], points[4]));
		links.Add(new VeryLett.ClothLink(points[2], points[5]));
		links.Add(new VeryLett.ClothLink(points[3], points[6]));

		links.Add(new VeryLett.ClothLink(points[4], points[9]));
		links.Add(new VeryLett.ClothLink(points[5], points[10]));
		links.Add(new VeryLett.ClothLink(points[6], points[11]));
		links.Add(new VeryLett.ClothLink(points[5], points[8]));
		links.Add(new VeryLett.ClothLink(points[6], points[9]));
		links.Add(new VeryLett.ClothLink(points[7], points[10]));

		links.Add(new VeryLett.ClothLink(points[8],  points[13]));
		links.Add(new VeryLett.ClothLink(points[9],  points[14]));
		links.Add(new VeryLett.ClothLink(points[10], points[15]));
		links.Add(new VeryLett.ClothLink(points[9],  points[12]));
		links.Add(new VeryLett.ClothLink(points[10], points[13]));
		links.Add(new VeryLett.ClothLink(points[11], points[14]));
	}
}
