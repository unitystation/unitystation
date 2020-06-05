using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class FireAlarm : MonoBehaviour, IServerLifecycle, ICheckedInteractable<HandApply>
{
	public List<FireLock> FireLockList = new List<FireLock>();
	private MetaDataNode metaNode;
	public bool activated = false;
	public float coolDownTime = 1.0f;
	public bool isInCooldown = false;

	public SpriteHandler spriteHandler;
	public Sprite topLightSpriteNormal;
	public Sprite openEmptySprite;
	public Sprite openCabledSprite;
	public SpriteSheetAndData topLightSpriteAlert;

	public bool coverOpen;
	public bool hasCables = true;



	public void SendCloseAlerts()
	{
		if (!hasCables)
			return;
		if (!activated && !isInCooldown)
		{
			activated = true;
			spriteHandler.SetSprite(topLightSpriteAlert, 0);
			SoundManager.PlayNetworkedAtPos("FireAlarm", metaNode.Position);
			StartCoroutine(SwitchCoolDown());
			foreach (var firelock in FireLockList)
			{
				firelock.ReceiveAlert();
			}
		}
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		var integrity = GetComponent<Integrity>();
		integrity.OnExposedEvent.AddListener(SendCloseAlerts);
		AtmosManager.Instance.inGameFireAlarms.Add(this);
		RegisterTile registerTile = GetComponent<RegisterTile>();
		MetaDataLayer metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
		var wallMount = GetComponent<WallmountBehavior>();
		var direction = wallMount.CalculateFacing().CutToInt();
		metaNode = metaDataLayer.Get(registerTile.LocalPositionServer + direction, false);
		foreach (var firelock in FireLockList)
		{
			firelock.fireAlarm = this;
		}
		if (!info.SpawnItems)
		{
			hasCables = false;
			coverOpen = true;
			spriteHandler.SetSprite(openEmptySprite);
		}

	}

	public void TickUpdate()
	{
		if (!activated)
		{
			if (metaNode.GasMix.Pressure < AtmosConstants.WARNING_LOW_PRESSURE || metaNode.GasMix.Pressure > AtmosConstants.WARNING_HIGH_PRESSURE)
			{
				SendCloseAlerts();
			}
		}
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		AtmosManager.Instance.inGameFireAlarms.Remove(this);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.Intent == Intent.Harm) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
		{
			if (coverOpen)
			{
				coverOpen = false;
				if (activated)
				{
					spriteHandler.SetSprite(topLightSpriteAlert, 0);
				}
				else
				{
					spriteHandler.SetSprite(topLightSpriteNormal);
				}
			}
			else
			{
				coverOpen = true;
				if (hasCables)
				{
					spriteHandler.SetSprite(openCabledSprite);
				}
				else
				{
					spriteHandler.SetSprite(openEmptySprite);
				}
			}
			SoundManager.PlayNetworkedAtPos("screwdriver1", interaction.Performer.WorldPosServer());
			return;
		}
		if (coverOpen)
		{
			if (hasCables && Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wirecutter))
			{
				//cut out cables
				Chat.AddActionMsgToChat(interaction, $"You remove the cables.",
					$"{interaction.Performer.ExpensiveName()} removes the cables.");
				ToolUtils.ServerPlayToolSound(interaction);
				Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), 5);
				spriteHandler.SetSprite(openEmptySprite);
				hasCables = false;
				activated = false;
				return;
			}

			if (!hasCables && Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Cable) &&
			    Validations.HasUsedAtLeast(interaction, 5))
			{
				//add 5 cables
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start adding cables to the frame...",
					$"{interaction.Performer.ExpensiveName()} starts adding cables to the frame...",
					"You add cables to the frame.",
					$"{interaction.Performer.ExpensiveName()} adds cables to the frame.",
					() =>
					{
						Inventory.ServerConsume(interaction.HandSlot, 5);
						hasCables = true;
						spriteHandler.SetSprite(openCabledSprite);
					});
			}

		}
		else
		{
			if (activated && !isInCooldown)
			{
				activated = false;
				spriteHandler.SetSprite(topLightSpriteNormal);
				StartCoroutine(SwitchCoolDown());
				foreach (var firelock in FireLockList)
				{
					if (firelock.Controller.IsClosed)
					{
						firelock.Controller.ServerOpen();
					}
				}
			}
			else
			{
				SendCloseAlerts();
			}
		}

	}

	//Copied over from LightSwitchV2.cs
	void OnDrawGizmosSelected()
	{
		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;

		//Highlighting all controlled FireLocks
		Gizmos.color = new Color(1, 0.5f, 0, 1);
		for (int i = 0; i < FireLockList.Count; i++)
		{
			var FireLock = FireLockList[i];
			if(FireLock == null) continue;
			Gizmos.DrawLine(sprite.transform.position, FireLock.transform.position);
			Gizmos.DrawSphere(FireLock.transform.position, 0.25f);
		}
	}

	private IEnumerator SwitchCoolDown()
	{
		isInCooldown = true;
		yield return WaitFor.Seconds(coolDownTime);
		isInCooldown = false;
	}
}

