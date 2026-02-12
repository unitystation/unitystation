using System.Collections;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers.NetworkManagement;
using US13.MapSaver;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;

namespace US13.Items.Others
{
	public class EtherealThing : MonoBehaviour, IServerSpawn
	{

		public Pickupable Pickupable;


		private bool InIted = false;

		public void OnSpawnServer(SpawnInfo info)
		{

			Pickupable = this.GetComponent<Pickupable>();
			if (this.gameObject.activeInHierarchy == false) return;
			if (this.GetComponent<RuntimeSpawned>() == null)
			{
				StartCoroutine(WaitingFrame());
			}
			else
			{
				StartCoroutine(WaitingFrame());
			}
		}

		public void Start()
		{
			if (this.GetComponent<RuntimeSpawned>() == null)
			{
				StartCoroutine(WaitingFrame());
			}
		}

		private IEnumerator WaitingFrame()
		{
			if (InIted)
			{
				yield break;
			}

			InIted = true;
			yield return null;


			if (CustomNetworkManager.IsServer)
			{
				if (Pickupable != null && Pickupable.ItemSlot != null)
				{
					Inventory.ServerDrop(Pickupable.ItemSlot); //TOOD Handle inventory sometime
				}
			}

			var RegisterTile = this.GetComponent<RegisterTile>();
			RegisterTile.Matrix.MetaDataLayer.EtherealThings.Add(this);
		}
	}
}
