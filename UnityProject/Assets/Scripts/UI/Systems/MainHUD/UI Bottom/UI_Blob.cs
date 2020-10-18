using System.Collections;
using System.Collections.Generic;
using Blob;
using UnityEngine;
using TMPro;

public class UI_Blob : MonoBehaviour
{
	public TMP_Text healthText = null;

	public TMP_Text resourceText = null;

	public TMP_Text numOfBlobTilesText = null;

	[SerializeField]
	private GameObject overlayNode = null;
	[SerializeField]
	private GameObject overlayStrong = null;
	[SerializeField]
	private GameObject overlayReflective = null;
	[SerializeField]
	private GameObject overlayFactory = null;
	[SerializeField]
	private GameObject overlayResource = null;
	[SerializeField]
	private GameObject overlayRemoveBlob = null;
	[SerializeField]
	private GameObject overlayRally = null;
	[SerializeField]
	private GameObject overlayMoveCore = null;

	private bool node;
	private bool strong;
	private bool reflective;
	private bool factory;
	private bool resource;
	private bool remove;
	private bool rally;
	private bool core;

	[HideInInspector]
	public BlobPlayer blobPlayer = null;

	[HideInInspector]
	public BlobMouseInputController controller = null;
	public void JumpToCore()
	{
		if (blobPlayer == null) return;

		blobPlayer.CmdTeleportToCore();
	}

	public void JumpToNode()
	{
		if (blobPlayer == null) return;

		blobPlayer.CmdTeleportToNode();
	}

	/// <summary>
	/// Alternative to alt click
	/// </summary>
	public void RemoveBlob()
	{
		if (remove)
		{
			overlayRemoveBlob.SetActive(false);
			controller.placeOther = false;
			blobPlayer.CmdToggleRemove(true);
			remove = false;
			return;
		}

		ClearBools();
		remove = true;

		controller.placeOther = false;
		blobPlayer.CmdToggleRemove(false);
		ClearOutline();
		overlayRemoveBlob.SetActive(true);
	}

	public void RallySpores()
	{
		Chat.AddExamineMsgToClient("The blob has yet to evolve to command these.");

		return;

		if (rally)
		{
			overlayRally.SetActive(false);
			controller.placeOther = false;
			rally = false;
			return;
		}

		ClearBools();
		rally = true;

		ClearOutline();
		overlayRally.SetActive(controller.placeOther);
	}

	public void ReadaptStrain()
	{
		Chat.AddExamineMsgToClient("The blob has yet to evolve these abilities.");
	}

	public void RelocateCore()
	{
		if (core)
		{
			overlayMoveCore.SetActive(false);
			controller.placeOther = false;
			core = false;
			return;
		}

		ClearBools();
		core = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Core;
		ClearOutline();
		overlayMoveCore.SetActive(controller.placeOther);
	}

	public void PlaceNode()
	{
		if (node)
		{
			overlayNode.SetActive(false);
			controller.placeOther = false;
			node = false;
			return;
		}

		ClearBools();
		node = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Node;
		ClearOutline();
		overlayNode.SetActive(controller.placeOther);
	}

	public void PlaceStrong()
	{
		if (strong)
		{
			overlayStrong.SetActive(false);
			controller.placeOther = false;
			strong = false;
			return;
		}

		ClearBools();
		strong = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Strong;
		ClearOutline();
		overlayStrong.SetActive(controller.placeOther);
	}

	public void PlaceReflective()
	{
		if (reflective)
		{
			overlayReflective.SetActive(false);
			controller.placeOther = false;
			reflective = false;
			return;
		}

		ClearBools();
		reflective = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Reflective;
		ClearOutline();
		overlayReflective.SetActive(controller.placeOther);
	}

	public void PlaceFactory()
	{
		if (factory)
		{
			overlayFactory.SetActive(false);
			controller.placeOther = false;
			factory = false;
			return;
		}

		ClearBools();
		factory = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Factory;
		ClearOutline();
		overlayFactory.SetActive(controller.placeOther);
	}

	public void PlaceResource()
	{
		if (resource)
		{
			overlayResource.SetActive(false);
			controller.placeOther = false;
			resource = false;
			return;
		}

		ClearBools();
		resource = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Resource;
		ClearOutline();
		overlayResource.SetActive(controller.placeOther);
	}

	public void ClearOutline()
	{
		overlayFactory.SetActive(false);
		overlayNode.SetActive(false);
		overlayReflective.SetActive(false);
		overlayResource.SetActive(false);
		overlayStrong.SetActive(false);
		overlayMoveCore.SetActive(false);
		overlayRally.SetActive(false);
		overlayRemoveBlob.SetActive(false);
	}

	public void ClearBools()
	{
		node= false;
		strong= false;
		reflective= false;
		factory= false;
		resource= false;
		remove= false;
		rally= false;
		core= false;
	}
}
