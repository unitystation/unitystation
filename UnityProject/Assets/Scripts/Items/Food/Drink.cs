using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Items.Food
{
	public class Drink : Edible
	{
		private void Start()
		{
			//assuming all drinks are spillable on throw
			if (itemAttributes)
			{
				itemAttributes.AddTrait(CommonTraits.Instance.SpillOnThrow);
			}
		}
	}
}
