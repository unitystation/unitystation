using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Messages.Client.DeviceLinkMessage;
using Shared.Managers;
using Shared.Systems.ObjectConnection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(EscapeKeyTarget))]
public class DeviceRotator : SingletonManager<DeviceRotator>
{
	public Button StopSelectingButton;

	// so we can escape while drawing - enabled while drawing, disabled when done
	private EscapeKeyTarget escapeKeyTarget;

	private bool cachedLightingState;

	public GameGizmoLine CursorLine;

	public bool Updating = false;

	public Rotatable PressedObject = null;

	public OrientationEnum OrientationEnum = OrientationEnum.Default;

	public OrientationEnum OriginalDirection;

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


			PressedObject = MouseUtils.GetOrderedObjectsUnderMouse(null,
				go => go.GetComponent<Rotatable>() != null)
				.FirstOrDefault()?.GetComponent<Rotatable>();

			if (PressedObject == null)
			{
				return;
			}
			ColorUtility.TryParseHtmlString("#00F9FF", out var Colour);


			OriginalDirection = PressedObject.CurrentDirection;
			CursorLine = GameGizmomanager.AddNewLineStatic(PressedObject.gameObject, Vector3.zero,null ,
				MouseUtils.MouseToWorldPos(), Colour);
		}

		if (CursorLine != null && PressedObject != null)
		{

			var Orientation = ((MouseUtils.MouseToWorldPos() - PressedObject.transform.position).ToOrientationEnum());
			if (OrientationEnum != Orientation)
			{
				OrientationEnum = Orientation;
				PressedObject.FaceDirection(Orientation);
			}
			CursorLine.To = PressedObject.transform.position +(Orientation.ToLocalVector3());
			CursorLine.UpdateMe();
		}

		if (CommonInput.GetMouseButtonUp(0) && PressedObject != null)
		{
			CursorLine.OrNull()?.Remove();
			CursorLine = null;
			DeviceRotateMessage.Send(PressedObject.gameObject, (MouseUtils.MouseToWorldPos() - PressedObject.transform.position).ToOrientationEnum(), OriginalDirection);
			PressedObject = null;
			OriginalDirection = OrientationEnum.Default;
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

		if (PressedObject != null && OriginalDirection != OrientationEnum.Default)
		{
			PressedObject.FaceDirection(OriginalDirection);
		}

		PressedObject = null;
		UIManager.IsMouseInteractionDisabled = false;
		if (escapeKeyTarget == null) return;
		escapeKeyTarget.enabled = false;
		if (Camera.main.OrNull()?.GetComponent<LightingSystem>() != null)
		{
			Camera.main.GetComponent<LightingSystem>().enabled = cachedLightingState;
		}

		CleanupGizmos();
		StopSelectingButton.interactable = false;
	}

	public void CleanupGizmos()
	{
		if (CursorLine != null)
		{
			CursorLine.Remove();
		}
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