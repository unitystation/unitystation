using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayGroup
{
	public class PlayerDirection : MonoBehaviour
	{
		private float nextActionTime = 0.0f;
		private PlayerScript playerScript;

		void Start()
		{
			playerScript = GetComponent<PlayerScript>();
		}

		void Update()
		{
			if (playerScript != null) {
				// Check if the mouse is hovering over an UI element.
				// Because we don't want to rotate when we're just pressing UI things.
				if (Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) {
					Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - playerScript.transform.position).normalized;
					float angle = Angle(dir);
//                    Debug.Log(angle);
					//change the facingDirection of player
					CheckPlayerDirection(angle);
					return;
				}
				//Wait between actions to mimic traditional(byond) behaviour 
				if (Time.time > nextActionTime) {
					nextActionTime = Time.time + 0.3f;
					if (playerScript.physicsMove.isMoving) {
						CheckDirOnMove();
					}
				}
			}
		}

		//Face player back in direction of move if it changes while moving
		void CheckDirOnMove()
		{
			if (!playerScript.isVersion2) {
				if (playerScript.physicsMove.MoveDirection != playerScript.playerSprites.currentDirection) {
					playerScript.playerSprites.FaceDirection(playerScript.physicsMove.MoveDirection);
				}
			} else {
			//TODO PlayerMove equivalent
				Debug.Log("TODO: PlayerMove equivalent on CheckDirOnMove");
			}
		}

		//Set player facing in direction of mouse click
		void CheckPlayerDirection(float angle)
		{
			if (angle >= 315f && angle <= 360f || angle >= 0f && angle <= 45f) {
				playerScript.playerSprites.FaceDirection(Vector2.up);
			}

			if (angle > 45f && angle <= 135f) {
				playerScript.playerSprites.FaceDirection(Vector2.right);
			}

			if (angle > 135f && angle <= 225f) {
				playerScript.playerSprites.FaceDirection(Vector2.down);
			}

			if (angle > 225f && angle < 315f) {
				playerScript.playerSprites.FaceDirection(Vector2.left);
			}
		}

		//Calculate the mouse click angle in relation to player(for facingDirection on PlayerSprites)
		float Angle(Vector2 dir)
		{
			if (dir.x < 0) {
				return 360 - (Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg * -1);
			} else {
				return Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
			}
		}
	}
}