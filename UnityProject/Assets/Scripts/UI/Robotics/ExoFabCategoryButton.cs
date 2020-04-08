using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExoFabCategoryButton : NetButton
{
	[HideInInspector]
	public string categoryName;

	public override void ExecuteServer()
	{
		ServerMethod.Invoke();
	}
}