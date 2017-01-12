using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayGroup
{
    public class PlayerDirection : MonoBehaviour
    {
        private float nextActionTime = 0.0f;

        void Update()
        {
            if (PlayerManager.LocalPlayerScript != null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 dir = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - PlayerManager.LocalPlayer.transform.position).normalized;
                    float angle = Angle(dir);
                    Debug.Log(angle);
                    //change the facingDirection of player
                    CheckPlayerDirection(angle);
                    return;
                }
                //Wait between actions to mimic traditional(byond) behaviour 
                if (Time.time > nextActionTime)
                {
                    nextActionTime = Time.time + 0.3f;
                    if (PlayerManager.LocalPlayerScript.physicsMove.isMoving)
                    {
                        CheckDirOnMove();
                    }
                }
            }
        }

        //Face player back in direction of move if it changes while moving
        void CheckDirOnMove()
        {
            if (PlayerManager.LocalPlayerScript.physicsMove.MoveDirection != PlayerManager.LocalPlayerScript.playerSprites.currentDirection)
            {
                PlayerManager.LocalPlayerScript.playerSprites.FaceDirection(PlayerManager.LocalPlayerScript.physicsMove.MoveDirection);
            }
        }

        //Set player facing in direction of mouse click
        void CheckPlayerDirection(float angle)
        {
            if (angle >= 315f && angle <= 360f || angle >= 0f && angle <= 45f)
            {
                PlayerManager.LocalPlayerScript.playerSprites.FaceDirection(Vector2.up);
            }

            if (angle > 45f && angle <= 135f)
            {
                PlayerManager.LocalPlayerScript.playerSprites.FaceDirection(Vector2.right);
            }

            if (angle > 135f && angle <= 225f)
            {
                PlayerManager.LocalPlayerScript.playerSprites.FaceDirection(Vector2.down);
            }

            if (angle > 225f && angle < 315f)
            {
                PlayerManager.LocalPlayerScript.playerSprites.FaceDirection(Vector2.left);
            }
        }

        //Calculate the mouse click angle in relation to player(for facingDirection on PlayerSprites)
        float Angle(Vector2 dir)
        {
            if (dir.x < 0)
            {
                return 360 - (Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg * -1);
            }
            else
            {
                return Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            }
        }
    }
}