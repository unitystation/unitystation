using System;
using System.Linq;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;

/// <summary>
/// Behavior for an object which has a different sprite for each direction it is facing and changes
/// facing when Directional tells it to.
/// Copied from OccupiableDirectionalSprite.cs. Comments from that preserved.
///
/// Initial orientation should be set in Directional.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Directional))]
public class DirectionalSprite : NetworkBehaviour
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

	[Tooltip("Sprite renderer on which to render the base sprite")]
	public SpriteRenderer spriteRenderer;

	private Directional directional;

	public void Awake()
	{
		EnsureInit();
	}

	// Only runs in editor - useful for updating the sprite direction
	// when the initial direction is altered via inspector.
	private void OnValidate()
	{
		EnsureInit();
		OnDirectionChanged(directional.InitialOrientation);
	}

	private void EnsureInit()
	{
		if (directional != null || gameObject == null) return;
		directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChanged);
		OnDirectionChanged(directional.CurrentDirection);
	}

	public override void OnStartClient()
	{
		EnsureInit();
		//must invoke this because SyncVar hooks are not called on client init
		OnDirectionChanged(directional.CurrentDirection);
	}

	private void OnDirectionChanged(Orientation newDir)
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
	}
}
