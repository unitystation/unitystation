using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using Blob;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Main entry point for handling all input events
/// </summary>
public class BlobMouseInputController : MouseInputController
{
	private BlobPlayer blobPlayer;

	public bool placeOther;

	public BlobConstructs blobConstructs;

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

			if(ClicksFromBlobHud()) return;

			//check ctrl+click for dragging
			if (KeyboardInputManager.IsControlPressed())
			{
				//Place strong blob / reflective if strong blob already
				blobPlayer.CmdTryPlaceStrongReflective(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition).RoundToInt());
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
				//Remove blob
				blobPlayer.CmdRemoveBlob(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition).RoundToInt());
				return;
			}

			blobPlayer.CmdTryPlaceBlobOrAttack(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition).RoundToInt());
		}
		else
		{
			CheckHover();
		}
	}

	private bool ClicksFromBlobHud()
	{
		if (placeOther)
		{
			blobPlayer.CmdToggleRemove(true);

			switch (blobConstructs)
			{
				case BlobConstructs.Core:
					blobPlayer.CmdMoveCore(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition).RoundToInt());
					break;
				case BlobConstructs.Node:
					blobPlayer.CmdTryPlaceOther(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition).RoundToInt(), BlobConstructs.Node);
					break;
				case BlobConstructs.Factory:
					blobPlayer.CmdTryPlaceOther(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition).RoundToInt(), BlobConstructs.Factory);
					break;
				case BlobConstructs.Resource:
					blobPlayer.CmdTryPlaceOther(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition).RoundToInt(), BlobConstructs.Resource);
					break;
				case BlobConstructs.Strong:
					blobPlayer.CmdTryPlaceStrongReflective(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition).RoundToInt());
					break;
				case BlobConstructs.Reflective:
					blobPlayer.CmdTryPlaceStrongReflective(Camera.main.ScreenToWorldPoint(CommonInput.mousePosition).RoundToInt());
					break;
				default:
					Logger.LogError("Switch has no correct case for blob click!", Category.Blob);
					break;
			}

			return true;
		}

		return false;
	}
}
