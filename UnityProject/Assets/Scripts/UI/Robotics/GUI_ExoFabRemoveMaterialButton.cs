﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabRemoveMaterialButton : NetButton
{
	public int value = 5;
	public ItemTrait itemTrait;

	public override void ExecuteServer()
	{
		ServerMethod.Invoke();
	}
}