using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ExosuitFabricator : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[SyncVar(hook = nameof(SyncSprite))]
	private ExosuitFabricatorState stateSync;

	[SerializeField] private SpriteHandler spriteHandler;
	[SerializeField] private SpriteSheetAndData idleSprite;
	[SerializeField] private SpriteSheetAndData productionSprite;
	private RegisterObject registerObject;

	public enum ExosuitFabricatorState
	{
		Idle,
		Production,
	};

	public void OnEnable()
	{
		registerObject = GetComponent<RegisterObject>();
	}

	public override void OnStartClient()
	{
		SyncSprite(stateSync, stateSync);
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		Debug.Log("Clicking on Exosuit fab");
		if (stateSync != ExosuitFabricatorState.Production)
		{
			Debug.Log("Production start");
			stateSync = ExosuitFabricatorState.Production;
			StartCoroutine(ServerProcessProduction());
		}
	}

	private IEnumerator ServerProcessProduction()
	{
		yield return WaitFor.Seconds(10f);

		Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, registerObject.WorldPositionServer + Vector3Int.down, transform.parent, count: 1);

		stateSync = ExosuitFabricatorState.Idle;

		Debug.Log("Production End");
	}

	public void SyncSprite(ExosuitFabricatorState stateOld, ExosuitFabricatorState stateNew)
	{
		stateSync = stateNew;
		if (stateNew == ExosuitFabricatorState.Idle)
		{
			spriteHandler.SetSprite(idleSprite);
		}
		else if (stateNew == ExosuitFabricatorState.Production)
		{
			spriteHandler.SetSprite(productionSprite, 0);
		}
		else
		{
			//Do nothing
		}
	}

	//Needs implementation
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return true;
	}
}