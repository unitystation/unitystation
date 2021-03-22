using System;
using System.Collections;
using Light2D;
using Lighting;
using Mirror;
using ScriptableObjects;
using UnityEngine;
using Systems.Electricity;
using Random = UnityEngine.Random;
using Objects.Construction;

public class LightSource : ObjectTrigger, ICheckedInteractable<HandApply>, IAPCPowered, IServerLifecycle, ISetMultitoolSlave
{
	public Color ONColour;
	public Color EmergencyColour;

	public LightSwitchV2 relatedLightSwitch;

	[SerializeField]
	private LightMountState InitialState = LightMountState.On;

	[SyncVar(hook =nameof(SyncLightState))]
	private LightMountState mState;

	[Header("Generates itself if this is null:")]
	public GameObject mLightRendererObject;
	[SerializeField]
	private bool isWithoutSwitch = true;
	public bool IsWithoutSwitch => isWithoutSwitch;
	private bool switchState = true;
	private PowerStates powerState;
	
	[SerializeField] private SpriteHandler spriteHandler;
	[SerializeField] private SpriteRenderer spriteRendererLightOn;
	private LightSprite lightSprite;
	[SerializeField] private EmergencyLightAnimator emergencyLightAnimator = default;
	[SerializeField] private Integrity integrity = default;
	[SerializeField] private Directional directional;

	[SerializeField] private BoxCollider2D boxColl = null;
	[SerializeField] private Vector4 collDownSetting = Vector4.zero;
	[SerializeField] private Vector4 collRightSetting = Vector4.zero;
	[SerializeField] private Vector4 collUpSetting = Vector4.zero;
	[SerializeField] private Vector4 collLeftSetting = Vector4.zero;
	[SerializeField] private SpritesDirectional spritesStateOnEffect = null;
	[SerializeField] private SOLightMountStatesMachine mountStatesMachine = null;
	private SOLightMountState currentState;
	private RegisterTile registerTile;
	private LightFixtureConstruction construction;

	private ItemTrait traitRequired;
	private GameObject itemInMount;

	private GameObject currentSparkEffect;

	public float integrityThreshBar { get; private set; }

	[SerializeField]
	private MultitoolConnectionType conType = MultitoolConnectionType.LightSwitch;
	public MultitoolConnectionType ConType  => conType;

