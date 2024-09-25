using Core;
using Mirror;
using UnityEngine;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

/// <summary>
/// Only mime can make it visible until it's moved
/// </summary>
public class InvisibleBox : Pickupable
{
	[Header("Assign these to make it work")]
	[SerializeField] private SpriteColorSync boxSpriteColor = default;
	[SerializeField] private UniversalObjectPhysics ObjectPhysics = default;
	private readonly Color transparent = new Color(1f, 1f, 1f, 0f);
	private readonly Color semiTransparent = new Color(1f, 1f, 1f, 0.5f);

	public override void Start()
	{
		base.Start();

		if (!isServer)
		{
			return;
		}

		if (ObjectPhysics)
		{
			if (boxSpriteColor)
			{
				boxSpriteColor.SetTransitionTime(1f);
				boxSpriteColor.SetColorServer(semiTransparent);
			}

			ObjectPhysics.OnLocalTileReached.AddListener((_, _) =>
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
		if (interaction.PerformerPlayerScript.Mind.IsMiming)
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
		if (this.gameObject != info.MovedObject.gameObject) return;
		if (info.RemoveType == InventoryRemoveType.Drop || info.RemoveType == InventoryRemoveType.Throw)
		{
			if (boxSpriteColor)
			{
				boxSpriteColor.SetColorServer(transparent);
			}
		}
	}
}
