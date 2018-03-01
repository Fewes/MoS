using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GondolaMoveScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        transform.position += transform.forward * 1.75f * Time.deltaTime;
        transform.Rotate(Vector3.up, 45.0f * Time.deltaTime);

    }
}
