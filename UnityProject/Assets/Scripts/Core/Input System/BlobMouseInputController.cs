using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using Blob;
using Logs;
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

	private void Awake()
	{
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

		if (KeyboardInputManager.IsMiddleMouseButtonPressed())
		{
			//Rally blobs
			blobPlayer.CmdRally(MouseUtils.MouseToWorldPos().RoundToInt());
			return;
		}

		if (CommonInput.GetMouseButtonDown(0))
		{

			if(ClicksFromBlobHud()) return;

			//check ctrl+click for dragging
			if (KeyboardInputManager.IsControlPressed())
			{
				//Place strong blob / reflective if strong blob already
				blobPlayer.CmdTryPlaceStrongReflective(MouseUtils.MouseToWorldPos().RoundToInt());
				return;
			}

			if (KeyboardInputManager.IsShiftPressed())
			{
				//like above, send shift-click request, then do nothing else.
				Inspect();
				return;
			}

			if (KeyboardInputManager.IsAltActionKeyPressed())
			{
				//Remove blob
				blobPlayer.CmdRemoveBlob(MouseUtils.MouseToWorldPos().RoundToInt());
				return;
			}

			blobPlayer.CmdTryPlaceBlobOrAttack(MouseUtils.MouseToWorldPos().RoundToInt());
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
					blobPlayer.CmdMoveCore(MouseUtils.MouseToWorldPos().RoundToInt());
					break;
				case BlobConstructs.Node:
					blobPlayer.CmdTryPlaceOther(MouseUtils.MouseToWorldPos().RoundToInt(), BlobConstructs.Node);
					break;
				case BlobConstructs.Factory:
					blobPlayer.CmdTryPlaceOther(MouseUtils.MouseToWorldPos().RoundToInt(), BlobConstructs.Factory);
					break;
				case BlobConstructs.Resource:
					blobPlayer.CmdTryPlaceOther(MouseUtils.MouseToWorldPos().RoundToInt(), BlobConstructs.Resource);
					break;
				case BlobConstructs.Strong:
					blobPlayer.CmdTryPlaceStrongReflective(MouseUtils.MouseToWorldPos().RoundToInt());
					break;
				case BlobConstructs.Reflective:
					blobPlayer.CmdTryPlaceStrongReflective(MouseUtils.MouseToWorldPos().RoundToInt());
					break;
				case BlobConstructs.Rally:
					blobPlayer.CmdRally(MouseUtils.MouseToWorldPos().RoundToInt());
					break;
				default:
					Loggy.LogError("Switch has no correct case for blob click!", Category.Blob);
					break;
			}

			return true;
		}

		return false;
	}
}
