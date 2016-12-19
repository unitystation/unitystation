using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayGroup
{
	public class PlayerCollisions : MonoBehaviour
	{
		private PlayerScript playerScript;
		// Use this for initialization
		void Awake ()
		{
			playerScript = gameObject.GetComponent<PlayerScript> ();
		}

		void OnTriggerStay2D (Collider2D coll)
		{
			//FIXME: I experimented for a long while trying to figure out how to move someone elses
			//FIXME: rigidbody over PUN. Turns out that the owners photonview will keep putting the
			//FIXME: object back to where it believes it should be, making it impossible to push it or move it
			//FIXME: Same goes for transforms. Need to research a solution


//			if (coll.gameObject.layer == 8) { 
//				Debug.Log (gameObject.name + " CollidedWith: " + coll.gameObject.name);
//				var dir = coll.gameObject.transform.position - transform.position;
//				PlayerScript otherPlayerScript = coll.gameObject.GetComponent<PlayerScript> ();
//				if (otherPlayerScript != null) {
//					if (!otherPlayerScript.isMine) {
//						Debug.Log ("PlayerScript Found on: " + otherPlayerScript.gameObject.name);
//						ShuffleOtherPlayer (dir, otherPlayerScript);
//					}
//				}
//			}
		}

		//currently under experimentation:
		void ShuffleOtherPlayer (Vector3 dirToTarget, PlayerScript otherPlayer) //move the other player from the tile you are trying to occupy
		{
			//Checks to see if should move the player and in what direction
			if (playerScript.playerSprites.currentDirection == Vector2.right && dirToTarget.x < 0f && dirToTarget.x > -3f) {
				Vector3 moveTargetPos = RoundPos (otherPlayer.transform.position);
				moveTargetPos.x -= 1f; //Move him left
				otherPlayer.physicsMove.ManualMoveToTarget(moveTargetPos);
				Debug.Log ("Shuffle the other player left");
				return;
			}
			if (playerScript.playerSprites.currentDirection == Vector2.left && dirToTarget.x > 0f && dirToTarget.x < 3f) {
				Vector3 moveTargetPos = RoundPos (otherPlayer.transform.position);
				moveTargetPos.x += 1f; //Move him right
				otherPlayer.physicsMove.ManualMoveToTarget(moveTargetPos);
				Debug.Log ("Shuffle the other player right");
				return;
			}
			if (playerScript.playerSprites.currentDirection == Vector2.down && dirToTarget.y > 0f && dirToTarget.y < 3f) {
				Vector3 moveTargetPos = RoundPos (otherPlayer.transform.position);
				moveTargetPos.y += 1f; //Move him up
				otherPlayer.physicsMove.ManualMoveToTarget(moveTargetPos);
				return;
				Debug.Log ("Shuffle the other player up");
			}
			if (playerScript.playerSprites.currentDirection == Vector2.up && dirToTarget.y < 0f && dirToTarget.y > -3f) {
				Debug.Log ("Shuffle the other player down");
				Vector3 moveTargetPos = RoundPos (otherPlayer.transform.position);
				moveTargetPos.y -= 1f; //Move him down
				otherPlayer.physicsMove.ManualMoveToTarget(moveTargetPos);
				return;
			}
		}

		private Vector3 RoundPos (Vector3 pos)
		{
			pos.x = Mathf.Round (pos.x);
			pos.y = Mathf.Round (pos.y);
			pos.z = 0f;
			return pos;
		}
	}
}