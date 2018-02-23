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

    [System.Serializable]
    public class Wind
    {
        private Vector3 position = Vector3.zero;
        public Vector3 direction = Vector3.forward;
        public float frequency = 0.6f;
        public float speed = 1.0f;
        [Range(1, 10)]
        public float power = 1;
        public bool debugPreview = false;

        public Wind() {}

        public void SetWindProperties(Vector3 dir, float sp)
        {
            direction = dir;
            speed = sp;
        }

        public void Update(float deltaTime)
        {
            position += direction * speed * deltaTime;
        }

        public float GetGustMultiplier(float worldx, float worldz, float worldy)
        {
            float GustFloor = Mathf.PerlinNoise((worldx - position.x) * frequency, (worldz - position.z) * frequency);
            float GustWall = Mathf.PerlinNoise((worldx - position.x) * frequency, (worldy - position.y) * frequency);
            return Mathf.Pow((GustFloor + GustWall)/2,power);
        }

        public Vector3 GetForce(float worldx, float worldz, float worldy, float area)
        {
            float airDensity = 1.2256f;
            float pressure = airDensity * speed * speed / 2;
            float forceMagnitude = pressure * area;

            return direction * forceMagnitude * GetGustMultiplier(worldx, worldz, worldy);
        }

        public void DrawDebugVolume()
        {
            Vector3 debugDrawOrigin = Vector3.zero;
            int numPoints = 20;
            int ypoints = 15;
            float width = 10.0f;
            float stepSize = width / numPoints;
            for (int x = 0; x < numPoints; x++)
            {
                for (int z = 0; z < numPoints; z++)
                {
                    for (int y = 0; y < ypoints; y++)
                    {
                        Vector3 worldCoord = debugDrawOrigin + new Vector3(x * stepSize, y * (width / ypoints), z * stepSize);    // Vector3.right * x * stepSize + Vector3.forward * z * stepSize + ;
                        float gustStrength = GetGustMultiplier(worldCoord.x, worldCoord.z, worldCoord.y);
                        if (gustStrength <= 0.2)
                        {
                            break;
                        }
                        Gizmos.color = new Color(gustStrength, gustStrength, gustStrength, gustStrength);
                        Gizmos.DrawSphere(worldCoord, 0.1f);

                    }
                }
            }
        }
    }

    public void SetPreset(PresetEnum newPreset)
    {
        tempPreset = newPreset;
        preset = newPreset;

        switch (preset)
        {
            case PresetEnum.Wool:
                massPerSquareMeter = 0.4f;
                springCoefficient = 70;
                globalDampening = 4;
                internalDampening = 0.002f;
                crossLinkStrength = 0.04f;
                solver = SolverEnum.InternalDamperForce;
                break;

            case PresetEnum.Silk:
                massPerSquareMeter = 0.25f;
                springCoefficient = 80;
                globalDampening = 4;
                internalDampening = 0.002f;
                crossLinkStrength = 0.1f;
                solver = SolverEnum.InternalDamperForce;
                break;

            case PresetEnum.Leather:
                massPerSquareMeter = 3.3f;
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

    public float		massPerSquareMeter		= 1;
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

    public Wind         globalWind;
    
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
        // WIND
        // Draw points
        if (globalWind.debugPreview)
        {
            globalWind.DrawDebugVolume();
        }

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
        float clothArea = GetComponent<ClothFactory>().GetArea();
        float pointArea = clothArea / points.Count;
        float pointMass = massPerSquareMeter * pointArea;

		float dt = Time.deltaTime + remainder;
		int simSteps = (int)Mathf.Floor(dt / simTime);
		remainder = dt - simSteps * simTime;

        globalWind.Update(dt);

        // Apply wind
        foreach (var point in points)
        {
            point.velocity += globalWind.GetForce(point.position.x, point.position.z,point.position.y, pointArea) / pointMass * dt;
        }

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
