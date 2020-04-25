using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;
public enum LightMountState
{
	None = 0,
	On,
	Off,
	MissingBulb,
	Broken,
	TypeCount,
}
public class LightMountStates : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	private float coolDownTime = 3.0f;

	private bool isInCoolDown;

	[SyncVar(hook = nameof(SyncLightState))]
	private LightMountState state = LightMountState.On;

	public LightMountState State => state;

	[Tooltip("Sprite for bulb.")]
	public SpriteRenderer spriteRenderer;

	[Tooltip("Sprite for light effect.")]
	public SpriteRenderer spriteRendererLightOn;
	//Second layer for light effect

	private LightSource lightSource;
	private Integrity integrity;
	private Orientation orientation;

	[Tooltip("Item with this trait will be put in.")]
	public ItemTrait traitRequired;

	[Header("Properly functional state.")]
	[Tooltip("In On/Off state will drop this item.")]
	public GameObject appliableItem;

	public Sprite[] spriteListFull;

	public Sprite[] spriteListLightOn;

	[Header("Broken state.")]
	[Tooltip("In Broken state will drop this item.")]
	public GameObject appliableBrokenItem;

	public Sprite[] spriteListBroken;

	[Tooltip("On what % of integrity mount changes state.")]
	[Range(0.40f, 0.90f)]
	public float multiplierBroken = 0.60f;

	private float integrityStateBroken;

	[Header("Empty state.")]
	[Tooltip("On what % of integrity mount changes state.")]
	[Range(0.2f, 0.6f)]
	public float multiplierMissingBulb;

	public Sprite[] spriteListMissingBulb;

	private float integrityStateMissingBulb;

	private void OnEnable()
	{
		EnsureInit();
		integrity.OnApllyDamage.AddListener(OnDamageReceived);
	}

	private void OnDisable()
	{
		if(integrity != null) integrity.OnApllyDamage.RemoveListener(OnDamageReceived);
	}

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

		if (interaction.HandObject == null)
		{
			if (state == LightMountState.On && state != LightMountState.MissingBulb && state != LightMountState.Broken && !Validations.HasItemTrait(interaction.PerformerPlayerScript.Equipment.GetClothingItem(NamedSlot.hands).GameObjectReference, CommonTraits.Instance.BlackGloves))
			{
				if (interaction.HandSlot.NamedSlot == NamedSlot.leftHand)
				{
					interaction.PerformerPlayerScript.playerHealth.ApplyDamageToBodypart(gameObject, 10f, AttackType.Energy, DamageType.Burn, BodyPartType.LeftArm);
					Chat.AddExamineMsgFromServer(interaction.Performer, "<color=red>You burn your left hand while attempting to remove the light</color>");
				}
				else
				{
					interaction.PerformerPlayerScript.playerHealth.ApplyDamageToBodypart(gameObject, 10f, AttackType.Energy, DamageType.Burn, BodyPartType.RightArm);
					Chat.AddExamineMsgFromServer(interaction.Performer, "<color=red>You burn your right hand while attempting to remove the light</color>");
				}
				return;
			}

			Spawn.ServerPrefab(state == LightMountState.Broken ? appliableBrokenItem : appliableItem,
				interaction.Performer.WorldPosServer());
			ServerChangeLightState(LightMountState.MissingBulb);
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.LightReplacer) && state != LightMountState.MissingBulb)
		{
			Spawn.ServerPrefab(state == LightMountState.Broken ? appliableBrokenItem : appliableItem,
				interaction.Performer.WorldPosServer());
			ServerChangeLightState(LightMountState.MissingBulb);
		}
		else if (Validations.HasItemTrait(interaction.HandObject, traitRequired) && state == LightMountState.MissingBulb)
		{

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Broken))
			{
				ServerChangeLightState(LightMountState.Broken);
			}
			else
			{
				ServerChangeLightState(lightSource.SwitchState ? LightMountState.On : LightMountState.Off);
			}
			Despawn.ServerSingle(interaction.HandObject);
		}
	}

	public bool EnsureInit()
	{
		if (lightSource == null) lightSource = GetComponent<LightSource>();
		if (integrity == null) integrity = GetComponent<Integrity>();

		if (lightSource == null || integrity == null)
		{
			Logger.Log($"This light mount is missing a light source or integrity component! {gameObject.name}", Category.Lighting);
			return false;
		}
		integrityStateBroken = integrity.initialIntegrity * multiplierBroken;
		integrityStateMissingBulb = integrity.initialIntegrity * multiplierMissingBulb;
		orientation = GetComponent<Directional>().CurrentDirection;
		return true;
	}

	private void ChangeLightState(LightMountState newState)
	{
		if (!EnsureInit()) return;

		if (newState == LightMountState.On)
		{
			lightSource.ServerChangeLightState(LightState.On);
			spriteRenderer.sprite = GetSprite(spriteListFull);
			spriteRendererLightOn.sprite = GetSprite(spriteListLightOn);
			integrity.soundOnHit = "GlassHit";
			return;
		}

		if (newState == LightMountState.MissingBulb)
		{
			spriteRenderer.sprite = GetSprite(spriteListMissingBulb);
			integrity.soundOnHit = "";
		}
		else if (newState == LightMountState.Broken)
		{

			spriteRenderer.sprite = GetSprite(spriteListBroken);
			integrity.soundOnHit = "GlassStep";
		}
		else if (newState == LightMountState.Off)
		{
			spriteRenderer.sprite = GetSprite(spriteListFull);
			integrity.soundOnHit = "GlassHit";
		}
		lightSource.ServerChangeLightState(LightState.Off);
		spriteRendererLightOn.sprite = null;
	}

	//This one is for LightSource to make sure sprites and states are correct
	//when lights switch state is changed
	public void SwitchChangeState(bool switchState)
	{
		if (!isServer) return;
		if (State == LightMountState.Broken ||
		    State == LightMountState.MissingBulb)
		{
			return;
		}

		ServerChangeLightState(switchState ? LightMountState.On : LightMountState.Off);
	}

	//Gets sprites for eash state
	private Sprite GetSprite(Sprite[] spriteList)
	{
		if (spriteList == null)
		{
			return null;
		}
		int angle = orientation.Degrees;
		switch (angle)
		{
			case 0:
				return spriteList[1];
			case 90:
				return spriteList[0];
			case 180:
				return spriteList[3];
			default:
				return spriteList[2];
		}
	}

	private void CheckIntegrityState()
	{
		if (integrity.integrity <= integrityStateBroken && state != LightMountState.MissingBulb)
		{
			Vector3 pos = gameObject.AssumedWorldPosServer();

			if (integrity.integrity <= integrityStateMissingBulb)
			{
				ServerChangeLightState(LightMountState.MissingBulb);
				Spawn.ServerPrefab("GlassShard", pos, count: Random.Range(0, 2),
				scatterRadius: Random.Range(0, 2));
			}
			else if (state != LightMountState.Broken)
			{

				ServerChangeLightState(LightMountState.Broken);
				SoundManager.PlayNetworkedAtPos("GlassStep", pos, sourceObj: gameObject);
			}
		}
	}

	//Changes state when Integrity's ApplyDamage called
	private void OnDamageReceived(DamageInfo arg0)
	{
		CheckIntegrityState();
	}

	private void SyncLightState(LightMountState oldState, LightMountState newState)
	{
		state = newState;
		ChangeLightState(newState);
	}

	public override void OnStartClient()
	{
		SyncLightState(state, state);
		base.OnStartClient();
	}

	[Server]
	private void ServerChangeLightState(LightMountState newState)
	{
		state = newState;
	}

	private IEnumerator CoolDown()
	{
		isInCoolDown = true;
		yield return WaitFor.Seconds(coolDownTime);
		isInCoolDown = false;
	}
}
