using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Items.Others
{
		[RequireComponent(typeof(ItemLightControl))]
		public class FlashLight : NetworkBehaviour, ICheckedInteractable<HandActivate>
	{
		// The light the flashlight has access to
		private ItemLightControl lightControl;

		[SerializeField]
		private SpriteHandler spriteHandler;

		[Tooltip("The ON sprite should be in the first element (That's element 0)")]
		[SerializeField] private Sprite [] toggleSprites;


		/// <summary>
		/// Grabs the "ItemLightControl" component from the current gameObject, used to toggle the light, only does this once
		/// </summary>
		private void Start()
		{
			lightControl= gameObject.GetComponent<ItemLightControl>();
		}

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
			bool isOn = lightControl.IsOn;
			if (isOn)
			{
				lightControl.Toggle(false);
				spriteHandler.SetSprite(toggleSprites[1]);
			}
			else
			{
				lightControl.Toggle(true);
				spriteHandler.SetSprite(toggleSprites[0]);
			}
		}
	}
}
