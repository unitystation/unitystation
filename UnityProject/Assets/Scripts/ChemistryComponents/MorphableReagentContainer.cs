using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(ReagentContainer))]
public class MorphableReagentContainer : NetworkBehaviour
{
	private static int NoMajorReagent = "".GetStableHashCode();

	public SpriteRenderer mainSpriteRender;
	public MorphableReagentContainerData data;

	private Chemistry.Reagent majorReagent;
	[SyncVar(hook = "OnMajorReagentChanged")]
	private int majorReagentNameHash = NoMajorReagent;

	private ReagentContainerFillVisualisation fillVisual;
	private ReagentContainer serverContainer;
	private Pickupable pickupable;
	private ItemAttributesV2 item;

	private string defaultDescription;
	private Sprite defaultSprite;

	private void Awake()
	{
		if (!data || !mainSpriteRender)
			return;

		fillVisual = GetComponent<ReagentContainerFillVisualisation>();
		pickupable = GetComponent<Pickupable>();
		serverContainer = GetComponent<ReagentContainer>();
		item = GetComponent<ItemAttributesV2>();

		// save default data
		defaultSprite = mainSpriteRender.sprite;
		if (item)
		{
			defaultDescription = item.InitialDescription;
		}


		if (serverContainer)
		{
			serverContainer.OnReagentMixChanged.AddListener(OnMixChanged);
		}
	}

	[Server]
	private void OnMixChanged()
	{
		// Check if major reagent changed in a mix
		var newMajorReagent = serverContainer.MajorMixReagent;
		if (newMajorReagent != majorReagent)
		{
			// get major reagent name
			var majorReagentName = newMajorReagent ? newMajorReagent.Name : "";

			// now send it to all clients as string hash
			majorReagentNameHash = majorReagentName.GetStableHashCode();
			majorReagent = newMajorReagent;
		}
	}

	[Client]
	private void OnMajorReagentChanged(int oldHash, int newHash)
	{
		if (newHash != NoMajorReagent)
		{
			// check if we have this reagent in overrides table
			var spriteData = data.Get(newHash);
			if (spriteData != null)
				ShowVisualisation(spriteData);
			else
				DisableVisualisation();
		}
		else
		{
			DisableVisualisation();
		}

		// Update UI sprite in inventory
		pickupable?.RefreshUISlotImage();
	}

	private void ShowVisualisation(ContainerCustomSprite spriteData)
	{
		mainSpriteRender.sprite = spriteData.MainSprite;
		if (fillVisual && fillVisual.fillSpriteRender)
			fillVisual.fillSpriteRender.gameObject.SetActive(false);

		// set custom glass description, if avaliable
		var customDesc = spriteData.CustomDescription;
		if (!string.IsNullOrEmpty(customDesc))
		{
			if (item)
			{
				item.ServerSetArticleDescription(customDesc);
			}
		}
	}

	/// <summary>
	/// Disable all morphable overrides and show standard graphics
	/// </summary>
	private void DisableVisualisation()
	{
		mainSpriteRender.sprite = defaultSprite;
		if (fillVisual && fillVisual.fillSpriteRender)
			fillVisual.fillSpriteRender.gameObject.SetActive(true);

		// return description to standard
		if (item)
		{
			item.ServerSetArticleDescription(defaultDescription);
		}
	}

}
