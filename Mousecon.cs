using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mousecon : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Debug.Log(Input.GetAxis("Mouse X"));
	}
}
