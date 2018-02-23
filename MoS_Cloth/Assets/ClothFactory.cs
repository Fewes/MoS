using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ClothFactory : MonoBehaviour
{
	public abstract void InitializeCloth(Transform transform, ref List<VeryLett.ClothPoint> points, ref List<VeryLett.ClothLink> links, ref List<VeryLett.ClothLink> xLinks, ref MeshFilter meshFilter);

    public virtual float GetArea()
    {
        return 1.0f;
    }
}
