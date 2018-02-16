using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using Tilemaps;

public class ShuttleController : MonoBehaviour {

	//TEST MODE
	bool doFlyingThing = false;
	public Vector2 flyingDirection;
	public float speed;

	void Update(){
		if(Input.GetKeyDown(KeyCode.G) && !doFlyingThing){
			doFlyingThing = true;
		}

		if(doFlyingThing){
			transform.Translate(flyingDirection * speed * Time.deltaTime);
		}
	}
}
