using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class WallMountItemContainer : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	// Start is called before the first frame update

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

	public ItemTrait traitRequired;
	public GameObject appliableItem;
	public GameObject appliableBrokenItem;

	public Sprite[] spriteListBroken;
	public Sprite[] spriteListEmpty;
	public Sprite[] spriteListFull;
	public Sprite[] spriteListLightOn;

	public SpriteRenderer spriteRenderer;
	public SpriteRenderer spriteRendererLightOn;

	private LightSource lightSource;
	private LightSwitch lightSwitch;
	private Integrity integrity;

	private Orientation orientation;

	private void Awake()
	{
		lightSource = GetComponent<LightSource>();

		lightSwitch = lightSource.relatedLightSwitch;

		integrity = GetComponent<Integrity>();

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
				lightSource.Trigger(false);
				spriteRenderer.sprite = GetSprite(spriteListEmpty);
				spriteRendererLightOn.sprite = null;
				Spawn.ServerPrefab(appliableItem, interaction.Performer.WorldPosServer());
				Chat.AddExamineMsg(interaction.Performer, "You took the light tube out!");
				state = LightMountState.MissingBulb;
			}
			else if(state == LightMountState.Off)
			{
				spriteRenderer.sprite = GetSprite(spriteListEmpty);
				spriteRendererLightOn.sprite = null;
				Spawn.ServerPrefab(appliableItem, interaction.Performer.WorldPosServer());
				Chat.AddExamineMsg(interaction.Performer, "You took the light tube out!");
				state = LightMountState.MissingBulb;
			}
			else if(state == LightMountState.Broken)
			{
				spriteRenderer.sprite = GetSprite(spriteListEmpty);
				spriteRendererLightOn.sprite = null;
				Spawn.ServerPrefab(appliableBrokenItem, interaction.Performer.WorldPosServer());
				Chat.AddExamineMsg(interaction.Performer, "You took the broken light tube out!");
				state = LightMountState.MissingBulb;
			}
					
		}
		else if (Validations.HasItemTrait(interaction.HandObject, traitRequired) && state == LightMountState.MissingBulb)
		{

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Broken))
			{
				lightSource.Trigger(false);
				Despawn.ServerSingle(interaction.HandObject);
				spriteRenderer.sprite = GetSprite(spriteListBroken);
				spriteRendererLightOn.sprite = null;
				Chat.AddExamineMsg(interaction.Performer, "You put broken light tube in!");
				state = LightMountState.Broken;
			}
			else
			{
				if (lightSwitch.isOn == LightSwitch.States.On)
				{
					lightSource.Trigger(true);
					Despawn.ServerSingle(interaction.HandObject);
					spriteRenderer.sprite = GetSprite(spriteListFull);
					spriteRendererLightOn.sprite = GetSprite(spriteListLightOn);
					Chat.AddExamineMsg(interaction.Performer, "You put light tube in!");
					state = LightMountState.On;
				}
				else
				{
					lightSource.Trigger(false);
					Despawn.ServerSingle(interaction.HandObject);
					spriteRenderer.sprite = GetSprite(spriteListFull);
					spriteRendererLightOn.sprite = null;
					Chat.AddExamineMsg(interaction.Performer, "You put light tube in!");
					state = LightMountState.Off;
				}
			}
			
		}
	}

	public void ChangeState(LightState state)
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

	private Sprite GetSprite(Sprite[] spriteList)
	{
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

	private void OnDamageReceived(DamageInfo arg0)
	{
		if(integrity.integrity <= 70)
		{
			Vector3 pos = gameObject.AssumedWorldPosServer();
			lightSource.Trigger(false);
			spriteRenderer.sprite = GetSprite(spriteListBroken);
			spriteRendererLightOn.sprite = null;
			state = LightMountState.Broken;
			SoundManager.PlayNetworkedAtPos("GlassStep", pos);
			Spawn.ServerPrefab("GlassShard", pos, count: Random.Range(0, 2),
			scatterRadius: Random.Range(0, 2));
		}

	}
}