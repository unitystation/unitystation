using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using Logs;
using UnityEngine.Serialization;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnPickupTrait : ProtipObject
	{
		[FormerlySerializedAs("ProtipsForTraits"), SerializeField]
		private SerializableDictionary<ItemTrait, ProtipSO> protipsForTraits = new SerializableDictionary<ItemTrait, ProtipSO>();

		private void OnEnable()
		{
			StartCoroutine(SetupEvents());
		}

		private void OnDisable()
		{
			StartCoroutine(UnsubscribeFromEvents());
		}


		private void OnInventoryChange()
		{
			StartCoroutine(CheckHand());
		}

		private IEnumerator CheckHand()
		{
			yield return WaitFor.EndOfFrame;
			if (PlayerManager.Instance == null || PlayerManager.LocalPlayerScript == null
			                                   || PlayerManager.LocalPlayerScript.DynamicItemStorage == null) yield break;
			var handslot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot();
			if (handslot == null || handslot.IsEmpty || handslot.ItemAttributes.GetTraits().Count() == 0) yield break;
			foreach (var trait in handslot.ItemAttributes.GetTraits())
			{
				if(protipsForTraits.ContainsKey(trait) == false) continue;
				TriggerTip(protipsForTraits[trait], null, highlightableObjectNames);
			}
		}

		private IEnumerator SetupEvents()
		{
			//(Max): Wait until players join the round, since this is a client reference.
			yield return WaitFor.Seconds(80f);
			if (PlayerManager.LocalPlayerScript == null)
			{
				Loggy.Warning("[Protips] - Something went wrong accessing the player's local player script.. Are you sure that the player is in the round?", Category.Character);
				yield break;
			}

			if (PlayerManager.LocalPlayerScript.DynamicItemStorage == null)
			{
				Loggy.Error("To MAX Please fix"); // (Max): This error hasn't happened in ages, so I have no idea what to fix here.
				yield break;
			}

			PlayerManager.LocalPlayerScript.DynamicItemStorage.OnContentsChangeClient.AddListener(OnInventoryChange);
			// For the hosts and editor use, check if we're a headless server or not.
			if(CustomNetworkManager.IsHeadless == false) PlayerManager.LocalPlayerScript.
				DynamicItemStorage.OnContentsChangeServer.AddListener(OnInventoryChange);
		}

		private IEnumerator UnsubscribeFromEvents()
		{
			yield return WaitFor.EndOfFrame;
			// It's fine to do try catches on the client as it doesn't affect headless server performance.
			try
			{
				PlayerManager.LocalPlayerScript.DynamicItemStorage.OnContentsChangeClient.RemoveListener(OnInventoryChange);
				if(CustomNetworkManager.IsHeadless == false) PlayerManager.LocalPlayerScript.
					DynamicItemStorage.OnContentsChangeServer.RemoveListener(OnInventoryChange);
			}
			catch (Exception e)
			{
				Loggy.Warning("[Protips] - Attempted to unsubscribe an event while the player script is null, player might have become a ghost or deleted.");
			}

		}
	}
}