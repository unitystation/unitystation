using System;
using System.Collections;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Light2D;
using Lighting;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightSource : ObjectTrigger, ICheckedInteractable<HandApply>, IAPCPowered, IServerDespawn
{
	public LightSwitchV2 relatedLightSwitch;

	private float coolDownTime = 2.0f;
	private bool isInCoolDown;

	[SerializeField]
	private LightMountState InitialState = LightMountState.On;

	[SyncVar(hook =nameof(SyncLightState))]
	private LightMountState mState;

	[Header("Generates itself if this is null:")]
	public GameObject mLightRendererObject;
	[SerializeField]
	private bool isWithoutSwitch = true;
	public bool IsWithoutSwitch => isWithoutSwitch;
	private bool switchState;
	private PowerStates powerState;

	[SerializeField]private SpriteRenderer spriteRenderer;
	[SerializeField]private SpriteRenderer spriteRendererLightOn;
	private LightSprite lightSprite;
	private EmergencyLightAnimator emergencyLightAnimator;
	private Integrity integrity;
	private Directional directional;


	[SerializeField] private SpritesDirectional spritesStateOnEffect;
	[SerializeField] private SOLightMountStatesMachine mountStatesMachine;
	private SOLightMountState currentState;

	private ItemTrait traitRequired;
	private GameObject itemInMount;
	private float integrityThreshBar;

	private void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (mLightRendererObject == null)
			mLightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, new Color(0, 0, 0, 0), 12);

		if(spriteRenderer == null)
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();

		if(spriteRendererLightOn == null) spriteRendererLightOn = GetComponentsInChildren<SpriteRenderer>().Length > 1
			? GetComponentsInChildren<SpriteRenderer>()[1] : GetComponentsInChildren<SpriteRenderer>()[0];

		if(lightSprite == null)
			lightSprite = mLightRendererObject.GetComponent<LightSprite>();

		if(emergencyLightAnimator == null)
			emergencyLightAnimator = GetComponent<EmergencyLightAnimator>();

		if (integrity == null)
			integrity = GetComponent<Integrity>();

		if(directional == null)
			directional = GetComponent<Directional>();

		if(currentState == null)
			ChangeCurrentState(InitialState);

		if(traitRequired == null)
			traitRequired = currentState.TraitRequired;

		if (!isWithoutSwitch)
			switchState = InitialState == LightMountState.On;
	}

	[Server]
	public void ServerChangeLightState(LightMountState newState)
	{
		mState = newState;
	}

	private void SyncLightState(LightMountState oldState, LightMountState newState)
	{
		mState = newState;
		ChangeCurrentState(newState);
		SetAnimation();
	}

	private void ChangeCurrentState(LightMountState newState)
	{
		currentState = mountStatesMachine.LightMountStates[newState];
		CashCurrentStateVars();
	}

	private void CashCurrentStateVars()
	{
		spriteRenderer.sprite = currentState.SpritesDirectional.GetSpriteInDirection(directional.CurrentDirection.AsEnum());

		spriteRendererLightOn.sprite = mState == LightMountState.On
				? spritesStateOnEffect.GetSpriteInDirection(directional.CurrentDirection.AsEnum())
				: null;

		itemInMount = currentState.Tube;

		var currentMultiplier = currentState.MultiplierIntegrity;
		if (currentMultiplier > 0.15f)
			integrityThreshBar = integrity.initialIntegrity * currentMultiplier;
	}

	private void SetAnimation()
	{
		lightSprite.Color = currentState.LightColor;
		switch (mState)
		{
			case LightMountState.Emergency:
				lightSprite.Color = Color.red;
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
				lightSprite.Color = Color.white;
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
		if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;
		return true;
	}
	public void ServerPerformInteraction(HandApply interaction)
	{
		if (isInCoolDown) return;
		StartCoroutine(CoolDown());
		var handObject = interaction.HandObject;
		var performer = interaction.Performer;
		if (handObject == null)
		{
			if (mState == LightMountState.On &&
			    !Validations.HasItemTrait(interaction.PerformerPlayerScript.Equipment.GetClothingItem(NamedSlot.hands).GameObjectReference, CommonTraits.Instance.BlackGloves))
			{
				interaction.PerformerPlayerScript.playerHealth.ApplyDamageToBodypart(gameObject, 10f, AttackType.Energy, DamageType.Burn,
					interaction.HandSlot.NamedSlot == NamedSlot.leftHand ? BodyPartType.LeftArm : BodyPartType.RightArm);
				Chat.AddExamineMsgFromServer(performer, $"<color=red>You burn your hand while attempting to remove the light</color>");
				return;
			}
			Spawn.ServerPrefab(itemInMount,performer.WorldPosServer());
			ServerChangeLightState(LightMountState.MissingBulb);
		}
		else if (Validations.HasItemTrait(handObject, CommonTraits.Instance.LightReplacer) && mState  != LightMountState.MissingBulb)
		{
			Spawn.ServerPrefab(itemInMount,performer.WorldPosServer());
			ServerChangeLightState(LightMountState.MissingBulb);
		}
		else if (Validations.HasItemTrait(handObject, traitRequired) && mState  == LightMountState.MissingBulb)
		{

			if (Validations.HasItemTrait(handObject, CommonTraits.Instance.Broken))
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
			Despawn.ServerSingle(handObject);
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

	public bool SubscribeToSwitchEvent(LightSwitchV2 lightSwitch)
	{
		if (lightSwitch == null) return false;
		UnSubscribeFromSwitchEvent();
		relatedLightSwitch = lightSwitch;
		lightSwitch.switchTriggerEvent += Trigger;
		return true;
	}

	public bool UnSubscribeFromSwitchEvent()
	{
		if (relatedLightSwitch == null) return false;
		relatedLightSwitch.switchTriggerEvent -= Trigger;
		relatedLightSwitch = null;
		return true;
	}

	public override void Trigger(bool newState)
	{
		if (!isServer) return;
		switchState = newState;
		if(powerState == PowerStates.On)
			ServerChangeLightState(newState ? LightMountState.On : LightMountState.Off);
	}

	#endregion

	public override void OnStartClient()
	{
		EnsureInit();
		base.OnStartClient();
		SyncLightState(mState, mState);
	}

	private void OnEnable()
	{
		EnsureInit();
		integrity.OnApllyDamage.AddListener(OnDamageReceived);
	}

	private void OnDisable()
	{
		if(integrity != null) integrity.OnApllyDamage.RemoveListener(OnDamageReceived);
	}

	private void OnDamageReceived(DamageInfo arg0)
	{
		CheckIntegrityState();
	}

	private void CheckIntegrityState()
	{
		if (integrity.integrity > integrityThreshBar || mState == LightMountState.MissingBulb) return;
		Vector3 pos = gameObject.AssumedWorldPosServer();

		if (mState == LightMountState.Broken)
		{

			ServerChangeLightState(LightMountState.MissingBulb);
			SoundManager.PlayNetworkedAtPos("GlassStep", pos, sourceObj: gameObject);
		}
		else
		{
			ServerChangeLightState(LightMountState.Broken);
			Spawn.ServerPrefab("GlassShard", pos, count: Random.Range(0, 2),
				scatterRadius: Random.Range(0, 2));
		}
	}

	public void OnDespawnServer(DespawnInfo info)
	{
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

	private IEnumerator CoolDown()
	{
		isInCoolDown = true;
		yield return WaitFor.Seconds(coolDownTime);
		isInCoolDown = false;
	}
}