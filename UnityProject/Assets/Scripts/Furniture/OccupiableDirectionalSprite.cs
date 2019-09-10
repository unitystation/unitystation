using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

/// <summary>
/// Behavior for an object which has a different sprite for each direction it is facing and changes
/// facing when Directional tells it to. Also can become "occupied" by a player - modifying how it is drawn
/// so that it appears that a player is occupying it.
///
/// Initial orientation should be set in Directional.
/// </summary>
[RequireComponent(typeof(Directional))]
[RequireComponent(typeof(Integrity))]
[ExecuteInEditMode]
public class OccupiableDirectionalSprite : NetworkBehaviour
{
	[Header("Base Sprites")]
	[Tooltip("Base sprite when facing right")]
	[FormerlySerializedAs("s_right")]
	public Sprite Right;
	[Tooltip("Base sprite when facing down")]
	[FormerlySerializedAs("s_down")]
	public Sprite Down;
	[Tooltip("Base sprite when facing left")]
	[FormerlySerializedAs("s_left")]
	public Sprite Left;
	[Tooltip("Base sprite when facing up")]
	[FormerlySerializedAs("s_up")]
	public Sprite Up;

	[Tooltip("sprite renderer on which to render the base sprite")]
	public SpriteRenderer spriteRenderer;

	[Header("Sprites Over Player When Occupied")]
	[FormerlySerializedAs("s_right_front")]
	[Tooltip("Sprite to render in front of player when occupied facing right")]
	public Sprite OccupiedRight;
	[FormerlySerializedAs("s_down_front")]
	[Tooltip("Sprite to render in front of player when occupied facing down")]
	public Sprite OccupiedDown;
	[FormerlySerializedAs("s_left_front")]
	[Tooltip("Sprite to render in front of player when occupied facing left")]
	public Sprite OccupiedLeft;
	[FormerlySerializedAs("s_up_front")]
	[Tooltip("Sprite to render in front of player when occupied facing up")]
	public Sprite OccupiedUp;

	[Tooltip("sprite renderer on which to render the front sprites")]
	public SpriteRenderer spriteRendererFront;

	[SyncVar(hook=nameof(SyncIsOccupied))]
	private bool isOccupied;

	private const string BASE_SPRITE_LAYER_NAME = "Machines";
	private const string FRONT_SPRITE_LAYER_NAME = "OverPlayers";

	private Directional directional;

	public void Awake()
	{
		directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChanged);
		OnDirectionChanged(directional.CurrentDirection);
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
	}

	private void OnWillDestroyServer(DestructionInfo info)
	{
		//release the player
		if (isOccupied)
		{
			var playerMoveAtPosition = MatrixManager.GetAt<PlayerMove>(transform.position.CutToInt(), true)
				?.First(pm => pm.IsBuckled);
			playerMoveAtPosition.Unbuckle();
		}
	}

	public override void OnStartClient()
	{
		//must invoke this because SyncVar hooks are not called on client init
		SyncIsOccupied(isOccupied);
		OnDirectionChanged(directional.CurrentDirection);
	}

	public void OnDirectionChanged(Orientation newDir)
	{
		if (newDir == Orientation.Up)
		{
			spriteRenderer.sprite = Up;
		}
		else if (newDir == Orientation.Down)
		{
			spriteRenderer.sprite = Down;
		}
		else if (newDir == Orientation.Left)
		{
			spriteRenderer.sprite = Left;
		}
		else
		{
			spriteRenderer.sprite = Right;
		}

		UpdateFrontSprite();
		EnsureSpriteLayer();
	}

	// Updates the sprite that's drawn over the occupant when the occupant is buckled in (e.g. the seatbelt)
	private void UpdateFrontSprite()
	{
		if (spriteRendererFront)
		{
			if (isOccupied)
			{
				if (directional.CurrentDirection == Orientation.Up)
				{
					spriteRendererFront.sprite = OccupiedUp;
				}
				else if (directional.CurrentDirection == Orientation.Down)
				{
					spriteRendererFront.sprite = OccupiedDown;
				}
				else if (directional.CurrentDirection == Orientation.Left)
				{
					spriteRendererFront.sprite = OccupiedLeft;
				}
				else
				{
					spriteRendererFront.sprite = OccupiedRight;
				}
			}
			else
				spriteRendererFront.sprite = null;
		}
	}

	/// <summary>
	/// Set whether this object should render itself as if it is occupied or vacant.
	/// </summary>
	[Server]
	public void RenderOccupied(bool renderOccupied)
	{
		SyncIsOccupied(renderOccupied);
	}

	//syncvar hook for isOccupied
	private void SyncIsOccupied(bool newValue)
	{
		isOccupied = newValue;
		UpdateFrontSprite();
		EnsureSpriteLayer();
	}

	//ensures we are rendering in the correct sprite layer
	private void EnsureSpriteLayer()
	{
		if (directional.CurrentDirection == Orientation.Up && isOccupied)
		{
			spriteRenderer.sortingLayerName = FRONT_SPRITE_LAYER_NAME;
		}
		else
		{
			//restore original layer
			if (BASE_SPRITE_LAYER_NAME != null)
			{
				spriteRenderer.sortingLayerName = BASE_SPRITE_LAYER_NAME;
			}
		}
	}

//changes the rendered sprite in editor based on the value set in Directional
#if UNITY_EDITOR
	private void Update()
	{
		if (Application.isEditor && !Application.isPlaying)
		{
			var dir = GetComponent<Directional>().InitialOrientation;
			if (dir == Orientation.Up)
			{
				spriteRenderer.sprite = Up;
			}
			else if (dir == Orientation.Down)
			{
				spriteRenderer.sprite = Down;
			}
			else if (dir == Orientation.Left)
			{
				spriteRenderer.sprite = Left;
			}
			else
			{
				spriteRenderer.sprite = Right;
			}
		}
	}
#endif
}
