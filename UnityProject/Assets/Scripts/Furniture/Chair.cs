using System;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Note that chair determines its initial orientation based on the sprite it is set to
/// </summary>
public class Chair : MonoBehaviour
{
	private Orientation orientation;

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

	/// <summary>
	/// Figure out initial direction based on which sprite was selected.
	/// </summary>
	private void InitDirection()
	{
		if (spriteRenderer.sprite == s_right)
		{
			orientation = Orientation.Right;
		}
		else if (spriteRenderer.sprite == s_down)
		{
			orientation = Orientation.Down;
		}
		else if (spriteRenderer.sprite == s_left)
		{
			orientation = Orientation.Left;
		}
		else
		{
			orientation = Orientation.Up;
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

	public void OnRotation(RotationOffset fromCurrent)
	{
		orientation = orientation.Rotate(fromCurrent);
		if (orientation == Orientation.Up)
		{
			spriteRenderer.sprite = s_up;
		}
		else if (orientation == Orientation.Down)
		{
			spriteRenderer.sprite = s_down;
		}
		else if (orientation == Orientation.Left)
		{
			spriteRenderer.sprite = s_left;
		}
		else
		{
			spriteRenderer.sprite = s_right;
		}
	}
}
