using Mirror;
using UnityEngine;

/// <summary>
/// Only mime can make it visible until it's moved
/// </summary>
public class InvisibleBox : Pickupable
{
	[Header("Assign these to make it work")]
	[SerializeField] private SpriteColorSync boxSpriteColor;
	[SerializeField] private CustomNetTransform netTransform;
	private readonly Color transparent = new Color(1f, 1f, 1f, 0f);
	private readonly Color semiTransparent = new Color(1f, 1f, 1f, 0.5f);

	public override void Start()
	{
		base.Start();

		if (!isServer)
		{
			return;
		}

		if (netTransform)
		{
			if (boxSpriteColor)
			{
				boxSpriteColor.SetTransitionTime(1f);
				boxSpriteColor.SetColorServer(semiTransparent);
			}

			netTransform.OnTileReached().AddListener(pos =>
			{
				if (boxSpriteColor)
				{
					boxSpriteColor.SetColorServer(transparent);
				}
			});
		}
	}

	public override void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.PerformerPlayerScript.mind.IsMiming)
		{
			if (boxSpriteColor)
			{
				boxSpriteColor.SetColorServer(semiTransparent);
			}
		}
		base.ServerPerformInteraction(interaction);
	}

	public override void OnInventoryMoveServer(InventoryMove info)
	{
		base.OnInventoryMoveServer(info);
		if (info.RemoveType == InventoryRemoveType.Drop || info.RemoveType == InventoryRemoveType.Throw)
		{
			if (boxSpriteColor)
			{
				boxSpriteColor.SetColorServer(transparent);
			}
		}
	}
}