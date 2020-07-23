using System.Collections;
using System;
using UnityEngine;
using Mirror;

/// <summary>
/// Generic grenade base.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class FireLemon : NetworkBehaviour, IPredictedInteractable<HandActivate>, IServerDespawn
{
	[SerializeField]
	[Tooltip("Explosion prefab")]
	private Explosion explosionPrefab;

	[SerializeField]
	[TooltipAttribute("If the fuse is precise or has a degree of error equal to fuselength / 4")]
	private bool unstableFuse = false;

	[SerializeField]
	[TooltipAttribute("fuse timer in seconds")]
	private float fuseLength = 3;

	[SerializeField]
	[TooltipAttribute("Damage at epicenter of explosion if potency is 100.")]
	private int maxDamage = 125;

	[SerializeField]
	[TooltipAttribute("Radius of explosion of explosion if potency is 100.")]
	private float maxRadius = 5f;

	[SerializeField]
	[Tooltip("SpriteHandler used for blinking animation")]
	private SpriteHandler spriteHandler;

	[SerializeField]
	[Tooltip("Used to override the potency values of the plant data")]
	private int lemonPotencyOverride = 0;

	// Zero and one sprites reserved for left and right hands
	private const int LOCKED_SPRITE = 2;
	private const int ARMED_SPRITE = 3;

	private GrownFood grownFood;

	private int lemonPotency;

	private float finalDamage;

	private float finalRadius;

	///Getting Grownfood so we can get the potency of the plant, and calculates damage/radius.
	private void Awake()
	{
		grownFood = GetComponent<GrownFood>();
	}

	//whether this object has exploded
	private bool hasExploded;

	// is timer finished or was interupted?
	private bool timerRunning = false;

	//this object's registerObject
	private RegisterItem registerItem;
	private ObjectBehaviour objectBehaviour;

	private void Start()
	{
		registerItem = GetComponent<RegisterItem>();
		objectBehaviour = GetComponent<ObjectBehaviour>();

		// Set grenade to locked state by default
		UpdateSprite(LOCKED_SPRITE);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		// Set grenade to locked state by default
		UpdateSprite(LOCKED_SPRITE);
		// Reset grenade timer
		timerRunning = false;
		UpdateTimer(timerRunning);
		hasExploded = false;
	}

	public void ClientPredictInteraction(HandActivate interaction)
	{
		// Toggle the throw action after activation
		UIManager.Action.Throw();
	}

	public void ServerRollbackClient(HandActivate interaction)
	{
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (timerRunning)
			return;

		// Toggle the throw action after activation
		if (interaction.Performer == PlayerManager.LocalPlayer)
		{
			UIManager.Action.Throw();
		}

		// Start timer
		StartCoroutine(TimeExplode(interaction.Performer));
	}

	private IEnumerator TimeExplode(GameObject originator)
	{
		if (!timerRunning)
		{
			timerRunning = true;
			UpdateTimer(timerRunning);
			PlayPinSFX(originator.transform.position);

			if (unstableFuse)
			{
				float fuseVariation = fuseLength / 4;
				fuseLength = UnityEngine.Random.Range(fuseLength - fuseVariation, fuseLength + fuseVariation);
			}

			yield return WaitFor.Seconds(fuseLength);

			// Is timer still running?
			if (timerRunning)
				Explode();
		}
	}

	private void UpdateSprite(int sprite)
	{
		// Update sprite in game
		spriteHandler?.ChangeSprite(sprite);
	}

	public void Explode()
	{
		if (hasExploded)
		{
			return;
		}

		hasExploded = true;

		if (!CustomNetworkManager.IsServer)
		{
			return;
		}

		if (lemonPotencyOverride == 0)
		{
			lemonPotency = grownFood.GetPlantData().Potency;
		}
		else
		{
			lemonPotency = lemonPotencyOverride;
		}

		finalDamage = maxDamage * (lemonPotency / 100f);
		finalRadius = Convert.ToSingle(Math.Ceiling(maxRadius * (lemonPotency / 100f)));

		// Get data from grenade before despawning
		var explosionMatrix = registerItem.Matrix;
		var worldPos = objectBehaviour.AssumedWorldPositionServer();

		// Despawn grenade
		Despawn.ServerSingle(gameObject);

		// Explosion here
		var explosionGO = Instantiate(explosionPrefab, explosionMatrix.transform);
		explosionGO.transform.position = worldPos;
		explosionGO.SetExplosionData(Mathf.RoundToInt(finalDamage), finalRadius);
		explosionGO.Explode(explosionMatrix);
	}

	private void PlayPinSFX(Vector3 position)
	{
		SoundManager.PlayNetworkedAtPos("sizzle", position, sourceObj: gameObject);
	}

	private void UpdateTimer(bool timerRunning)
	{
		this.timerRunning = timerRunning;

		if (timerRunning)
		{
			// Start playing arm animation
			UpdateSprite(ARMED_SPRITE);
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