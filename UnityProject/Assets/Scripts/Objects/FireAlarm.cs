using System.Collections;
using System.Collections.Generic;
using Electric.Inheritance;
using Mirror;
using UnityEngine;

public class FireAlarm : SubscriptionController, IServerLifecycle, ICheckedInteractable<HandApply>, ISetMultitoolMaster
{
	public List<FireLock> FireLockList = new List<FireLock>();
	private MetaDataNode metaNode;
	public bool activated = false;
	public float coolDownTime = 1.0f;
	public bool isInCooldown = false;

	[SyncVar(hook = nameof(SyncSprite))] private FireAlarmState stateSync;
	public SpriteHandler spriteHandler;
	public Sprite topLightSpriteNormal;
	public Sprite openEmptySprite;
	public Sprite openCabledSprite;
	public SpriteDataSO topLightSpriteAlert;

	public bool coverOpen;
	public bool hasCables = true;

	[SerializeField]
	private MultitoolConnectionType conType = MultitoolConnectionType.FireAlarm;
	public MultitoolConnectionType ConType  => conType;

	private bool multiMaster = true;
	public bool MultiMaster => multiMaster;

	public void AddSlave(object SlaveObject)
	{
	}

	public enum FireAlarmState
	{
		TopLightSpriteAlert,
		OpenEmptySprite,
		TopLightSpriteNormal,
		OpenCabledSprite
	};


	public void SendCloseAlerts()
	{
		if (!hasCables)
			return;
		if (!activated && !isInCooldown)
		{
			activated = true;
			stateSync = FireAlarmState.TopLightSpriteAlert;
			SoundManager.PlayNetworkedAtPos("FireAlarm", metaNode.Position);
			StartCoroutine(SwitchCoolDown());
			foreach (var firelock in FireLockList)
			{
				if (firelock == null) continue;
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
			stateSync = FireAlarmState.OpenEmptySprite;
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
					stateSync = FireAlarmState.TopLightSpriteAlert;
				}
				else
				{
					stateSync = FireAlarmState.TopLightSpriteNormal;
				}
			}
			else
			{
				coverOpen = true;
				if (hasCables)
				{
					stateSync = FireAlarmState.OpenCabledSprite;
				}
				else
				{
					stateSync = FireAlarmState.OpenEmptySprite;
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
				stateSync = FireAlarmState.OpenEmptySprite;
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
						stateSync = FireAlarmState.OpenCabledSprite;
					});
			}

		}
		else
		{
			if (activated && !isInCooldown)
			{
				activated = false;
				stateSync = FireAlarmState.TopLightSpriteNormal;
				StartCoroutine(SwitchCoolDown());
				foreach (var firelock in FireLockList)
				{
					if(firelock == null) continue;
					var controller = firelock.Controller;
					if (controller == null) continue;

					controller.TryOpen();
				}
			}
			else
			{
				SendCloseAlerts();
			}
		}

	}

	private IEnumerator SwitchCoolDown()
	{
		isInCooldown = true;
		yield return WaitFor.Seconds(coolDownTime);
		isInCooldown = false;
	}

	public void SyncSprite(FireAlarmState stateOld, FireAlarmState stateNew)
	{
		stateSync = stateNew;
		if (stateNew == FireAlarmState.TopLightSpriteAlert)
		{

			spriteHandler.SetSpriteSO(topLightSpriteAlert);
		}
		else if (stateNew == FireAlarmState.OpenEmptySprite)
		{
			spriteHandler.SetSprite(openEmptySprite);
		}
		else if (stateNew == FireAlarmState.TopLightSpriteNormal)
		{
			spriteHandler.SetSprite(topLightSpriteNormal);
		}
		else if (stateNew == FireAlarmState.OpenCabledSprite)
		{
			spriteHandler.SetSprite(openCabledSprite);
		}
	}

	#region Editor

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

	public override IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
	{
		var approvedObjects = new List<GameObject>();

		foreach (var potentialObject in potentialObjects)
		{
			var fireLock = potentialObject.GetComponent<FireLock>();
			if (fireLock == null) continue;
			AddFireLockFromScene(fireLock);
			approvedObjects.Add(potentialObject);
		}

		return approvedObjects;
	}

	private void AddFireLockFromScene(FireLock fireLock)
	{
		if (FireLockList.Contains(fireLock))
		{
			FireLockList.Remove(fireLock);
			fireLock.fireAlarm = null;
		}
		else
		{
			FireLockList.Add(fireLock);
			fireLock.fireAlarm = this;
		}
	}

	#endregion
}
