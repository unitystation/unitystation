using UnityEngine;
using System.Collections;

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
		matrixMove = transform.root.GetComponent<MatrixMove>();
		if (matrixMove != null)
		{
			SetUpRotationListener();
			StartCoroutine(WaitForInit());
		}
	}

	IEnumerator WaitForInit()
	{
		while (!matrixMove.StateInit)
		{
			yield return YieldHelper.EndOfFrame;
		}
		ChangeSprite(matrixMove.ClientState.Orientation.Vector);
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
		ChangeSprite(next.Vector);
	}

	void ChangeSprite(Vector2 dir)
	{
		if (dir == Vector2.up)
		{
			spriteRenderer.sprite = s_up;
		}

		if (dir == Vector2.right)
		{
			spriteRenderer.sprite = s_right;
		}

		if (dir == Vector2.down)
		{
			spriteRenderer.sprite = s_down;
		}

		if (dir == Vector2.left)
		{
			spriteRenderer.sprite = s_left;
		}
	}
}
