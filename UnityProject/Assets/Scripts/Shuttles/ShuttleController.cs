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
	public float rotSpeed = 35;
	public KeyCode startKey = KeyCode.G;
	public KeyCode leftKey = KeyCode.Keypad4;
	public KeyCode rightKey = KeyCode.Keypad6;

	void Update(){
		if(Input.GetKeyDown(startKey)){
			doFlyingThing = !doFlyingThing;
		}
		if(Input.GetKeyDown(KeyCode.KeypadPlus)){
			speed++;
		}
		if(Input.GetKeyDown(KeyCode.KeypadMinus)){
			speed--;
		}

		if(Input.GetKey(leftKey)){
			transform.Rotate(Vector3.forward * rotSpeed * Time.deltaTime);
		}
		if(Input.GetKey(rightKey)){
			transform.Rotate(Vector3.back * rotSpeed * Time.deltaTime);
		}
		

		if(doFlyingThing){
			transform.Translate(flyingDirection * speed * Time.deltaTime);
		}
	}
}
