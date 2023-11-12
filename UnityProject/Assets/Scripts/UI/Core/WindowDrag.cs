using System;
using UnityEngine;

public class WindowDrag : MonoBehaviour
{
	/// <summary>
	/// Disable ability to drag the window
	/// </summary>
	public bool disableDrag = false;

	private float offsetX;
	private float offsetY;
	private Vector3 startPositon;
	private RectTransform rectTransform;
	private bool isReady = false;

	/// <summary>
	/// Calculates and sets the initial window start position relative to the screen size.
	/// Tells the OnRectTransformDimensionsChange() that this window object is set up and "isReady" to be clamped
	/// within it's bounds.
	/// </summary>
	private void Start ()
	{
		rectTransform = GetComponent<RectTransform>();
		startPositon = transform.localPosition;

		isReady = true;
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		UIManager.PreventChatInput = true;
	}

	public void UpdateMe()
	{
		if (CustomNetworkManager.IsHeadless)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			return;
		}

		if (KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ResetWindowPosition))
		{
			this.transform.localPosition = Vector3.zero;
		}
	}

	/// <summary>
	/// Resets the window to its start position relative to the screen size.
	/// </summary>
	private void OnDisable ()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);

		transform.localPosition = startPositon;
		UIManager.PreventChatInput = false;
	}

	/// <summary>
	/// Sets the windowDrag fields offsetX and offsetY from the window position and the mouse position.
    /// The fields offsetX and offsetY are the mouse position's offset from the window's top-left corner.
	/// In onDrag(), these offsets are used to "hook" the window to the cursor as it is dragged.
	/// </summary>
	public void BeginDrag()
	{
		if (disableDrag) return;

		var windowTransformPosition = transform.position;

		offsetX = windowTransformPosition.x - CommonInput.mousePosition.x;
		offsetY = windowTransformPosition.y - CommonInput.mousePosition.y;
	}

	/// <summary>
	/// Moves the window with the cursor within the screen bounds when called.
	/// </summary>
	public void OnDrag()
	{
		if (disableDrag) return;

		ClampWindowPosition(offsetX + CommonInput.mousePosition.x, offsetY + CommonInput.mousePosition.y);
	}

	public virtual void DragEnd()
	{

	}

	/// <summary>
	/// Moves and Clamps the window.
	/// </summary>
	/// <param name="x">The window's X coordinate world position to be clamped.</param>
	/// <param name="y">The window's Y coordinate world position to be clamped.</param>
	private void ClampWindowPosition(float x, float y)
	{
		var windowSize = rectTransform.sizeDelta;
		var windowScale = rectTransform.lossyScale;

		var windowWidth = windowSize.x;
		var windowHeight = windowSize.y;

		var widthScale = windowScale.x;
		var heightScale = windowScale.y;

		transform.position = new Vector3(
			Mathf.Clamp(x,
				windowWidth * widthScale * -0.4f,
				Screen.width - windowWidth * widthScale * -0.4f),
			Mathf.Clamp(y,
				windowHeight * heightScale * -0.4f,
				Screen.height - windowHeight * heightScale * -0.4f));
	}

	/// <summary>
	/// Gets called when the resolution's width is thinned, as the window's RectTransform is thinned also.
	/// This does not get called when the resolution's height is shortened, as the window's RectTransform does not
	/// get shortened.
	/// As there is no event function for a resolution change, this is used as a workaround.
	/// </summary>
	private void OnRectTransformDimensionsChange()
	{
		if (!isReady)
		{
			return;
		}

		var windowPosition = transform.position;
		// Moves the window to it's current position, clamping the window within it's bounds in the process.
		ClampWindowPosition(windowPosition.x, windowPosition.y);
	}

}
