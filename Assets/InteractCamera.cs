using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractCamera : MonoBehaviour {

	public Camera mainCam;
	public Camera interactCam;

	void Start(){
		interactCam.orthographicSize = mainCam.orthographicSize;

	}

	void Update(){
		if (interactCam.orthographicSize != mainCam.orthographicSize) {
			interactCam.orthographicSize = mainCam.orthographicSize;
		}
	}
}
