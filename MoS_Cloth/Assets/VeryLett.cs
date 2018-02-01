using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VeryLett : MonoBehaviour
{
	public class ClothPoint
	{
		public Vector3	position;
		public Vector3	velocity;
		public bool		fixd;	

		public ClothPoint (Vector3 pos)
		{
			position = pos;
			velocity = Vector3.zero;
			fixd = false;
		}
	}

	public class ClothLink
	{
		public ClothPoint	A;
		public ClothPoint	B;
		public float		restDist;

		public ClothLink (ClothPoint a, ClothPoint b)
		{
			A = a;
			B = b;
			restDist = Vector3.Distance(A.position, B.position);
		}

		public void SolveLink (float k, float st)
		{
			// Relative vector going from A to B
			Vector3 dir = B.position-A.position;

			// Current distance between A and B
			float dist = dir.magnitude;

			// Normalize directional vector
			dir /= dist;

			// Calculate spring force
			Vector3 force = dir * (dist - restDist) * k * st;

			// Apply force to both points
			A.velocity += force;
			B.velocity -= force;
		}
	}

	// Public
	public float		mass					= 1;
	public float		springCoefficient		= 10;
	public float		dampeningCoefficient	= 3;
	public float		simTime					= 0.005f;
	[Range(0, 1)]
	public float		gravityMultiplier		= 0.25f;

	// Private
	float				remainder;

	List<ClothPoint>	points;
	List<ClothLink>		links;
	
	void Start ()
	{
		GetComponent<ClothFactory>().InitializeCloth(transform, ref points, ref links);
	}
	
	void OnDrawGizmos ()
	{
		if (points == null || links == null)
			return;

		// Draw points
		foreach (var point in points)
		{
			Gizmos.color = Color.yellow * 10;
			Gizmos.DrawSphere(point.position, 0.025f);
		}

		// Draw links
		foreach (var link in links)
		{
			Gizmos.color = Color.green * 10;
			Gizmos.DrawLine(link.A.position, link.B.position);
		}
	}
	
	void Update ()
	{
		points[0].position = transform.position;

		float pointMass = mass / points.Count;
		
		float dt = Time.deltaTime + remainder;
		int simSteps = (int)Mathf.Floor(dt / simTime);
		remainder = dt - simSteps * simTime;

		for (int i = 0; i < simSteps; i++)
		{
			// Apply gravity
			foreach (var point in points)
				point.velocity += Physics.gravity * pointMass * simTime * gravityMultiplier;

			// Calculate link constraints
			foreach (var link in links)
				link.SolveLink(springCoefficient, simTime);

			// Apply dampening
			foreach (var point in points)
				point.velocity *= Mathf.Max(1 - simTime * dampeningCoefficient, 0);

			// Apply forces
			foreach (var point in points)
				if (!point.fixd)
					point.position += point.velocity;
		}
	}
}
