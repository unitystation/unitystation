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

	public override void Start()
	{
		base.Start();

		if (!isServer)
		{
			return;
		}

		if (netTransform)
		{
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
		base.ServerPerformInteraction(interaction);
		if (interaction.PerformerPlayerScript.mind.IsMiming)
		{
			if (boxSpriteColor)
			{
				boxSpriteColor.SetColorServer(Color.white);
			}
		}
	}
}