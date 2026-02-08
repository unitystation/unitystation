using System;
using Mirror;
using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Utils;

namespace US13.Items.Weapons
{
	public class ActivatableWeapon : NetworkBehaviour, ICheckedInteractable<HandActivate>
	{
		[SyncVar(hook = nameof(SyncState))] private bool isActive = false;

		public bool IsActive => isActive;

		public Action<GameObject> ServerOnActivate;
		public Action<GameObject> ServerOnDeactivate;

		public Action ClientOnActivate;
		public Action ClientOnDeactivate;

		public MultiInterestBool canActivate = new(true,
			MultiInterestBool.RegisterBehaviour.RegisterFalse,
			MultiInterestBool.BoolBehaviour.ReturnOnFalse);

		public MultiInterestBool canDeactivate = new(true,
			MultiInterestBool.RegisterBehaviour.RegisterFalse,
			MultiInterestBool.BoolBehaviour.ReturnOnFalse);

		public void SyncState(bool oldState, bool newState)
		{
			isActive = newState;

			if (isActive)
			{
				ClientOnActivate?.Invoke();
			}
			else
			{
				ClientOnDeactivate?.Invoke();
			}
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (isActive && canDeactivate)
			{
				ServerOnDeactivate?.Invoke(interaction.Performer);
				SyncState(isActive, !isActive);
			}
			else if (canActivate)
			{
				ServerOnActivate?.Invoke(interaction.Performer);
				SyncState(isActive, !isActive);
			}
		}
	}
}
