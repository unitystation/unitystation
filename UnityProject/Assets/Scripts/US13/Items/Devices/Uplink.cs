using System.Collections;
using Mirror;
using NaughtyAttributes;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Lifecycle;
using US13.Items.PDA;
using US13.Items.Traits;
using US13.Player;
using US13.Systems.Inventory;
using Util;

namespace US13.Items.Devices
{
	/// <summary>
	/// Component that defines this item as an uplink device for traitors to receive access to the uplink shop.
	/// This handles transactions, access codes, and locking/unlocking the uplink as a sort of backend.
	/// You will need to hook the data and functionality of this component to a user-facing interface
	/// like an NtOS device, or GUI_PDAUplinkMenu (the PDA Tab Window).
	/// </summary>
	public class Uplink : NetworkBehaviour, IServerInventoryMove
	{
		/// ---- NOTE ----
		/// This component can probably be reworked to be more "universal" so that more antags can reuse this component
		/// instead of having to rewrite yet another way for handling their own spendable currencies, upgrade points, and
		/// antag shops.
		/// --------------
		

		[Tooltip("How long the delay before the owner is informed of the uplink code " +
		         "(intedned to reduce information overload - likely just received objectives)")]
		[SerializeField, BoxGroup("Uplink"), Range(0, 60)]
		private float informUplinkCodeDelay = 10;

		[SerializeField, BoxGroup("Uplink")]
		private bool debugUplink = false;

		public ItemTrait TeleCrystalTrait;
		public string UplinkUnlockCode { get; private set; }
		public bool IsUplinkCapable { get; private set; } = false;
		public bool IsUplinkLocked { get; private set; } = true;

		public bool IsNukeOps = false;

		/// <summary>
		/// The count of how many telecrystals this PDA has
		/// </summary>
		public int UplinkTC { get; set; }

		public static string GenerateUplinkUnlockCode()
		{
			var codeList = UplinkPasswordList.Instance.WordList;

			string code = codeList[Random.Range(0, codeList.Count)];

			string nums = Random.Range(111, 999).ToString();
			return code + nums;
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (info.ToRootPlayer == null) return;
			if (debugUplink)
			{
				InstallUplink(info.ToRootPlayer.PlayerScript.Mind, 80, true);
			}
		}

		public void InstallUplink(Mind player, int tcCount, bool isNukie)
		{
			UplinkTC = tcCount; // Add; if uplink installed again (e.g. via admin tools (player request more TC)).
			UplinkUnlockCode = GenerateUplinkUnlockCode();
			IsUplinkCapable = true;
			IsNukeOps = isNukie;
			StartCoroutine(DelayInformUplinkCode(player));
		}

		public void ControlUplinkLock(bool isLocked)
		{
			IsUplinkLocked = isLocked;
		}

		private IEnumerator DelayInformUplinkCode(Mind player)
		{
			// We delay the uplink code inform to reduce information overload (player was likely just given objectives)
			yield return WaitFor.Seconds(informUplinkCodeDelay);
			InformUplinkCode(player);
		}

		private void InformUplinkCode(Mind player)
		{
			var uplinkMessage =
				$"{(debugUplink ? "<b>UPLINK DEBUGGING ENABLED: </b>" : "")}" +
				$"</i>The Syndicate has cunningly disguised a <i>Syndicate Uplink</i> as your <i>{gameObject.ExpensiveName()}</i>. " +
				$"Simply enter the code <b>{UplinkUnlockCode}</b> into the ringtone select to unlock its hidden features.<i>";


			Chat.AddExamineMsgFromServer(player.gameObject, uplinkMessage);
		}

		/// <summary>
		/// Spawns the item requested by the uplink if there are enough TC.
		/// </summary>
		[Server]
		public bool SpawnUplinkItem(GameObject objectRequested, int cost)
		{
			if (!IsUplinkCapable || IsUplinkLocked) return false;

			if (cost > UplinkTC) return false;
			var pickupable = GetComponent<Pickupable>();
			var player = pickupable.ItemSlot.Player;
			var result = Spawn.ServerPrefab(objectRequested, player.WorldPosition, PrePickRandom: true);

			if (result.Successful == false) return false;
			UplinkTC -= cost;
			var item = result.GameObject;
			Inventory.ServerAdd(item, ItemSlot.GetBestSlotForPlayer(item, player));
			return true;
		}
	}
}