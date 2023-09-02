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

		private IEnumerator OnEnable()
		{
			//(Max): For some reason, the spawning times will become inconsistent between frames.
			//To play it say, we intentionally await 2 full seconds until everything has run its course.
			yield return WaitFor.Seconds(2f);
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
			var handslot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot();
			if(handslot == null || handslot.IsEmpty || handslot.ItemAttributes.GetTraits().Count() == 0) yield break;
			foreach (var trait in handslot.ItemAttributes.GetTraits())
			{
				if(protipsForTraits.ContainsKey(trait) == false) continue;
				TriggerTip(protipsForTraits[trait], null, highlightableObjectNames);
			}
		}

		private IEnumerator SetupEvents()
		{
			// Await a frame for everything to be initialised properly
			yield return WaitFor.EndOfFrame;
			if (PlayerManager.LocalPlayerScript == null)
			{
				Loggy.LogError("[Protips] - Something went wrong accessing the player's local player script.. Are you sure everything is setup correctly?", Category.Character);
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
				Loggy.LogWarning("[Protips] - Attempted to unsubscribe an event while the player script is null, player might have become a ghost or deleted.");
			}

		}
	}
}