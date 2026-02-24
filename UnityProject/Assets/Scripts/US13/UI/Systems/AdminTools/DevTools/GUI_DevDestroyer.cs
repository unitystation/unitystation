using System.Linq;
using UnityEngine;
using US13.Core.Input_System;
using US13.Core.Lifecycle;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Messages.Client.DevSpawner;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;


namespace US13.UI.Systems.AdminTools.DevTools
{
	/// <summary>
	/// Main logic for the UI for destroying objects
	/// </summary>
	public class GUI_DevDestroyer : MonoBehaviour
	{
		// destroyable objects
		private LayerMask layerMask;
		private LightingSystem lightingSystem;

		private bool cachedLightingState;

		void Awake()
		{
			layerMask = LayerMask.GetMask("Furniture", "Machines", "Unshootable Machines", "Items",
				"Objects");
			lightingSystem = Camera.main.GetComponent<LightingSystem>();
		}

		private void OnEnable()
		{
			cachedLightingState = lightingSystem.enabled;
			lightingSystem.enabled = false;
			UIManager.IsMouseInteractionDisabled = true;
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			lightingSystem.enabled = cachedLightingState;
			UIManager.IsMouseInteractionDisabled = false;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			// check which objects we are over, pick the top one to delete
			if (CommonInput.GetMouseButtonDown(0))
			{
				var hits = MouseUtils.GetOrderedObjectsUnderMouse(layerMask,
					go => go.GetComponent<UniversalObjectPhysics>() != null, useMappedItems : DevCameraControls.Instance.MappingItemState).ToArray();
				if (hits.Any() == false) return;
				var target = hits.First().GetComponentInParent<UniversalObjectPhysics>()?.gameObject;
				if (target == null) return;
				if (CustomNetworkManager.IsServer)
				{
					_ = Despawn.ServerSingle(target);
				}
				else
				{
					DevDestroyMessage.Send(target);
				}
			}
		}
	}
}
