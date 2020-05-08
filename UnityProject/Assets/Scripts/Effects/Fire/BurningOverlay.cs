using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Handles animation of burning overlay prefabs, which are injected into burning objects.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BurningOverlay : MonoBehaviour
{
	[Tooltip("Seconds between sprite changes")]
	public float AnimationSpeed = 0.1f;
	[Tooltip("Possible burning sprites for this prefab")]
	public Sprite[] sprites;

	private bool burn;

	private SpriteRenderer spriteRenderer;

	private float animSpriteTime;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		//wait until we are told to burn
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
	/// start displaying the burning animation
	/// </summary>
	public void Burn()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		spriteRenderer.sprite = sprites[Random.Range(0, sprites.Length)];
		spriteRenderer.enabled = true;
		burn = true;
	}

	/// <summary>
	/// stop the burning animation
	/// </summary>
	public void StopBurning()
	{
		if (spriteRenderer == null) return;
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
			spriteRenderer.sprite = sprites[Random.Range(0, sprites.Length)];
		}
	}
}