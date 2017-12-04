using UnityEngine;
using System.Collections;
using PlayGroup;
using UnityEngine.Networking;

namespace NPC
{
    public class RandomMove : NetworkBehaviour
    {
        private bool isRight = false;
        public float speed = 6f;
        private Vector3 currentPosition, targetPosition, currentDirection;

        private HealthBehaviour healthBehaviour;

        void Start()
        {
            healthBehaviour = GetComponent<HealthBehaviour>();
            targetPosition = transform.position;
            currentPosition = targetPosition;
        }

        void Update()
        {
            if (NetworkServer.active && !healthBehaviour.IsDead)
            {
                Move();
            }
        }

        public override void OnStartServer()
        {
            if (isServer)
            {
                StartCoroutine(RandMove());
            }
            base.OnStartServer();
        }

        [ClientRpc]
        void RpcFlip()
        {
            Vector2 newScale = transform.localScale;
            newScale.x = -newScale.x;
            transform.localScale = newScale;
        }

        void OnDisable()
        {
            StopCoroutine(RandMove());
        }

        void OnTriggerExit2D(Collider2D coll)
        {
            //Players layer
            if (coll.gameObject.layer == 8)
            {
                //player stopped pushing
            }
        }

        //COROUTINES
        IEnumerator RandMove()
        {
            float ranTime = Random.Range(2f, 10f);
            yield return new WaitForSeconds(ranTime);

            int ranDir = Random.Range(0, 4);

            if (ranDir == 0)
            {
                //Move Up
                TryToMove(Vector3.up);


            }
            else if (ranDir == 1)
            {
                //Move Right
                TryToMove(Vector3.right);
                if (!isRight)
                {
                    isRight = true;
                    RpcFlip();
                }

            }
            else if (ranDir == 2)
            {
                //Move Down
                TryToMove(Vector3.down);

            }
            else if (ranDir == 3)
            {
                //Move Left
                TryToMove(Vector3.left);

                if (isRight)
                {
                    isRight = false;
                    RpcFlip();
                }
            }

            yield return new WaitForSeconds(0.2f);

            StartCoroutine(RandMove());
        }

        void Move()
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            if (targetPosition == transform.position)
            {
                currentPosition = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), transform.position.z);
            }
        }

        private bool TryToMove(Vector3 direction)
        {
            var horizontal = Vector3.Scale(direction, Vector3.right);
            var vertical = Vector3.Scale(direction, Vector3.up);

            if (Matrix.Matrix.At(currentPosition + direction).IsPassable())
            {
                if ((Matrix.Matrix.At(currentPosition + horizontal).IsPassable() ||
                                Matrix.Matrix.At(currentPosition + vertical).IsPassable()))
                {
                    targetPosition = currentPosition + direction;
                    targetPosition.z = transform.position.z;
                    return true;
                }
            }
            else if (horizontal != Vector3.zero && vertical != Vector3.zero)
            {
                if (Matrix.Matrix.At(currentPosition + horizontal).IsPassable())
                {
                    targetPosition = currentPosition + horizontal;
                    targetPosition.z = transform.position.z;
                    return true;
                }
                else if (Matrix.Matrix.At(currentPosition + vertical).IsPassable())
                {
                    targetPosition = currentPosition + vertical;
                    targetPosition.z = transform.position.z;
                    return true;
                }
            }
            return false;
        }
    }
}