using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Main logic for the UI for destroying objects
/// </summary>
public class GUI_DevDestroyer : MonoBehaviour
{

	// destroyable objects
	private LayerMask layerMask;
	private LightingSystem lightingSystem;

	void Awake()
	{
		layerMask = LayerMask.GetMask("Furniture", "Machines", "Unshootable Machines", "Items",
			"Objects");
		lightingSystem = Camera.main.GetComponent<LightingSystem>();
	}

	private void OnEnable()
	{
		lightingSystem.enabled = false;
		UIManager.IsMouseInteractionDisabled = true;
	}

	private void OnDisable()
	{
		lightingSystem.enabled = true;
		UIManager.IsMouseInteractionDisabled = false;
	}

	private void Update()
	{
		//check which objects we are over, pick the top one to delete
		if (CommonInput.GetMouseButtonDown(0))
		{
			var hits = MouseUtils.GetOrderedObjectsUnderMouse(layerMask,
				go => go.GetComponent<CustomNetTransform>() != null);
			if (hits.Any())
			{
				if (CustomNetworkManager.IsServer)
				{
					Despawn.ServerSingle(hits.First().GetComponentInParent<CustomNetTransform>().gameObject);
				}
				else
				{
					DevDestroyMessage.Send(hits.First().GetComponentInParent<CustomNetTransform>().gameObject,
						ServerData.UserID, PlayerList.Instance.AdminToken);
				}
			}
		}
	}
}
