using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class ShuttleHeater : AdvancedPipe
{
	public override void CalculateSprite()
	{
		return;
	}

	public override void SetSpriteLayer(bool anchoredLayer)
	{
		return;
	}

	public override void SetAnchored(bool value)
	{
		anchored = value;
		pickupable.ServerSetCanPickup(!value);
	}
}