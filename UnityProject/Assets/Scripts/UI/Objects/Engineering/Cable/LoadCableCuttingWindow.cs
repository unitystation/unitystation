using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Class used to load, enable and disable CableCuttingWindow
/// </summary>
public class LoadCableCuttingWindow : MonoBehaviour
{
	/// <summary>
	/// Path to window prefab resource
	/// </summary>
	private const string PATH_TO_WINDOW_PREFAB = "CableCuttingWindow";

	/// <summary>
	/// Reference to cableCuttingWindow - instead of loading from resources every time, just enable and disable GameObject
	/// </summary>
	[SerializeField]
	private GameObject cableCuttingWindowPrefab;

	/// <summary>
	/// Reference to cableCuttingWindow - instead of loading from resources every time, just enable and disable GameObject
	/// </summary>
	private CableCuttingWindow cableCuttingWindow;

	private bool isWindowActive;

	/// <summary>
	/// Reference to local player transform, used to calculate distance
	/// </summary>
	private Transform localPlayerTransform;

	/// <summary>
	/// Position at which CableCuttingWindow was opened last time
	/// </summary>
	private Vector3 targetWorldPosition;

	/// <summary>
	/// Reference to last item in hand to detect when item has changed
	/// </summary>
	private GameObject itemInHand;

	private void Update()
	{
		// check only if window is active to not waste cpu time
		if (isWindowActive && !CanWindowBeEnabled())
			CloseCableCuttingWindow();
	}

	private void OnEnable()
	{
		// store reference to player transform
		localPlayerTransform = PlayerManager.LocalPlayer.transform;
	}

	/// <summary>
	/// Check if player can open cable cutting window (Itemtrait & distance)
	/// </summary>
	public bool CanWindowBeEnabled()
	{
		// check only if object in player's hand has changed
		// disable window if item is not wirecutter
		if (itemInHand != PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().ItemObject
			&& !Validations.HasItemTrait(PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().ItemObject, CommonTraits.Instance.Wirecutter))
		{
			return false;
		}
		// disable window if distance is greater than interaction distance
		else if (Vector2.Distance(localPlayerTransform.position, targetWorldPosition) > PlayerScript.interactionDistance)
			return false;

		return true;
	}

	/// <summary>
	/// Load/enable cable cutting window and initialize it
	/// </summary>
	public void OpenCableCuttingWindow()
	{
		// get mouse position
		Vector3 mousePosition = MouseUtils.MouseToWorldPos();
		// round mouse position
		Vector3Int roundedMousePosition = Vector3Int.RoundToInt(mousePosition);
		targetWorldPosition = roundedMousePosition;

		// check if window can be enabled, if not - return
		if (!CanWindowBeEnabled()) return;

		// get matrix
		GameObject hit = MouseUtils.GetOrderedObjectsUnderMouse().FirstOrDefault();
		Matrix matrix = hit.GetComponentInChildren<Matrix>();

		// return if matrix is null
		if (matrix == null) return;

		Vector3Int cellPosition = matrix.MetaTileMap.WorldToCell(mousePosition);

		// if window exist, just initialize it
		if (cableCuttingWindow != null)
		{
			cableCuttingWindow.InitializeCableCuttingWindow(matrix, cellPosition, mousePosition);
		}
		// else, load window from resources, store reference and initialize
		else
		{
			// only load from resources if the prefab is null
			if (cableCuttingWindowPrefab == null)
				cableCuttingWindowPrefab = Resources.Load<GameObject>(PATH_TO_WINDOW_PREFAB);
			cableCuttingWindow = Instantiate(cableCuttingWindowPrefab).GetComponentInChildren<CableCuttingWindow>();
			cableCuttingWindow.InitializeCableCuttingWindow(matrix, cellPosition, mousePosition);
		}

		// enable window
		cableCuttingWindow.SetWindowActive(true);

		isWindowActive = true;
		itemInHand = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().ItemObject;
	}

	/// <summary>
	/// Disable cable cutting window
	/// </summary>
	public void CloseCableCuttingWindow()
	{
		// disable window
		cableCuttingWindow.SetWindowActive(false);

		isWindowActive = false;
	}
}
