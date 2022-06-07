using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Chemistry;
using HealthV2;
using Items;
using System.Threading.Tasks;
using Mirror;

namespace Chemistry
{
	public class ChemSmoke : MonoBehaviour, IServerDespawn
	{
		public float InitialLifetime = 5000;

		private ReagentMix reagents;

		public ReagentMix Reagents
		{
			get { return reagents; }
			set { if (reagents == null && value != null) reagents = value; SyncReagents(); }
		}

		public Matrix Matrix;

		public void OnDespawnServer(DespawnInfo info)
        {
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, DeliverReagents);
        }

		private void SyncReagents()
        {
			if(reagents != null)
            {
				if (reagents.Total > 0)
				{
					gameObject.GetComponent<SpriteRenderer>().color = reagents.MixColor;
					DespawnAfter(InitialLifetime + reagents.Total * 1000);
					return;
				}
			}
			UpdateManager.Add(CallbackType.PERIODIC_UPDATE, DeliverReagents);
			DespawnAfter(InitialLifetime);
		}

		public bool HasGasMask(PlayerScript playerScript)
		{
			foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.mask))
			{
				if (itemSlot.Item == null) continue;
				if (itemSlot.Item.GetComponent<ItemAttributesV2>().HasTrait(CommonTraits.Instance.GasMask))
				{
					return true;
				}
			}
			return false;
		}

		private void DeliverReagents()
        {
			var players = Matrix.Get<PlayerScript>(gameObject.GetComponent<Transform>().position.RoundToInt(), true);
			foreach(PlayerScript player in players)
            {
				RespiratorySystemBase resp = player.gameObject.GetComponent<RespiratorySystemBase>();
				CirculatorySystemBase circ = player.gameObject.GetComponent<CirculatorySystemBase>();
				if (resp.GetInternalGasMix() == null && HasGasMask(player) == false && player.playerHealth.IsDead == false)
                {
					circ.BloodPool.Add(reagents);
                }
            }
        }

		public async Task DespawnAfter(float time)
		{
			await Task.Delay((int)time);
			Despawn.ServerSingle(gameObject);
		}
	}
}
