using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExoFabRemoveMaterialButton : NetButton
{
	public enum MaterialType
	{
		Iron,
		Glass,
		Silver,
		Gold,
		Titanium,
		Uranium,
		Plasma,
		Diamond,
		BluespaceCrystal,
		Bananium,
		Plastic
	}

	public int value;

	public override void ExecuteServer()
	{
		base.ExecuteServer();
	}
}