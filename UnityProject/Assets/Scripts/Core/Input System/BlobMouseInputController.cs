using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using Blob;
using UnityEngine;
using UnityEngine.EventSystems;
using Weapons;
using Objects.Wallmounts;

/// <summary>
/// Main entry point for handling all input events
/// </summary>
public class BlobMouseInputController : MouseInputController
{
	private BlobPlayer blobPlayer;

	public override void Start()
	{
		base.Start();
		blobPlayer = GetComponent<BlobPlayer>();
	}

	public override void CheckMouseInput()
	{
		if (EventSystem.current.IsPointerOverGameObject())
		{
			//don't do any game world interactions if we are over the UI
			return;
		}

		if (UIManager.IsMouseInteractionDisabled)
		{
			//still allow tooltips
			CheckHover();
			return;
		}

		if (CommonInput.GetMouseButtonDown(0))
		{

			//check ctrl+click for dragging
			if (KeyboardInputManager.IsControlPressed())
			{
				//TODO place strong blob / reflective if strong blob already

				Debug.LogError("ctrl");

				return;
			}

			if (KeyboardInputManager.IsShiftPressed())
			{
				//like above, send shift-click request, then do nothing else.
				Inspect();
				return;
			}

			if (KeyboardInputManager.IsAltPressed())
			{
				//TODO remove blob
				Debug.LogError("alt");

				return;
			}

			//todo check to see if we need to override
			CheckClickInteractions(true);

			blobPlayer.CmdTryPlaceBlobOrAttack(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition).RoundToInt());
		}
		else
		{
			CheckHover();
		}
	}
}
