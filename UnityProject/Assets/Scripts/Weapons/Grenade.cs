using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

/// <summary>
///     shape of explosion that occurs
/// </summary>
public enum ExplosionType
{
	Square, // radius is equal in all directions from center []

	Diamond, // classic SS13 diagonals are reduced and angled <>
	Bomberman, // plus +
	Circle, // Diamond without tip
}

/// <summary>
/// Generic grenade base.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class Grenade : NetworkBehaviour, IInteractable<HandActivate>, IClientSpawn
{

	public Explosion explosionPrefab;

	[TooltipAttribute("If the fuse is precise or has a degree of error equal to fuselength / 4")]
	public bool unstableFuse = false;
	[TooltipAttribute("fuse timer in seconds")]
	public float fuseLength = 3;

	[Tooltip("Used for animation")]
	public SpriteHandler spriteHandler;
	// Zero and one reserved for hands
	private const int LOCKED_SPRITE = 2;
	private const int ARMED_SPRITE = 3;


	//whether this object has exploded
	private bool hasExploded;
	//this object's registerObject
	[SyncVar(hook = nameof(UpdateTimer))]
	private bool timerRunning = false;
	private RegisterItem registerItem;

	private ObjectBehaviour objectBehaviour;

	private void Start()
	{
		registerItem = GetComponent<RegisterItem>();
		objectBehaviour = GetComponent<ObjectBehaviour>();

		// Set grenade to locked state by default
		UpdateSprite(LOCKED_SPRITE);
	}

	public void OnSpawnClient(ClientSpawnInfo info)
	{
		// Set grenade to locked state by default
		UpdateSprite(LOCKED_SPRITE);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		StartCoroutine(TimeExplode(interaction.Performer));
	}

	private IEnumerator TimeExplode(GameObject originator)
	{
		if (!timerRunning)
		{
			timerRunning = true;
			PlayPinSFX(originator.transform.position);
			if (unstableFuse)
			{
				float fuseVariation = fuseLength / 4;
				fuseLength = Random.Range(fuseLength - fuseVariation, fuseLength + fuseVariation);
			}

			yield return WaitFor.Seconds(fuseLength);
			Explode("explosion");
		}
	}

	private void UpdateSprite(int sprite)
	{
		// Update sprite in game
		spriteHandler?.ChangeSprite(sprite);
	}

	/// <summary>
	/// This coroutines make sure that sprite in hands is animated
	/// TODO: replace this with more general aproach for animated icons
	/// </summary>
	/// <returns></returns>
	private IEnumerator AnimateSpriteInHands()
	{
		while (timerRunning && !hasExploded)
		{
			if (UIManager.Hands.CurrentSlot != null)
			{
				// UIManager doesn't update held item sprites automatically
				if (UIManager.Hands.CurrentSlot.Item == gameObject)
				{
					UIManager.Hands.CurrentSlot.UpdateImage(gameObject);
				}
			}

			yield return null;
		}

	}

	public void Explode(string damagedBy)
	{
		if (hasExploded)
		{
			return;
		}
		hasExploded = true;

		if (isServer)
		{
			// Explosion here
			var explosionGO = Instantiate(explosionPrefab, registerItem.Matrix.transform);
			explosionGO.transform.position = objectBehaviour.AssumedWorldPositionServer();
			explosionGO.Explode(registerItem.Matrix);

			Despawn.ServerSingle(gameObject);
		}
	}

	private void PlayPinSFX(Vector3 position)
	{
		SoundManager.PlayNetworkedAtPos("armbomb", position);
	}

	private void UpdateTimer(bool timerRunning)
	{
		this.timerRunning = timerRunning;

		if (timerRunning)
		{
			// Start playing arm animation
			UpdateSprite(ARMED_SPRITE);
			// Update grenade icon in hands
			StartCoroutine(AnimateSpriteInHands());
		}
		else
		{
			// We somehow deactivated bomb
			UpdateSprite(LOCKED_SPRITE);
		}

	}

#if UNITY_EDITOR
	/// <summary>
	/// Used only for debug in editor
	/// </summary>
	[ContextMenu("Pull a pin")]
	private void PullPin()
	{
		StartCoroutine(TimeExplode(gameObject));
	}
#endif
}