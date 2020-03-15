using System.Collections.Generic;
using UnityEngine;
using Mirror;

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

	private LightMountState state = LightMountState.On;

	public LightMountState State
	{
		get
		{
			return state;
		}
	}

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
	private void Start()
	{
		lightSwitch = lightSource.relatedLightSwitch;
	}
	private void Awake()
	{
		lightSource = GetComponent<LightSource>();


		integrity = GetComponent<Integrity>();

		integrityStateBroken = integrity.initialIntegrity * multiplierBroken;

		integrityStateMissingBulb = integrity.initialIntegrity * multiplierMissingBulb;

		orientation = GetComponent<Directional>().CurrentDirection;

		integrity.OnApllyDamage.AddListener(OnDamageReceived);
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

			if(state == LightMountState.On)
			{
				Spawn.ServerPrefab(appliableItem, interaction.Performer.WorldPosServer());
				Chat.AddExamineMsg(interaction.Performer, "You took the light tube out!");
				ChangeState(LightMountState.MissingBulb, spriteListMissingBulb, null, "", false);
			}
			else if(state == LightMountState.Off)
			{
				Spawn.ServerPrefab(appliableItem, interaction.Performer.WorldPosServer());
				Chat.AddExamineMsg(interaction.Performer, "You took the light tube out!");
				ChangeState(LightMountState.MissingBulb, spriteListMissingBulb, null, "");
			}
			else if(state == LightMountState.Broken)
			{
				Spawn.ServerPrefab(appliableBrokenItem, interaction.Performer.WorldPosServer());
				Chat.AddExamineMsg(interaction.Performer, "You took the broken light tube out!");
				ChangeState(LightMountState.MissingBulb, spriteListMissingBulb, null, "");
			}
					
		}
		else if (Validations.HasItemTrait(interaction.HandObject, traitRequired) && state == LightMountState.MissingBulb)
		{

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Broken))
			{
				Despawn.ServerSingle(interaction.HandObject);
				Chat.AddExamineMsg(interaction.Performer, "You put broken light tube in!");
				ChangeState(LightMountState.Broken, spriteListBroken, null, "GlassStep", false);
			}
			else
			{
				if(lightSwitch == null)
				{
					lightSwitch = lightSource.relatedLightSwitch;
				}
				if (lightSwitch.isOn == LightSwitch.States.On)
				{
					Despawn.ServerSingle(interaction.HandObject);
					Chat.AddExamineMsg(interaction.Performer, "You put light tube in!");
					ChangeState(LightMountState.On, spriteListFull, spriteListLightOn, "GlassHit", true);
				}
				else
				{
					Despawn.ServerSingle(interaction.HandObject);
					Chat.AddExamineMsg(interaction.Performer, "You put light tube in!");
					ChangeState(LightMountState.Off, spriteListFull, null, "GlassHit", false);
				}
			}
			
		}
	}

	private void ChangeState(LightMountState state, Sprite[] spriteListBulb, Sprite[] spriteListLight, string sound, bool? triggerState = null)
	{

		if (triggerState != null)
		{
			lightSource.Trigger(triggerState.Value);
		}
		spriteRenderer.sprite = GetSprite(spriteListBulb);
		spriteRendererLightOn.sprite = GetSprite(spriteListLight);
		integrity.soundOnHit = sound;
		this.state = state;
	}

	//This one is for LightSource to make sure sprites and states are correct
	//when lights switch state is changed
	public void SwitchChangeState(LightState state)
	{
	
		if (state == LightState.On)
		{
			spriteRendererLightOn.sprite = GetSprite(spriteListLightOn);
			this.state = LightMountState.On;
		}
		else
		{
			spriteRendererLightOn.sprite = null;
			this.state = LightMountState.Off;
		}
	}

	//Gets sprites for eash state
	private Sprite GetSprite(Sprite[] spriteList)
	{
		if(spriteList == null)
		{
			return null;
		}
		int angle = orientation.Degrees;
		switch(angle)
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
				
				if(integrity.integrity <= integrityStateMissingBulb)
				{
					ChangeState(LightMountState.MissingBulb, spriteListMissingBulb, null, "", false);
					Spawn.ServerPrefab("GlassShard", pos, count: Random.Range(0, 2),
					scatterRadius: Random.Range(0, 2));
				}
				else if(state != LightMountState.Broken)
				{
					
					ChangeState(LightMountState.Broken, spriteListBroken, null, "GlassStep", false);
					SoundManager.PlayNetworkedAtPos("GlassStep", pos);
				}
			}
	}

	//Changes state when Integrity's ApplyDamage called
	private void OnDamageReceived(DamageInfo arg0)
	{
		CheckIntegrityState();
	}
}