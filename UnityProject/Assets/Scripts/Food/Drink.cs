using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drink : Edible {
	private void Start()
	{
		//assuming all drinks are spillable on throw
		itemAttributes.AddTrait(CommonTraits.Instance.SpillOnThrow);
	}

	// Use this for initialization
	public override void TryEat()
	{
        isDrink = true;
        base.TryEat();
	}
}
