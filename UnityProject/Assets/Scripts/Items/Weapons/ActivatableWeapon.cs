using System;
using Core.Utils;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

namespace Weapons.ActivatableWeapons
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
