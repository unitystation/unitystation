﻿using PlayGroup;
using PlayGroups.Input;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Items
{
	public class PickUpTrigger : InputTrigger
	{
		private void Start()
		{
			CheckSpriteOrder();
		}

		public override void Interact(GameObject originator, Vector3 position, string hand)
		{
			if (originator.GetComponent<PlayerScript>().canNotInteract())
			{
				return;
			}

			if (!isServer)
			{
				UISlotObject uiSlotObject = new UISlotObject(hand, gameObject);

				//PreCheck
				if (UIManager.CanPutItemToSlot(uiSlotObject))
				{
					//Simulation
					gameObject.GetComponent<CustomNetTransform>().DisappearFromWorld();
					//                    UIManager.UpdateSlot(uiSlotObject);

					//Client informs server of interaction attempt
					InteractMessage.Send(gameObject, hand);
				}
			}
			else
			{
				//Server actions
				if (!ValidatePickUp(originator, hand))
				{
					//Rollback prediction (inform player about item's true state)
					GetComponent<CustomNetTransform>().NotifyPlayer(originator);
				}
			}
		}

		[Server]
		public bool ValidatePickUp(GameObject originator, string handSlot = null)
		{
			var ps = originator.GetComponent<PlayerScript>();
			var slotName = handSlot ?? UIManager.Hands.CurrentSlot.eventName;
			var cnt = GetComponent<CustomNetTransform>();
			var state = cnt.ServerState;
			if (SlotUnavailable(ps, slotName))
			{
				return false;
			}
			if (cnt.IsFloatingServer ? !CanReachFloating(ps, state) : !ps.IsInReach(state.WorldPosition))
			{
				Logger.LogWarning($"Not in reach! server pos:{state.WorldPosition} player pos:{originator.transform.position} (floating={cnt.IsFloatingServer})", Category.Security);
				return false;
			}
			Logger.LogTraceFormat("Pickup success! server pos:{0} player pos:{1} (floating={2})", Category.Security, 
				state.WorldPosition, originator.transform.position, cnt.IsFloatingServer);


			//set ForceInform to false for simulation
			return ps.playerNetworkActions.AddItem(gameObject, slotName, false /*, false*/);
		}

		/// <summary>
		///     Making reach check less strict when object is flying, otherwise high ping players can never catch shit!
		/// </summary>
		private static bool CanReachFloating(PlayerScript ps, TransformState state)
		{
			return ps.IsInReach(state.WorldPosition) || ps.IsInReach(state.WorldPosition - (Vector3) state.Impulse, 2f);
		}

		private static bool SlotUnavailable(PlayerScript ps, string slotName)
		{
			return ps == null
			       || !ps.playerNetworkActions.Inventory.ContainsKey(slotName)
			       || ps.playerNetworkActions.SlotNotEmpty(slotName);
		}

		/// <summary>
		///     If a SpriteRenderer.sortingOrder is 0 then there will be difficulty
		///     interacting with the object via the InputTrigger especially when placed on
		///     tables. This method makes sure that it is never 0 on start
		/// </summary>
		private void CheckSpriteOrder()
		{
			SpriteRenderer sR = GetComponentInChildren<SpriteRenderer>();
			if (sR != null)
			{
				if (sR.sortingLayerName == "Items" && sR.sortingOrder == 0)
				{
					sR.sortingOrder = 1;
				}
			}
		}
	}
}