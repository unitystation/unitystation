using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class ShuttleHeater : AdvancedPipe
{
	public override void SpriteChange()
	{
		return;
	}

	public override void SetSpriteLayer()
	{
		return;
	}

	public override void SetAnchored(bool value)
	{
		anchored = value;
	}
}
