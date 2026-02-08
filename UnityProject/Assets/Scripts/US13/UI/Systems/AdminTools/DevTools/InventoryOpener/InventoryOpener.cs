using System.Linq;
using Shared.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using US13.Core.Input_System;
using US13.Managers.UpdateManager;
using US13.Messages.Client.Admin;
using US13.Systems.Inventory;
using US13.UI.Core;
using Util;

namespace US13.UI.Systems.AdminTools.DevTools.InventoryOpener
{
	[RequireComponent(typeof(EscapeKeyTarget))]
	public class InventoryOpener : SingletonManager<InventoryOpener>
	{
		public Button StopSelectingButton;
		// so we can escape while drawing - enabled while drawing, disabled when done
		private EscapeKeyTarget escapeKeyTarget;
		public bool Updating = false;
		private bool cachedLightingState;

		public Toggle OnlyUseOccupied;

		private void OnEnable()
		{
			escapeKeyTarget = GetComponent<EscapeKeyTarget>();
		}

		public override void Start()
		{
			base.Start();
			this.gameObject.SetActive(false);
		}

		private void OnDisable()
		{
			OnEscape();
			if (Updating)
			{
				UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
				Updating = false;
			}
		}

		private void UpdateMe()
		{
			if (CommonInput.GetMouseButtonDown(0))
			{
				//Ignore spawn if pointer is hovering over GUI
				if (EventSystem.current.IsPointerOverGameObject())
				{
					return;
				}


				var PressedObject = MouseUtils.GetOrderedObjectsUnderMouse(null,
						go => go.GetComponent<ItemStorage>() != null)
					.FirstOrDefault()?.GetComponent<ItemStorage>();

				if (PressedObject == null)
				{
					return;
				}

				AdminRequestInventories.Send(PressedObject, OnlyUseOccupied.isOn);

			}

		}


		public void CloseButton()
		{
			this.gameObject.SetActive(false);
		}

		public void OnEscape()
		{
			//stop drawing
			if (Updating)
			{
				UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
				Updating = false;
			}

			UIManager.IsMouseInteractionDisabled = false;
			if (escapeKeyTarget == null) return;
			escapeKeyTarget.enabled = false;
			if (Camera.main.OrNull()?.GetComponent<LightingSystem>() != null)
			{
				Camera.main.GetComponent<LightingSystem>().enabled = cachedLightingState;
			}

			StopSelectingButton.interactable = false;

		}

		[NaughtyAttributes.Button]
		public void OnSelected()
		{
			StopSelectingButton.interactable = true;
			UIManager.IsMouseInteractionDisabled = true;
			escapeKeyTarget.enabled = true;
			cachedLightingState = Camera.main.GetComponent<LightingSystem>().enabled;
			Camera.main.GetComponent<LightingSystem>().enabled = false;
			if (Updating == false)
			{
				UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
				Updating = true;
			}
		}
	}
}
