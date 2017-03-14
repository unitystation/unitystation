using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxStars : MonoBehaviour {

	public float speed = 1f;

	public void MoveInDirection(Vector2 dir){
		if (dir == Vector2.right) {
			transform.position = new Vector3(transform.position.x - (dir.x * speed), transform.position.y, transform.position.z);
		}else if (dir == Vector2.left) {
			transform.position = new Vector3(transform.position.x + (Mathf.Abs(dir.x) * speed), transform.position.y, transform.position.z);
		}else if (dir == Vector2.up) {
			transform.position = new Vector3(transform.position.x, transform.position.y - (dir.y * speed), transform.position.z);
		}else if (dir == Vector2.down) {
			transform.position = new Vector3(transform.position.x, transform.position.y + (Mathf.Abs(dir.y) * speed), transform.position.z);
		}
	}
}
