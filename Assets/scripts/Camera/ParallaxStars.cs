using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxStars : MonoBehaviour {

	public float speed = 1f;

	public void MoveInDirection(Vector2 dir) {


        transform.position -= new Vector3(dir.x, dir.y) * speed; 

        
	}
}
