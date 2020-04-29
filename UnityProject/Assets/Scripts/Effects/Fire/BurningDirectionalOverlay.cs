
using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Handles animation of burning overlay prefabs which have different sprites for each direction
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BurningDirectionalOverlay : MonoBehaviour
{
	[Tooltip("Seconds between sprite changes")]
	public float AnimationSpeed = 0.1f;
	[Tooltip("Possible left-facing burning sprites for this prefab")]
	public Sprite[] leftSprites;
	[Tooltip("Possible right-facing burning sprites for this prefab")]
	public Sprite[] rightSprites;
	[Tooltip("Possible up-facing burning sprites for this prefab")]
	public Sprite[] upSprites;
	[Tooltip("Possible down-facing burning sprites for this prefab")]
	public Sprite[] downSprites;

	private bool burn;
	private Orientation orientation;
	private SpriteRenderer spriteRenderer;
	private float animSpriteTime;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		StopBurning();
	}

	private void OnDisable()
	{
		if (burn)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}
	}

	/// <summary>
	/// Display the burning animation in the specified direction
	/// </summary>
	/// <param name="direction"></param>
	public void Burn(Orientation direction)
	{
		if(spriteRenderer == null) return;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		orientation = direction;
		burn = true;
		spriteRenderer.enabled = true;
		ChangeBurningSprite();
	}

	private void ChangeBurningSprite()
	{
		if (orientation == Orientation.Down)
		{
			spriteRenderer.sprite = downSprites[Random.Range(0, downSprites.Length)];
		}
		else if (orientation == Orientation.Up)
		{
			spriteRenderer.sprite = upSprites[Random.Range(0, upSprites.Length)];
		}
		else if (orientation == Orientation.Left)
		{
			spriteRenderer.sprite = leftSprites[Random.Range(0, leftSprites.Length)];
		}
		else if (orientation == Orientation.Right)
		{
			spriteRenderer.sprite = rightSprites[Random.Range(0, rightSprites.Length)];
		}
	}

	/// <summary>
	/// stop displaying the burning animation
	/// </summary>
	public void StopBurning()
	{
		spriteRenderer.sprite = null;
		spriteRenderer.enabled = false;
		burn = false;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void UpdateMe()
	{
		if (!burn) return;
		animSpriteTime += Time.deltaTime;
		if (animSpriteTime > AnimationSpeed)
		{
			animSpriteTime = 0f;
			ChangeBurningSprite();
		}
	}
}
