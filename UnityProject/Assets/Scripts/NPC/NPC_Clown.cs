using System.Collections;
using UnityEngine;

namespace NPC
{
	public class NPC_Clown : MonoBehaviour
	{
		public Sprite[] clownSprites;
		private bool isMoving;

		private bool isRight;
		private SpriteRenderer spriteRenderer;

		private void Start()
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
			//Snap to grid
			//FIXME need to figure out the grid and how to round to it
			Vector2 newPos = new Vector2(Mathf.Round(transform.position.x / 100f) * 100f,
				Mathf.Round(transform.position.y / 100f) * 100f);
			transform.position = newPos;
			StartCoroutine(RandMove());
		}

		private void OnDisable()
		{
			StopCoroutine(RandMove());
		}

		private void Update()
		{
			if (!isMoving)
			{
				StartCoroutine(RandMove());
			}
		}

		private IEnumerator RandMove()
		{
			isMoving = true;
			float ranTime = Random.Range(0.2f, 6f);

			yield return new WaitForSeconds(ranTime);

			int ranDir = Random.Range(0, 4);

			if (ranDir == 0)
			{
				//Move Up
				spriteRenderer.sprite = clownSprites[2];
				Vector2 movePos = new Vector2(transform.position.x, transform.position.y + 32f);
				transform.position = movePos;
			}
			else if (ranDir == 1)
			{
				//Move Right
				spriteRenderer.sprite = clownSprites[3];
				Vector2 movePos = new Vector2(transform.position.x + 32f, transform.position.y);
				transform.position = movePos;

				if (!isRight)
				{
					isRight = true;
					Flip();
				}
			}
			else if (ranDir == 2)
			{
				//Move Down
				spriteRenderer.sprite = clownSprites[0];
				Vector2 movePos = new Vector2(transform.position.x, transform.position.y - 32f);
				transform.position = movePos;
			}
			else if (ranDir == 3)
			{
				//Move Left
				spriteRenderer.sprite = clownSprites[3];
				Vector2 movePos = new Vector2(transform.position.x - 32f, transform.position.y);
				transform.position = movePos;

				if (isRight)
				{
					isRight = false;
					Flip();
				}
			}

			float ranPitch = Random.Range(0.5f, 1.5f);
			SoundManager.Play("ClownHonk", 0.3f, ranPitch);

			isMoving = false;
		}

		private void Flip()
		{
			Vector2 newScale = transform.localScale;
			newScale.x = -newScale.x;
			transform.localScale = newScale;
		}
	}
}