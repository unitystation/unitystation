using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

public class LightMountStates : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	//Burned out state is missing
	public enum LightMountState
	{
		None = 0,
		On,
		Off,
		MissingBulb,
		Broken,
		TypeCount,
	}

	[SyncVar(hook = nameof(SyncLightState))]
	private LightMountState state = LightMountState.On;

	public LightMountState State => state;


	[Tooltip("Sprite for bulb.")]
	public SpriteRenderer spriteRenderer;

	//Second layer for light effect

	private LightSource lightSource;
	private LightSwitch lightSwitch;
	private Integrity integrity;
	private Orientation orientation;

	[Tooltip("Item with this trait will be put in.")]
	public ItemTrait traitRequired;

	[Header("Properly functional state.")]
	[Tooltip("In On/Off state will drop this item.")]
	public GameObject appliableItem;

	public Sprite[] spriteListFull;


	public Sprite[] spriteListLightOn;

	[Tooltip("Sprite for light effect.")]
	public SpriteRenderer spriteRendererLightOn;



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
		if (interaction.HandObject == null)
		{

			if (state == LightMountState.On)
			{
				Spawn.ServerPrefab(appliableItem, interaction.Performer.WorldPosServer());
				Chat.AddExamineMsg(interaction.Performer, "You took the light tube out!");
				ServerChangeLightState(LightMountState.MissingBulb);
			}
			else if (state == LightMountState.Off)
			{
				Spawn.ServerPrefab(appliableItem, interaction.Performer.WorldPosServer());
				Chat.AddExamineMsg(interaction.Performer, "You took the light tube out!");
				ServerChangeLightState(LightMountState.MissingBulb);
			}
			else if (state == LightMountState.Broken)
			{
				Spawn.ServerPrefab(appliableBrokenItem, interaction.Performer.WorldPosServer());
				Chat.AddExamineMsg(interaction.Performer, "You took the broken light tube out!");
				ServerChangeLightState(LightMountState.MissingBulb);
			}

		}
		else if (Validations.HasItemTrait(interaction.HandObject, traitRequired) && state == LightMountState.MissingBulb)
		{

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Broken))
			{
				Despawn.ServerSingle(interaction.HandObject);
				Chat.AddExamineMsg(interaction.Performer, "You put broken light tube in!");
				ServerChangeLightState(LightMountState.Broken);
			}
			else
			{
				if (lightSwitch == null)
				{
					lightSwitch = lightSource.relatedLightSwitch;
				}
				if (lightSwitch.isOn == LightSwitch.States.On)
				{
					Despawn.ServerSingle(interaction.HandObject);
					Chat.AddExamineMsg(interaction.Performer, "You put light tube in!");
					ServerChangeLightState(LightMountState.On);
				}
				else
				{
					Despawn.ServerSingle(interaction.HandObject);
					Chat.AddExamineMsg(interaction.Performer, "You put light tube in!");
					ServerChangeLightState(LightMountState.Off);
				}
			}
		}
	}

	bool EnsureInit()
	{
		if (lightSource == null) lightSource = GetComponent<LightSource>();
		if (integrity == null) integrity = GetComponent<Integrity>();

		if (lightSource == null || integrity == null)
		{
			Logger.Log($"This light mount is missing a light source or integrity component! {gameObject.name}", Category.Lighting);
			return false;
		}

		lightSwitch = lightSource.relatedLightSwitch;
		integrityStateBroken = integrity.initialIntegrity * multiplierBroken;
		integrityStateMissingBulb = integrity.initialIntegrity * multiplierMissingBulb;
		orientation = GetComponent<Directional>().CurrentDirection;
		return true;
	}

	private void ChangeLightState(LightMountState newState)
	{
		if (!EnsureInit()) return;

		if (newState == LightMountState.MissingBulb)
		{
			lightSource.Trigger(false);
			spriteRenderer.sprite = GetSprite(spriteListMissingBulb);
			spriteRendererLightOn.sprite = null;
			integrity.soundOnHit = "";
		}
		else if (newState == LightMountState.Broken)
		{
			lightSource.Trigger(false);
			spriteRenderer.sprite = GetSprite(spriteListBroken);
			spriteRendererLightOn.sprite = null;
			integrity.soundOnHit = "GlassStep";
		}
		else if (newState == LightMountState.Off)
		{
			lightSource.Trigger(false);
			spriteRenderer.sprite = GetSprite(spriteListFull);
			spriteRendererLightOn.sprite = null;
			integrity.soundOnHit = "GlassHit";
		}
		else if (newState == LightMountState.On)
		{
			lightSource.Trigger(true);
			spriteRenderer.sprite = GetSprite(spriteListFull);
			spriteRendererLightOn.sprite = GetSprite(spriteListLightOn);
			integrity.soundOnHit = "GlassHit";
		}
	}

	//This one is for LightSource to make sure sprites and states are correct
	//when lights switch state is changed
	public void SwitchChangeState(LightState state)
	{
		if (!isServer) return;

		if (state == LightState.On)
		{
			ServerChangeLightState(LightMountState.On);
		}
		else
		{
			ServerChangeLightState(LightMountState.Off);
		}
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
}