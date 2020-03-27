using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Pickupable))]
public class Meter : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	private Pipe pipe;
	public bool anchored;
	private RegisterTile registerTile;
	private ObjectBehaviour objectBehaviour;

	public SpriteRenderer spriteRenderer;
	public List<Sprite> spriteList = new List<Sprite>();
	[SyncVar(hook = nameof(SyncSprite))] public int spriteSync;

	private void Awake() {
		registerTile = GetComponent<RegisterTile>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}

	public override void OnStartClient()
	{
		SyncSprite(0, spriteSync);
	}

	public void UpdateMe()
	{
		float pressure = pipe.pipenet.gasMix.Pressure;
		if(pressure >= 0)
		{
			spriteSync = 1;
		}
		else if (pressure > 500)
		{
			spriteSync = 2;
		}
		else if (pressure > 1000)
		{
			spriteSync = 3;
		}
		else if (pressure > 2000)
		{
			spriteSync = 4;
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
			return false;
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
			return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if(anchored)
		{
			SoundManager.PlayNetworkedAtPos("Wrench", registerTile.WorldPositionServer, 1f);
			Detach();
		}
		else
		{
			var foundPipes = MatrixManager.GetAt<Pipe>(registerTile.WorldPositionServer, true);
			for (int i = 0; i < foundPipes.Count; i++)
			{
				Pipe foundPipe = foundPipes[i];
				if(foundPipe.anchored)
				{
					SoundManager.PlayNetworkedAtPos("Wrench", registerTile.WorldPositionServer, 1f);
					pipe = foundPipe;
					ToggleAnchored(true);
					UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
					UpdateMe();
					break;
				}
			}
		}
	}

	public void Detach()
	{
		ToggleAnchored(false);
		spriteSync = 0;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	void ToggleAnchored(bool value)
	{
		objectBehaviour.ServerSetPushable(!value);
		anchored = value;
	}

	public void SyncSprite(int oldValue, int value)
	{
		spriteRenderer.sprite = spriteList[value];
	}

}