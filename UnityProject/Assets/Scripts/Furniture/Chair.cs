using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Chair : NetworkBehaviour
{

	public Orientation currentDirection;

	public Vector2 directionOnStart;

	public Sprite s_right;
	public Sprite s_down;
	public Sprite s_left;
	public Sprite s_up;

	public SpriteRenderer spriteRenderer;

	private MatrixMove matrixMove;

	public override void OnStartClient()
	{
		

		base.OnStartClient();
		StartCoroutine(WaitForLoad());
	}

	IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(2f);
		Vector3Int worldPos = Vector3Int.RoundToInt((Vector2)transform.position); //cutting off Z-axis & rounding
		MatrixInfo matrixAtPoint = MatrixManager.AtPoint(worldPos);

		if (matrixAtPoint.MatrixMove != null)
		{
			matrixMove = matrixAtPoint.MatrixMove;
			matrixMove.OnRotate.AddListener(OnRotation);
			ChangeDirection(matrixMove.ClientState.Orientation);
		}

	}

	private void OnDisable()
	{
		if(matrixMove != null)
		{
			matrixMove.OnRotate.RemoveListener(OnRotation);
		}
	}

	public void OnRotation(Orientation before, Orientation next)
	{
		ChangeChairDirection(Orientation.DegreeBetween(before, next));
	}

	void ChangeChairDirection(int degrees)
	{
		for (int i = 0; i < Mathf.Abs(degrees / 90); i++)
		{
			if (degrees < 0)
			{
				ChangeDirection(currentDirection.Previous());
			}
			else
			{
				ChangeDirection(currentDirection.Next());
			}
		}
	}

	void ChangeDirection(Orientation dir)
	{
		if(dir == Orientation.Up)
		{
			spriteRenderer.sprite = s_up;
		}

		if (dir == Orientation.Right)
		{
			spriteRenderer.sprite = s_right;
		}

		if (dir == Orientation.Down)
		{
			spriteRenderer.sprite = s_down;
		}

		if (dir == Orientation.Left)
		{
			spriteRenderer.sprite = s_left;
		}
	}

}
