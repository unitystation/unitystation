using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Clothing;
using Systems.Explosions;
using UI.Action;

namespace Items.Others
{
	[RequireComponent(typeof(ItemLightControl))]
	public class FlashLight : NetworkBehaviour, ICheckedInteractable<HandActivate>, IEmpAble
	{
		[Tooltip("The SpriteHandler this flashlight type should use when setting the on/off sprite.")]
		[SerializeField]
		private SpriteHandler spriteHandler = default;

		// The light the flashlight has access to
		private ItemLightControl lightControl;
		private ItemActionButton actionButton;

		protected bool IsOn => lightControl.IsOn;
		protected int SpriteIndex => IsOn ? 1 : 0;
		protected bool HasActionButton => actionButton != null;

		#region Lifecycle

		private void Awake()
		{
			lightControl = GetComponent<ItemLightControl>();
			actionButton = GetComponent<ItemActionButton>();
		}

		private void OnEnable()
		{
			if (HasActionButton)
			{
				actionButton.ClientActionClicked += ClientUpdateActionSprite;
				actionButton.ServerActionClicked += ToggleLight;
			}
		}

		private void OnDisable()
		{
			if (HasActionButton)
			{
				actionButton.ClientActionClicked -= ClientUpdateActionSprite;
				actionButton.ServerActionClicked -= ToggleLight;
			}
		}

		#endregion Lifecycle

		#region Interaction

		/// <summary>
		/// Checks to make sure the player is conscious and stuff
		/// </summary>
		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		/// <summary>
		/// Toggles the light on and off depending on the "IsOn" bool from the "ItemLightControl" component
		/// </summary>
		public void ServerPerformInteraction(HandActivate interaction)
		{
			ToggleLight();
		}

		#endregion Interaction

		protected virtual void ClientUpdateActionSprite()
		{
			spriteHandler.ChangeSprite(IsOn ? 0 : 1);
		}

		protected virtual void ToggleLight()
		{
			lightControl.Toggle(!lightControl.IsOn);
			spriteHandler.ChangeSprite(SpriteIndex);

			if (TryGetComponent<ClothingV2>(out var clothing))
			{
				clothing.ChangeSprite(lightControl.IsOn ? 1 : 0);
			}
		}

		public void OnEmp(int EmpStrength = 0)
		{
			if (lightControl.IsOn)
			{
				ToggleLight();
			}
		}
	}
}
