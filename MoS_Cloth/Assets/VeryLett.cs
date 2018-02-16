using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VeryLett : MonoBehaviour
{
	public class ClothPoint
	{
		public Vector3	position;
		public Vector3	velocity;
		public Vector3  accumulatedVelocity;
        public bool     pinned;
		public int[]	vIndices;

		public ClothPoint (Vector3 pos)
		{
			position = pos;
			velocity = Vector3.zero;
			pinned = false;
		}

		public ClothPoint (Vector3 pos, int[] indices)
		{
			position = pos;
			velocity = Vector3.zero;
			pinned = false;
			vIndices = indices;
		}
	}

    public class ClothPointAttachment
    {
        public ClothPoint point;
        public Transform parent;
        public Vector3 bindPosition;

        public ClothPointAttachment(ClothPoint p, Transform parentTransform)
        {
            point = p;
            parent = parentTransform;

            bindPosition = parent.InverseTransformPoint(p.position);
        }

        public void UpdatePosition()
        {
            point.position = parent.TransformPoint(bindPosition);
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

		public void SolveLink(float k, float st, float pointMass)
		{
			// Relative vector going from A to B
			Vector3 dir = B.position - A.position;

			// Current distance between A and B
			float dist = dir.magnitude;

			// Normalize directional vector
			dir /= dist;

			// Calculate spring force
			Vector3 force = k * dir * (dist - restDist);

			// Apply force to both points
			A.accumulatedVelocity += force / pointMass * st;
			B.accumulatedVelocity -= force / pointMass * st;
		}

		public void SolveLinkWithDamper(float k, float st, float pointMass, float dampeningCoefficient)
		{
			// Relative vector going from A to B
			Vector3 r = B.position - A.position;

			// Calculate spring force
			Vector3 force = k * (r.magnitude - restDist) * r.normalized;

			// Calculate dampening
			Vector3 vA = A.velocity + B.velocity;
			Vector3 vB = B.velocity + A.velocity;
			Vector3 Fa = dampeningCoefficient * Vector3.Project(vA, r.normalized);
			Vector3 Fb = dampeningCoefficient * Vector3.Project(vB, r.normalized);

			// Store accumulated velocities for later
			Vector3 springDisplacement = force / pointMass * st;
			A.accumulatedVelocity +=  springDisplacement - Fa / pointMass * st;
			B.accumulatedVelocity += -springDisplacement - Fb / pointMass * st;
		}
	}

    public void SetPreset(PresetEnum newPreset)
    {
        tempPreset = newPreset;
        preset = newPreset;

        switch (preset)
        {
            case PresetEnum.Wool:
                mass = 1.2f;
                springCoefficient = 50;
                globalDampening = 4;
                internalDampening = 0.002f;
                crossLinkStrength = 0.04f;
                solver = SolverEnum.InternalDamperForce;
                break;

            case PresetEnum.Silk:
                mass = 0.75f;
                springCoefficient = 100;
                globalDampening = 4;
                internalDampening = 0.002f;
                crossLinkStrength = 0.1f;
                solver = SolverEnum.InternalDamperForce;
                break;

            case PresetEnum.Leather:
                mass = 10;
                springCoefficient = 220;
                globalDampening = 1;
                internalDampening = 0.04f;
                crossLinkStrength = 1.5f;
                solver = SolverEnum.InternalDamperForce;
                break;

            case PresetEnum.Default:
                
                break;

        }
    }

	[System.Serializable]
	public enum SolverEnum { Default, InternalDamperForce };

    [System.Serializable]
    public enum PresetEnum { Default, Wool, Silk , Leather };


    // Public
    [SerializeField]
    public PresetEnum   preset                  = PresetEnum.Default;
    PresetEnum          tempPreset              = PresetEnum.Default;

    public float		mass					= 1;
	public float		springCoefficient		= 10;
	public float		globalDampening			= 0;
	public float		simTime					= 0.005f;
	[Range(0, 1)]
	public float		gravityMultiplier		= 0.25f;
	public SolverEnum   solver				    = SolverEnum.Default;

	[Range(0, 0.2f)]
	public float		internalDampening	   = 0.01f;
    [Range(0, 2f)]
    public float        crossLinkStrength      = 0.5f;

    public Transform    attachmentTransform    = null;

    // Private
    float				remainder;

	List<ClothPoint>	points;
	List<ClothLink>		links;
    List<ClothLink>     xLinks;

	MeshFilter			meshFilter;
	Vector3[]			vertices;

    List<ClothPointAttachment> attachedPoints;

    void Start ()
	{
        SetPreset(preset);

        GetComponent<ClothFactory>().InitializeCloth(transform, ref points, ref links, ref xLinks, ref meshFilter);
        attachedPoints = new List<ClothPointAttachment>();

        if (meshFilter)
		{
			meshFilter.sharedMesh.MarkDynamic();
			vertices = meshFilter.sharedMesh.vertices;
		}

        // Default attachment is the GameObject transform
        if (!attachmentTransform) attachmentTransform = transform;

        // Create list of attached points so that we can update them in the main Update loop
        foreach (var p in points)
        {
            if (p.pinned)
            {
                attachedPoints.Add(new ClothPointAttachment(p, attachmentTransform));
            }
        }
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

		// Draw horizontal and vertical links
		foreach (var link in links)
		{
			Gizmos.color = Color.green * 10;
			Gizmos.DrawLine(link.A.position, link.B.position);
		}

        // Draw crosslinks
        foreach (var xLink in xLinks)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(xLink.A.position, xLink.B.position);
        }
    }
	
	void Update ()
	{
        if (tempPreset != preset)
            SetPreset(preset);

		if (points == null) return;
		float pointMass = mass / points.Count;

		points[0].position = transform.position;
		float dt = Time.deltaTime + remainder;
		int simSteps = (int)Mathf.Floor(dt / simTime);
		remainder = dt - simSteps * simTime;

		for (int i = 0; i < simSteps; i++)
		{
            // Transform attached points
            foreach (var p in attachedPoints)
            {
                p.UpdatePosition();
            }

            // Apply gravity
            foreach (var point in points)
			{
				point.accumulatedVelocity = Vector3.zero;
				point.accumulatedVelocity += Physics.gravity * gravityMultiplier * simTime; // m/s^2 * s = m/s
			}

			// Calculate link constraints
			if (solver == SolverEnum.InternalDamperForce)
			{
				foreach (var link in links)
				{
					link.SolveLinkWithDamper(springCoefficient, simTime, pointMass, internalDampening);
				}

                foreach (var xLink in xLinks)
                {
                    xLink.SolveLinkWithDamper(springCoefficient * crossLinkStrength, simTime, pointMass, internalDampening);
                }

            }
			else // SolverEnum.Default
			{
				foreach (var link in links)
				{
					link.SolveLink(springCoefficient, simTime, pointMass);
				}
                foreach (var xLink in xLinks)
                {
                    xLink.SolveLink(springCoefficient * crossLinkStrength, simTime, pointMass);
                }
            }

			foreach (var point in points)
			{
				point.velocity += point.accumulatedVelocity;
			}

			// Apply global dampening
			foreach (var point in points)
				point.velocity *= Mathf.Max(1 - simTime * globalDampening, 0);

			// Apply forces
			foreach (var point in points)
			if (!point.pinned)
				point.position += point.velocity * simTime; // m/s * s = m
		}

		UpdateMesh();
	}

	void UpdateMesh ()
	{
		if (!meshFilter)
			return;

		foreach (var point in points)
		{
			if (point.vIndices == null)
				continue;

			foreach (var i in point.vIndices)
			{
				vertices[i] = meshFilter.transform.InverseTransformPoint(point.position);
			}
		}

		meshFilter.sharedMesh.vertices = vertices;
		meshFilter.sharedMesh.RecalculateBounds();
		meshFilter.sharedMesh.RecalculateNormals();
		meshFilter.sharedMesh.RecalculateTangents();
	}
}
