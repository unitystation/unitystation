using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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

	[Tooltip("Object will disappear automatically after this many seconds have passed. Leave at" +
	         "zero or negative value to prevent this.")]
	public float SecondsUntilDisappear = -1f;

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
			SyncChosenSprite(Random.Range(0, PossibleSprites.Length - 1));
		}
		//set lifetime
		if (SecondsUntilDisappear > 0)
		{
			StartCoroutine(DisappearAfterLifetime());
		}
	}

	public override void OnStartClient()
	{
		SyncChosenSprite(chosenSprite);
	}

	private IEnumerator DisappearAfterLifetime(){
		yield return WaitFor.Seconds(SecondsUntilDisappear);
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();
	}

	private void SyncChosenSprite(int chosenSprite)
	{
		this.chosenSprite = chosenSprite;
		if (PossibleSprites != null && PossibleSprites.Length > 0)
		{
			spriteRenderer.sprite = PossibleSprites[this.chosenSprite];
		}
	}

	/// <summary>
	///attempts to clean this decal, cleaning it if it is cleanable
	/// </summary>
	public void TryClean()
	{
		if (Cleanable)
		{
			PoolManager.PoolNetworkDestroy(gameObject);
		}
	}
}