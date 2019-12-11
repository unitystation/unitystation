using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class OreRedemptionMachine : MonoBehaviour, IInteractable<HandApply>
{
	public List<OreToMaterial> ExpectedOres;
	 
	public SpriteHandler SpriteHandler;

	private RegisterObject RegisterObject;

	public void OnEnable()
	{
		RegisterObject = GetComponent<RegisterObject>();
	}

    void Start()
    {
		SpriteHandler.PushTexture();
    }

	public void ServerPerformInteraction(HandApply interaction)
	{
		Logger.Log("1");// 
		var localPosInt = MatrixManager.Instance.WorldToLocalInt(RegisterObject.WorldPositionServer, RegisterObject.Matrix);
		var OreItems = RegisterObject.Matrix.Get<ItemAttributesV2>(localPosInt + Vector3Int.up, true);

		foreach (var Ore in OreItems) {
			Logger.Log("2" + Ore.ToString());
			foreach (var exOre in ExpectedOres) {
				Logger.Log("3");
				if (Ore != null)
				{
					if (Ore.HasTrait(exOre.Tray))
					{
						Logger.Log("4");
						var inStackable = Ore.gameObject.GetComponent<Stackable>();
						Spawn.ServerPrefab(exOre.Material, RegisterObject.WorldPositionServer + Vector3Int.down, transform.parent, count: inStackable.Amount );
						Despawn.ServerSingle(Ore.transform.gameObject);
						continue;
					}
				}
			}
		}
	}
}

[Serializable]
public class OreToMaterial {
	public ItemTrait Tray;
	public GameObject Material;
}
