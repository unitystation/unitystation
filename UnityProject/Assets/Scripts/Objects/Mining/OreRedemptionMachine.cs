using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Items;
using UnityEngine.Serialization;

namespace Objects.Mining
{
	/// <summary>
	/// Causes object to consume ore on the tile above it and produce materials on the tile below it. Temporary
	/// until ORM UI is implemented.
	/// </summary>
	public class OreRedemptionMachine : MonoBehaviour, IInteractable<HandApply>
	{
		[FormerlySerializedAs("ExpectedOres")]
		[SerializeField]
		private List<OreToMaterial> expectedOres = null;

		private RegisterObject registerObject;

		public void OnEnable()
		{
			registerObject = GetComponent<RegisterObject>();
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var localPosInt = MatrixManager.Instance.WorldToLocalInt(registerObject.WorldPositionServer, registerObject.Matrix);
			var OreItems = registerObject.Matrix.Get<ItemAttributesV2>(localPosInt + Vector3Int.up, true);

			foreach (var Ore in OreItems)
			{
				foreach (var exOre in expectedOres)
				{
					if (Ore != null)
					{
						if (Ore.HasTrait(exOre.Trait))
						{
							var inStackable = Ore.gameObject.GetComponent<Stackable>();
							Spawn.ServerPrefab(exOre.Material, registerObject.WorldPositionServer + Vector3Int.down, transform.parent, count: inStackable.Amount);
							Despawn.ServerSingle(Ore.transform.gameObject);
						}
					}
				}
			}
		}
	}

	[Serializable]
	public class OreToMaterial
	{
		[FormerlySerializedAs("Tray")]
		public ItemTrait Trait;
		public GameObject Material;
	}
}
