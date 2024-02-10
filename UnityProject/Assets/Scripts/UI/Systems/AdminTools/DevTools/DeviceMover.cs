using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Shared.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(EscapeKeyTarget))]
public class DeviceMover : SingletonManager<DeviceMover>
{
	//TODO on  appear at World position  reset stuff


    public Button StopSelectingButton;

    public Toggle RoundToggle;

	// so we can escape while drawing - enabled while drawing, disabled when done
	private EscapeKeyTarget escapeKeyTarget;

	public GameObject PressedObject = null;

	public GameGizmoLine CursorLine;

	public bool Updating = false;

	public Vector3 StartPositionWorld;

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
		if (CommonInput.GetMouseButtonDown(0) && PressedObject == null)
		{
			OnMouseDown();
		}

		if (CursorLine != null && PressedObject != null)
		{
			OnMousePositionUpdate();
		}

		if (CommonInput.GetMouseButtonUp(0) && PressedObject != null)
		{
			OnMouseButtonUp();
		}
	}

	public void OnMouseDown()
	{
		//Ignore spawn if pointer is hovering over GUI
		if (EventSystem.current.IsPointerOverGameObject()) return;


		PressedObject = MouseUtils.GetOrderedObjectsUnderMouse(  useMappedItems : DevCameraControls.Instance.MappingItemState).FirstOrDefault();
		if (KeyboardInputManager.IsAltActionKeyPressed() == false &&  PressedObject.TryGetComponent<UniversalObjectPhysics>(out var Physics) == false)
		{
			PressedObject = null;
			return;
		}


		if (PressedObject == null) return;


		StartPositionWorld = MouseUtils.MouseToWorldPos();
		ColorUtility.TryParseHtmlString("#1E00FF", out var Colour);
		CursorLine = GameGizmomanager.AddNewLineStatic(null, MouseUtils.MouseToWorldPos(), null,
			MouseUtils.MouseToWorldPos(), Colour);
	}

	public void OnMousePositionUpdate()
	{
		if (RoundToggle.isOn)
		{
			var WorldPosition = MouseUtils.MouseToWorldPos();
			var Matrix = WorldPosition.GetMatrixAtWorld();
			CursorLine.To = (WorldPosition.ToLocal(Matrix).RoundToInt().ToWorld(Matrix));
		}
		else
		{
			CursorLine.To = (MouseUtils.MouseToWorldPos());
		}

		CursorLine.UpdateMe();

	}

	public void OnMouseButtonUp()
	{
		CursorLine.OrNull()?.Remove();
		CursorLine = null;


		var ObjectPhysics = PressedObject.GetComponent<UniversalObjectPhysics>();
		if (ObjectPhysics != null)
		{
			if (RoundToggle.isOn)
			{
				var WorldPosition = MouseUtils.MouseToWorldPos();
				var Matrix = WorldPosition.GetMatrixAtWorld();
				DeviceMoverMessage.Send(ObjectPhysics.gameObject, WorldPosition.ToLocal(Matrix).RoundToInt().ToWorld(Matrix), null, Vector3.zero);
			}
			else
			{
				DeviceMoverMessage.Send(ObjectPhysics.gameObject, MouseUtils.MouseToWorldPos(), null, Vector3.zero);
			}
		}
		else
		{
			DeviceMoverMessage.Send(null, Vector3.zero, PressedObject, (MouseUtils.MouseToWorldPos().RoundToInt() -StartPositionWorld.RoundToInt() ));
		}


		PressedObject = null;
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
		CursorLine.OrNull()?.Remove();
		CursorLine = null;
		PressedObject = null;
		UIManager.IsMouseInteractionDisabled = false;
		if (escapeKeyTarget != null)
		{
			escapeKeyTarget.enabled = false;
		}

		StopSelectingButton.interactable = false;
	}


	[NaughtyAttributes.Button]
	public void OnSelected()
	{
		StopSelectingButton.interactable = true;
		UIManager.IsMouseInteractionDisabled = true;
		escapeKeyTarget.enabled = true;
		if (Updating == false)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			Updating = true;
		}
	}
}
