using System.Collections;
using Tilemaps;
using Tilemaps.Scripts;
using UnityEngine;
using UnityEngine.Networking;

namespace NPC
{
	public class RandomMove : NetworkBehaviour
	{
		private HealthBehaviour _healthBehaviour;
		private Matrix _matrix;
		private Vector3Int currentPosition, targetPosition, currentDirection;
		private bool isRight;
		public float speed = 6f;

		private void Start()
		{
			_healthBehaviour = GetComponent<HealthBehaviour>();
			targetPosition = Vector3Int.RoundToInt(transform.position);
			currentPosition = targetPosition;
		}

		private void Update()
		{
			if (NetworkServer.active && !_healthBehaviour.IsDead)
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
		private void RpcFlip()
		{
			Vector2 newScale = transform.localScale;
			newScale.x = -newScale.x;
			transform.localScale = newScale;
		}

		private void OnDisable()
		{
			StopCoroutine(RandMove());
		}

		private void OnTriggerExit2D(Collider2D coll)
		{
			//Players layer
			if (coll.gameObject.layer == 8)
			{
				//player stopped pushing
			}
		}

		//COROUTINES
		private IEnumerator RandMove()
		{
			float ranTime = Random.Range(2f, 10f);
			yield return new WaitForSeconds(ranTime);

			int ranDir = Random.Range(0, 4);

			if (ranDir == 0)
			{
				//Move Up
				TryToMove(Vector3Int.up);
			}
			else if (ranDir == 1)
			{
				//Move Right
				TryToMove(Vector3Int.right);
				if (!isRight)
				{
					isRight = true;
					RpcFlip();
				}
			}
			else if (ranDir == 2)
			{
				//Move Down
				TryToMove(Vector3Int.down);
			}
			else if (ranDir == 3)
			{
				//Move Left
				TryToMove(Vector3Int.left);

				if (isRight)
				{
					isRight = false;
					RpcFlip();
				}
			}

			yield return new WaitForSeconds(0.2f);

			StartCoroutine(RandMove());
		}

		private void Move()
		{
			transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
			if (targetPosition == transform.position)
			{
				currentPosition = Vector3Int.RoundToInt(transform.position);
			}
		}

		private bool TryToMove(Vector3Int direction)
		{
			Vector3Int horizontal = Vector3Int.Scale(direction, Vector3Int.right);
			Vector3Int vertical = Vector3Int.Scale(direction, Vector3Int.up);

			if (_matrix.IsPassableAt(currentPosition + direction))
			{
				if (_matrix.IsPassableAt(currentPosition + horizontal) ||
				    _matrix.IsPassableAt(currentPosition + vertical))
				{
					targetPosition = currentPosition + direction;
					return true;
				}
			}

			if (_matrix.IsPassableAt(currentPosition + direction))
			{
				if (_matrix.IsPassableAt(currentPosition + horizontal) ||
				    _matrix.IsPassableAt(currentPosition + vertical))
				{
					targetPosition = currentPosition + direction;
					return true;
				}
			}
			else if (horizontal != Vector3.zero && vertical != Vector3.zero)
			{
				if (_matrix.IsPassableAt(currentPosition + horizontal))
				{
					targetPosition = currentPosition + horizontal;
					return true;
				}
				if (_matrix.IsPassableAt(currentPosition + vertical))
				{
					targetPosition = currentPosition + vertical;
					return true;
				}
			}
			return false;
		}
	}
}