	public void SetMaster(ISetMultitoolMaster Imaster)
	{
		var lightSwitch = (Imaster as Component)?.gameObject.GetComponent<LightSwitchV2>();
		if (lightSwitch != relatedLightSwitch)
		{
			SubscribeToSwitchEvent(lightSwitch);
		}
	}

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		construction = GetComponent<LightFixtureConstruction>();
		if (mLightRendererObject == null)
		{
			mLightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, new Color(0, 0, 0, 0), 12);
		}
		lightSprite = mLightRendererObject.GetComponent<LightSprite>();
		if (!isWithoutSwitch)
			switchState = InitialState == LightMountState.On;
		ChangeCurrentState(InitialState);
		traitRequired = currentState.TraitRequired;
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		if (!info.SpawnItems)
		{
			mState = LightMountState.MissingBulb;
		}
	}

	private void OnDirectionChange(Orientation newDir)
	{
		SetSprites();
	}

	private void OnEnable()
	{
		directional.OnDirectionChange.AddListener(OnDirectionChange);
		integrity.OnApplyDamage.AddListener(OnDamageReceived);
	}

	private void OnDisable()
	{
		directional.OnDirectionChange.RemoveListener(OnDirectionChange);
		if(integrity != null) integrity.OnApplyDamage.RemoveListener(OnDamageReceived);

		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, TrySpark);
	}

	[Server]
	public void ServerChangeLightState(LightMountState newState)
	{
		mState = newState;

		if (newState == LightMountState.Broken)
		{
			UpdateManager.Add(TrySpark, 1f);
		}
		else
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, TrySpark);
		}
	}

	public bool HasBulb()
	{
		return mState != LightMountState.MissingBulb && mState != LightMountState.None;
	}

	private void SyncLightState(LightMountState oldState, LightMountState newState)
	{
		mState = newState;
		ChangeCurrentState(newState);
		SetAnimation();
	}

	private void ChangeCurrentState(LightMountState newState)
	{
		if (mountStatesMachine.LightMountStates.Contains(newState))
		{
			currentState = mountStatesMachine.LightMountStates[newState];
		}
		SetSprites();
	}

	public void EditorDirectionChange()
	{
		directional = GetComponent<Directional>();
			spriteRendererLightOn = GetComponentsInChildren<SpriteRenderer>().Length > 1
				? GetComponentsInChildren<SpriteRenderer>()[1] : GetComponentsInChildren<SpriteRenderer>()[0];
		var state = mountStatesMachine.LightMountStates[LightMountState.On];

		spriteHandler.SetSpriteSO(state.SpriteData, null, SpritesDirectional.OrientationIndex(directional.CurrentDirection.AsEnum()));
		spriteRendererLightOn.sprite = spritesStateOnEffect.GetSpriteInDirection(directional.CurrentDirection.AsEnum());
		RefreshBoxCollider();
	}

	public void RefreshBoxCollider()
	{
		directional = GetComponent<Directional>();
		Vector2 offset = Vector2.zero;
		Vector2 size = Vector2.zero;

		switch (directional.CurrentDirection.AsEnum())
		{
			case OrientationEnum.Down:
				offset = new Vector2(collDownSetting.x, collDownSetting.y);
				size = new Vector2(collDownSetting.z, collDownSetting.w);
				break;
			case OrientationEnum.Right:
				offset = new Vector2(collRightSetting.x, collRightSetting.y);
				size = new Vector2(collRightSetting.z, collRightSetting.w);
				break;
			case OrientationEnum.Up:
				offset = new Vector2(collUpSetting.x, collUpSetting.y);
				size = new Vector2(collUpSetting.z, collUpSetting.w);
				break;
			case OrientationEnum.Left:
				offset = new Vector2(collLeftSetting.x, collLeftSetting.y);
				size = new Vector2(collLeftSetting.z, collLeftSetting.w);
				break;
		}

		boxColl.offset = offset;
		boxColl.size = size;
	}

	private void SetSprites()
	{
		spriteHandler.SetSpriteSO(currentState.SpriteData, null, SpritesDirectional.OrientationIndex(directional.CurrentDirection.AsEnum()));		
		spriteRendererLightOn.sprite = mState == LightMountState.On
				? spritesStateOnEffect.GetSpriteInDirection(directional.CurrentDirection.AsEnum())
				: null;

		itemInMount = currentState.Tube;

		var currentMultiplier = currentState.MultiplierIntegrity;
		if (currentMultiplier > 0.15f)
			integrityThreshBar = integrity.initialIntegrity * currentMultiplier;

		RefreshBoxCollider();
	}

	private void SetAnimation()
	{
		lightSprite.Color = currentState.LightColor;
		switch (mState)
		{
			case LightMountState.Emergency:
				lightSprite.Color = EmergencyColour;
				mLightRendererObject.transform.localScale = Vector3.one * 3.0f;
				mLightRendererObject.SetActive(true);
				if (emergencyLightAnimator != null)
				{
					emergencyLightAnimator.StartAnimation();
				}
				break;
			case LightMountState.On:
				if (emergencyLightAnimator != null)
				{
					emergencyLightAnimator.StopAnimation();
				}
				lightSprite.Color = ONColour;
				mLightRendererObject.transform.localScale = Vector3.one * 12.0f;
				mLightRendererObject.SetActive(true);
				break;
			default:
				if (emergencyLightAnimator != null)
				{
					emergencyLightAnimator.StopAnimation();
				}
				mLightRendererObject.transform.localScale = Vector3.one * 12.0f;
				mLightRendererObject.SetActive(false);
				break;
		}
	}

	#region ICheckedInteractable<HandApply>

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (!construction.isFullyBuilt()) return false;
		if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;
		if (interaction.HandObject != null && !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.LightReplacer) && !Validations.HasItemTrait(interaction.HandObject, traitRequired)) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.HandObject == null)
		{
			TryRemoveBulb(interaction);
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.LightReplacer))
		{
			TryReplaceBulb(interaction);
		}
		else if (Validations.HasItemTrait(interaction.HandObject, traitRequired))
		{
			TryAddBulb(interaction);
		}
	}

	private void TryRemoveBulb(HandApply interaction)
	{
		var handSlot = interaction.PerformerPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.hands);

		if (mState == LightMountState.On && (handSlot.IsOccupied == false ||
				!Validations.HasItemTrait(handSlot.ItemObject, CommonTraits.Instance.BlackGloves)))
		{
			var playerHealth = interaction.PerformerPlayerScript.playerHealth;
			var burntBodyPart = interaction.HandSlot.NamedSlot == NamedSlot.leftHand ? BodyPartType.LeftArm : BodyPartType.RightArm;
			playerHealth.ApplyDamageToBodyPart(gameObject, 10f, AttackType.Energy, DamageType.Burn, burntBodyPart);

			Chat.AddExamineMsgFromServer(interaction.Performer,
					"<color=red>You burn your hand on the bulb while attempting to remove it!</color>");
			return;
		}

		var spawnedItem = Spawn.ServerPrefab(itemInMount, interaction.Performer.WorldPosServer()).GameObject;
		ItemSlot bestHand = interaction.PerformerPlayerScript.ItemStorage.GetBestHand();
		if (bestHand != null && spawnedItem != null)
		{
			Inventory.ServerAdd(spawnedItem, bestHand);
		}

		ServerChangeLightState(LightMountState.MissingBulb);
	}

	private void TryAddBulb(HandApply interaction)
	{
		if (mState != LightMountState.MissingBulb) return;

		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Broken))
		{
			ServerChangeLightState(LightMountState.Broken);
		}
		else
		{
			ServerChangeLightState(
				(switchState && (powerState == PowerStates.On))
				? LightMountState.On : (powerState != PowerStates.OverVoltage)
				? LightMountState.Emergency : LightMountState.Off);
		}

		Despawn.ServerSingle(interaction.HandObject);
	}

	private void TryReplaceBulb(HandApply interaction)
	{
		if (mState != LightMountState.MissingBulb)
		{
			Spawn.ServerPrefab(itemInMount, interaction.Performer.WorldPosServer());
			ServerChangeLightState(LightMountState.MissingBulb);
		}
	}

	#endregion

	#region IAPCPowered
	public void PowerNetworkUpdate(float Voltage)
	{

	}

	public void StateUpdate(PowerStates newPowerState)
	{
		if (!isServer) return;
		powerState = newPowerState;
		if(mState == LightMountState.Broken
		   || mState == LightMountState.MissingBulb) return;

		switch (newPowerState)
		{
			case PowerStates.On:
				ServerChangeLightState(LightMountState.On);
				return;
			case PowerStates.LowVoltage:
				ServerChangeLightState(LightMountState.Emergency);
				return;
			case PowerStates.OverVoltage:
				ServerChangeLightState(LightMountState.BurnedOut);
				return;
			case PowerStates.Off:
				ServerChangeLightState(LightMountState.Emergency);
				return;
		}
	}
	#endregion

	#region SwitchRelatedLogic

	public void SubscribeToSwitchEvent(LightSwitchV2 lightSwitch)
	{
		UnSubscribeFromSwitchEvent();
		relatedLightSwitch = lightSwitch;
		lightSwitch.SwitchTriggerEvent += Trigger;
	}

	public void UnSubscribeFromSwitchEvent()
	{
		if (relatedLightSwitch == null)
			return;
		relatedLightSwitch.SwitchTriggerEvent -= Trigger;
		relatedLightSwitch = null;
	}

	public override void Trigger(bool newState)
	{
		if (!isServer) return;
		switchState = newState;
		if(mState == LightMountState.On ||  mState == LightMountState.Off)
			ServerChangeLightState(newState ? LightMountState.On : LightMountState.Off);
	}

	#endregion

	#region Spark

	private void TrySpark()
	{
		//Has to be broken and have power to spark
		if(mState != LightMountState.Broken || powerState == PowerStates.Off) return;

		//25% chance to do effect and not already doing an effect
		if(DMMath.Prob(75) || currentSparkEffect != null) return;

		var worldPos = registerTile.WorldPositionServer;

		var result = Spawn.ServerPrefab(CommonPrefabs.Instance.SparkEffect, worldPos, gameObject.transform.parent);
		if (result.Successful)
		{
			currentSparkEffect = result.GameObject;

			//Try start fire if possible
			var reactionManager = MatrixManager.AtPoint(worldPos, true).ReactionManager;
			reactionManager.ExposeHotspotWorldPosition(worldPos.To2Int());

			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.Sparks, worldPos, sourceObj: gameObject);
		}
	}

	#endregion

	private void OnDamageReceived(DamageInfo arg0)
	{
		if(CustomNetworkManager.IsServer == false) return;

		CheckIntegrityState();
	}

	private void CheckIntegrityState()
	{
		if (integrity.integrity > integrityThreshBar || mState == LightMountState.MissingBulb) return;
		Vector3 pos = gameObject.AssumedWorldPosServer();

		if (mState == LightMountState.Broken)
		{
			ServerChangeLightState(LightMountState.MissingBulb);
			SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.GlassStep, pos, sourceObj: gameObject);
		}
		else
		{
			ServerChangeLightState(LightMountState.Broken);
			Spawn.ServerPrefab(CommonPrefabs.Instance.GlassShard, pos, count: Random.Range(0, 2),
				scatterRadius: Random.Range(0, 2));
			TrySpark();
		}
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		Spawn.ServerPrefab(currentState.LootDrop, gameObject.RegisterTile().WorldPositionServer);
		UnSubscribeFromSwitchEvent();
	}

	void OnDrawGizmosSelected()
	{

		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;
		if (relatedLightSwitch == null)
		{
			if (isWithoutSwitch) return;
			Gizmos.color = new Color(1, 0.5f, 1, 1);
			Gizmos.DrawSphere(sprite.transform.position, 0.20f);
			return;
		}
		//Highlighting all controlled lightSources
		Gizmos.color = new Color(1, 1, 0, 1);
		Gizmos.DrawLine(relatedLightSwitch.transform.position, gameObject.transform.position);
		Gizmos.DrawSphere(relatedLightSwitch.transform.position, 0.25f);

	}
}
