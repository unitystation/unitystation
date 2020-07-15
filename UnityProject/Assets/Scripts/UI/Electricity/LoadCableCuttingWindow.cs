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

	private void Update()
	{
		// disable window if distance is greater than interaction distance
		if (isWindowActive && Vector2.Distance(localPlayerTransform.position, targetWorldPosition) > PlayerScript.interactionDistance)
			CloseCableCuttingWindow();
	}

	private void OnEnable()
	{
		// store reference to player transform
		localPlayerTransform = PlayerManager.LocalPlayer.transform;
	}

	/// <summary>
	/// Load/enable cable cutting window and initialize it
	/// </summary>
	public void OpenCableCuttingWindow()
	{
		// get mouse position
		Vector3 mousePosition = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
		// round mouse position
		Vector3Int roundedMousePosition = Vector3Int.RoundToInt(mousePosition);

		GameObject hit = MouseUtils.GetOrderedObjectsUnderMouse().FirstOrDefault();
		MetaTileMap metaTileMap = hit.GetComponentInChildren<MetaTileMap>();

		Vector3Int cellPosition = metaTileMap.WorldToCell(mousePosition);

		// get matrix
		MatrixInfo matrixInfo = MatrixManager.AtPoint(roundedMousePosition, false);
		Matrix matrix = matrixInfo.Matrix;
		// get connections at target cell position
		List<IntrinsicElectronicData> conns = matrix.GetElectricalConnections(cellPosition);

		// return if thera are no electrical connections
		if (conns.Count < 1) return;

		// if window exist, just initialize it
		if (cableCuttingWindow != null)
		{
			cableCuttingWindow.InitializeCableCuttingWindow(matrix, cellPosition, mousePosition);
		}
		// else, load window from resources, store reference and initialize
		else
		{
			GameObject windowPrefab = Resources.Load<GameObject>(PATH_TO_WINDOW_PREFAB);
			cableCuttingWindow = Instantiate(windowPrefab).GetComponentInChildren<CableCuttingWindow>();
			cableCuttingWindow.InitializeCableCuttingWindow(matrix, cellPosition, mousePosition);
		}

		cableCuttingWindow.SetWindowActive(true);

		isWindowActive = true;
		targetWorldPosition = roundedMousePosition;
	}

	/// <summary>
	/// Disable cable cutting window
	/// </summary>
	public void CloseCableCuttingWindow()
	{
		cableCuttingWindow.SetWindowActive(false);

		isWindowActive = false;
	}
}
