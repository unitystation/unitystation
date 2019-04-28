using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
///Behavior for an object which has a different sprite for each direction it is facing. Sprite is changed
/// when matrix rotates.
///
/// Initial orientation is based on the sprite the SpriteRenderer is initially set to.
/// </summary>
public class DirectionalSprite : NetworkBehaviour
{
	/// <summary>
	/// When true, chairs will rotate to their new orientation at the end of matrix rotation. When false
	/// they will rotate to the new orientation at the start of matrix rotation.
	/// </summary>
	private const bool ROTATE_AT_END = true;

	//absolute orientation
	private Orientation orientation;

	public Sprite s_right;
	public Sprite s_down;
	public Sprite s_left;
	public Sprite s_up;

	public SpriteRenderer spriteRenderer;

	private MatrixMove matrixMove;
	// cached registertile on this chair
	private RegisterTile registerTile;

	//when true, sprite will be put in the BuckledOverPlayer layer
	//when it is facing up, so it appears on top of the player.
	[SyncVar(hook=nameof(SyncBuckledOverPlayer))]
	private bool renderBuckledOverPlayer;
	//holds the original layer name if the layer is changed.
	private string originalSpriteLayerName;

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

	public override void OnStartClient()
	{
		//must invoke this because SyncVar hooks are not called on client init
		SyncBuckledOverPlayer(renderBuckledOverPlayer);
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

		EnsureSpriteLayer();
	}

	/// <summary>
	/// Renders this object on top of the player when orientation is Up.
	/// Used for things like chairs which need to be drawn on top of the player
	/// when the player is buckled in when the chair is facing up.
	/// </summary>
	[Server]
	public void RenderBuckledOverPlayerWhenUp(bool renderBuckledOverPlayer)
	{
		this.renderBuckledOverPlayer = renderBuckledOverPlayer;
	}

	//syncvar hook for renderBuckledOverPlayer
	private void SyncBuckledOverPlayer(bool newValue)
	{
		renderBuckledOverPlayer = newValue;
		EnsureSpriteLayer();
	}

	//ensures we are rendering in the correct sprite layer
	private void EnsureSpriteLayer()
	{
		if (orientation == Orientation.Up && renderBuckledOverPlayer)
		{
			//move to the corresponding sprite layer above the player
			originalSpriteLayerName = spriteRenderer.sortingLayerName;
			spriteRenderer.sortingLayerName = "BuckledOverPlayer";
		}
		else
		{
			//restore original layer
			if (originalSpriteLayerName != null)
			{
				spriteRenderer.sortingLayerName = originalSpriteLayerName;
			}
		}
	}
}
