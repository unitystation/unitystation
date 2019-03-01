using System;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Note that chair determines its initial orientation based on the sprite it is set to
/// </summary>
public class Chair : MonoBehaviour
{
	/// <summary>
	/// When true, chairs will rotate to their new orientation at the end of matrix rotation. When false
	/// they will rotate to the new orientation at the start of matrix rotation.
	/// </summary>
	private const bool ROTATE_AT_END = true;

	private Orientation orientation;

	public Sprite s_right;
	public Sprite s_down;
	public Sprite s_left;
	public Sprite s_up;

	public SpriteRenderer spriteRenderer;

	private MatrixMove matrixMove;
	// cached registertile on this chair
	private RegisterTile registerTile;

	public void Start()
	{
		InitDirection();
		matrixMove = transform.root.GetComponent<MatrixMove>();
		var registerTile = GetComponent<RegisterTile>();
		if (ROTATE_AT_END)
		{
			registerTile.OnRotateEnd.AddListener(OnRotate);
		}
		else
		{
			registerTile.OnRotateStart.AddListener(OnRotate);
		}
		if (matrixMove != null)
		{
			//TODO: Is this still needed?
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
		while (!matrixMove.ReceivedInitialRotation)
		{
			yield return YieldHelper.EndOfFrame;
		}
	}

	private void OnDisable()
	{
		if (registerTile != null)
		{
			if (ROTATE_AT_END)
			{
				registerTile.OnRotateEnd.RemoveListener(OnRotate);
			}
			else
			{
				registerTile.OnRotateStart.RemoveListener(OnRotate);
			}
		}
	}

	public void OnRotate(RotationOffset fromCurrent, bool isInitialRotation)
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
