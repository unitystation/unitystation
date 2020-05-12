
using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Handles animation of generic player overlay prefabs which have different sprites for each direction
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerDirectionalOverlay : MonoBehaviour
{
	private SpriteRenderer spriteRenderer;

	[Tooltip("Whether sprite order should be random or sequential")]
	public bool randomOrder = false;
	[Tooltip("Seconds between sprite changes")]
	public float AnimationSpeed = 0.1f;
	[Tooltip("Possible left-facing overlay sprites for this prefab")]
	public Sprite[] leftSprites;
	[Tooltip("Possible right-facing overlay sprites for this prefab")]
	public Sprite[] rightSprites;
	[Tooltip("Possible up-facing overlay sprites for this prefab")]
	public Sprite[] upSprites;
	[Tooltip("Possible down-facing overlay sprites for this prefab")]
	public Sprite[] downSprites;

	private int leftIndex, rightIndex, upIndex, downIndex = 0;
	private Orientation orientation;
	private float animSpriteTime;

	public bool OverlayActive { get; private set; }

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		StopOverlay();
	}

	private void OnDisable()
	{
		if (OverlayActive)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}
	}

	/// <summary>
	/// Display the overlay animation in the specified direction
	/// </summary>
	/// <param name="direction"></param>
	public void StartOverlay(Orientation direction)
	{
		if(spriteRenderer == null) return;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		orientation = direction;
		OverlayActive = true;
		spriteRenderer.enabled = true;
		ChangeOverlaySprite();
	}

	private void ChangeOverlaySprite()
	{
		if (orientation == Orientation.Down) SetSprite(downSprites, ref downIndex);
		else if (orientation == Orientation.Up) SetSprite(upSprites, ref upIndex);
		else if (orientation == Orientation.Left) SetSprite(leftSprites, ref leftIndex);
		else if (orientation == Orientation.Right) SetSprite(rightSprites, ref rightIndex);
	}

	private void SetSprite(Sprite[] sprites, ref int directionalIndex)
	{
		int index;

		if (randomOrder)
		{
			index = Random.Range(0, sprites.Length);
		}
		else
		{
			directionalIndex++;
			if (directionalIndex >= sprites.Length) directionalIndex = 0;
			index = directionalIndex;
		}

		spriteRenderer.sprite = sprites[index];
	}

	/// <summary>
	/// stop displaying the burning animation
	/// </summary>
	public void StopOverlay()
	{
		spriteRenderer.sprite = null;
		spriteRenderer.enabled = false;
		OverlayActive = false;
		leftIndex = rightIndex = upIndex = downIndex = 0;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void UpdateMe()
	{
		if (!OverlayActive) return;
		animSpriteTime += Time.deltaTime;
		if (animSpriteTime > AnimationSpeed)
		{
			animSpriteTime = 0f;
			ChangeOverlaySprite();
		}
	}
}
