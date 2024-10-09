using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Physics;
using Core.Utils;
using InGameGizmos;
using Shared.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(EscapeKeyTarget))]
public class DeviceAttributeEditor : SingletonManager<DeviceAttributeEditor>
{
	public Button StopSelectingButton;

	public TMP_InputField InputField;

	public TMP_Dropdown Dropdown;

	// so we can escape while drawing - enabled while drawing, disabled when done
	private EscapeKeyTarget escapeKeyTarget;

	public GameObject PressedObject = null;

	public bool Updating = false;

	public Toggle IsMappedToggle;

	public enum RenameType
	{
		ObjectName,
		AttributeRename,
		MindRename
	}

	private void OnEnable()
	{
		escapeKeyTarget = GetComponent<EscapeKeyTarget>();
	}

	public override void Start()
	{
		base.Start();
		// Create a list of new options
		List<string> options = new List<string> { "GameObject Name", "Attribute Name", "Mind Name (Player Name)" };

		// Clear existing options
		Dropdown.ClearOptions();

		// Add new options to the dropdown
		Dropdown.AddOptions(options);
		Dropdown.onValueChanged.AddListener(DropdownValueChanged);
		InputField.onEndEdit.AddListener(SetName);
		IsMappedToggle.onValueChanged.AddListener(IsMappedUpdate);
		this.gameObject.SetActive(false);
	}

	public void IsMappedUpdate(bool Newval)
	{
		DeviceIsMappedMessage.Send(PressedObject,Newval);
	}

	public void SetName(string NewName)
	{
		// Get the index of the selected option
		int index = Dropdown.value;
		RenameType RenameType = RenameType.AttributeRename;
		switch (index)
		{
			case 0: //object name
				RenameType = RenameType.ObjectName;
				break;
			case 1: //Attribute name
				RenameType = RenameType.AttributeRename;
				break;
			case 2: //Player
				RenameType = RenameType.MindRename;
				break;
		}

		DeviceRenamerMessage.Send(PressedObject, InputField.text, RenameType);
	}

	public void DropdownValueChanged(int Value)
	{
		SetNameUI();
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
	}

	public void OnMouseDown()
	{
		//Ignore spawn if pointer is hovering over GUI
		if (EventSystem.current.IsPointerOverGameObject()) return;


		PressedObject = MouseUtils
			.GetOrderedObjectsUnderMouse(useMappedItems: DevCameraControls.Instance.MappingItemState).FirstOrDefault();
		if (PressedObject.TryGetComponent<UniversalObjectPhysics>(out var Physics) == false)
		{
			PressedObject = null;
			return;
		}

		var Attribute = PressedObject.GetComponent<Attributes>();

		if (Attribute != null)
		{
			IsMappedToggle.isOn = PressedObject.GetComponent<Attributes>().IsMapped;
		}

		SetNameUI();
	}
	
	public void SetNameUI()
	{
		// Get the index of the selected option
		int index = Dropdown.value;

		switch (index)
		{
			case 0: //object name
				InputField.text = PressedObject.name;
				break;
			case 1: //Attribute name
				var Attributes = PressedObject.GetComponent<Attributes>();
				if (Attributes == null)
				{
					InputField.text = "N/A (no Attributes on Object)";
				}
				else
				{
					InputField.text = Attributes.InitialName;
				}
				break;
			case 2: //Player
				var player = PressedObject.Player();
				if (player == null || player.Script == null)
				{
					InputField.text = "N/A (Not a player)";
				}
				else
				{
					InputField.text =  player.Script.visibleName;
				}
				break;

		}
	}

	public void CloseButton()
	{
		this.gameObject.SetActive(false);
	}

	public void OnEscape()
	{
		OnStopSelecting();
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

	public void OnStopSelecting()
	{
		//stop drawing
		if (Updating)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			Updating = false;
		}
		PressedObject = null;
		UIManager.IsMouseInteractionDisabled = false;
		if (escapeKeyTarget != null)
		{
			escapeKeyTarget.enabled = false;
		}

		StopSelectingButton.interactable = false;
		InputField.text = "";
	}
}