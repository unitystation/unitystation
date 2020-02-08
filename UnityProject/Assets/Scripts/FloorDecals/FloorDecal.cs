using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Represents some decal that goes on the floor and can potentially be cleaned up by
/// janitorial actions. Decal can have random variations in its sprite among other
/// capabilities.
/// </summary>
[RequireComponent(typeof(CustomNetTransform))]
public class FloorDecal : NetworkBehaviour
{
	/// <summary>
	/// Whether this decal can be cleaned up by janitorial actions like mopping.
	/// </summary>
	[Tooltip("Whether this decal can be cleaned up by janitorial actions like mopping.")]
	public bool Cleanable = true;

	public bool CanDryUp = false;

	[Tooltip("Possible appearances of this decal. One will randomly be chosen when the decal appears." +
	         " This can be left empty, in which case the prefab's sprite renderer sprite will " +
	         "be used.")]
	public Sprite[] PossibleSprites;

	[SyncVar(hook=nameof(SyncChosenSprite))]
	private int chosenSprite;

	private SpriteRenderer spriteRenderer;

	private void Awake()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
	}

	public override void OnStartServer()
	{
		//randomly pick if there are options
		if (PossibleSprites != null && PossibleSprites.Length > 0)
		{
			chosenSprite = Random.Range(0, PossibleSprites.Length);
		}
	}

	public override void OnStartClient()
	{
		SyncChosenSprite(chosenSprite, chosenSprite);
	}

	private void SyncChosenSprite(int _oldSprite, int _chosenSprite)
	{
		chosenSprite = _chosenSprite;
		if (PossibleSprites != null && PossibleSprites.Length > 0)
		{
			spriteRenderer.sprite = PossibleSprites[chosenSprite];
		}
	}

	/// <summary>
	///attempts to clean this decal, cleaning it if it is cleanable
	/// </summary>
	public void TryClean()
	{
		if (Cleanable)
		{
			Despawn.ServerSingle(gameObject);
		}
	}
}
