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

	public SpriteHandler spriteHandler;
	public MorphableReagentContainerData data;

	private Chemistry.Reagent majorReagent;
	[SyncVar(hook = "OnMajorReagentChanged")]
	private int majorReagentNameHash = NoMajorReagent;

	private ReagentContainerFillVisualisation fillVisual;
	private ReagentContainer serverContainer;
	private Pickupable pickupable;

	private SpriteSheetAndData defaultSprite;

	private void Awake()
	{
		if (!data)
			return;

		var hasDefaultSprite = SetupDefaultSprite();
		if (!hasDefaultSprite)
			return;

		fillVisual = GetComponent<ReagentContainerFillVisualisation>();
		pickupable = GetComponent<Pickupable>();
		serverContainer = GetComponent<ReagentContainer>();

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
	}

	private void ShowVisualisation(ContainerCustomSprite sprite)
	{
		spriteHandler.SetSprite(sprite.SpriteSheet);
		if (fillVisual)
		{
			fillVisual.fillSpriteRender.enabled = false;
		}

		// Update UI sprite in inventory
		pickupable?.RefreshUISlotImage();
	}

	/// <summary>
	/// Disable all morphable overrides and show standard graphics
	/// </summary>
	private void DisableVisualisation()
	{
		spriteHandler.SetSprite(defaultSprite);
		if (fillVisual)
		{
			fillVisual.fillSpriteRender.enabled = true;
		}

		// Update UI sprite in inventory
		pickupable?.RefreshUISlotImage();
	}

	/// <summary>
	/// Setups default sprite sheet for container (like empty glass sprite)
	/// </summary>
	/// <returns>True if setup was successful</returns>
	private bool SetupDefaultSprite()
	{
		if (!spriteHandler)
		{
			Logger.LogError($"{gameObject.name} Can't use MorphableReagentContainer " +
				"without SpriteHandler selected", Category.Chemistry);
			return false;
		}

		if (spriteHandler.Sprites == null || spriteHandler.Sprites.Count == 0)
		{
			Logger.LogError($"{gameObject.name} Can't use MorphableReagentContainer " +
				"without SpriteHandler default sprite assigned. Add default sprite to SpriteHandler.", Category.Chemistry);
			return false;
		}

		defaultSprite = spriteHandler.Sprites.First();
		if (defaultSprite == null)
			return false;

		return true;
	}
}
