using System;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Chair : MonoBehaviour
{
	public Orientation currentDirection;

	public Sprite s_right;
	public Sprite s_down;
	public Sprite s_left;
	public Sprite s_up;

	public SpriteRenderer spriteRenderer;

	private MatrixMove matrixMove;

	public void Start()
	{
		InitDirection();
		matrixMove = transform.root.GetComponent<MatrixMove>();
		if (matrixMove != null)
		{
			SetUpRotationListener();
			StartCoroutine(WaitForInit());
		}
	}

	private void InitDirection()
	{
		if (spriteRenderer.sprite == s_right)
		{
			currentDirection = Orientation.Right;
		}
		else if (spriteRenderer.sprite == s_up)
		{
			currentDirection = Orientation.Up;
		}
		else if (spriteRenderer.sprite == s_left)
		{
			currentDirection = Orientation.Left;
		}
		else
		{
			currentDirection = Orientation.Down;
		}
	}

	IEnumerator WaitForInit()
	{
		while (!matrixMove.StateInit)
		{
			yield return YieldHelper.EndOfFrame;
		}
	}

	void SetUpRotationListener()
	{
		matrixMove.OnRotate.AddListener(OnRotation);
	}

	private void OnDisable()
	{
		if (matrixMove != null)
		{
			matrixMove.OnRotate.RemoveListener(OnRotation);
		}
	}

	public void OnRotation(Orientation before, Orientation next)
	{
		ChangeSprite(Orientation.DegreeBetween(before, next));
	}

	void ChangeSprite(float degrees)
	{
		for (int i = 0; i < Math.Abs(degrees / 90); i++)
		{
			if (degrees < 0)
			{
				currentDirection = currentDirection.Previous();
			}
			else
			{
				currentDirection = currentDirection.Next();
			}
		}

		if (currentDirection == Orientation.Up)
		{
			spriteRenderer.sprite = s_up;
		}
		else if (currentDirection == Orientation.Right)
		{
			spriteRenderer.sprite = s_right;
		}
		else if (currentDirection == Orientation.Left)
		{
			spriteRenderer.sprite = s_left;
		}
		else
		{
			spriteRenderer.sprite = s_down;
		}
	}
}
