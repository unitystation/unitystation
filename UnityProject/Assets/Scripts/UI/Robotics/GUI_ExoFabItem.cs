using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class GUI_ExoFabItem : DynamicEntry
{
	private MachineProduct products = null;

	public MachineProduct Products
	{
		get
		{
			return products;
		}
		set
		{
			products = value;
			//ReInit();
		}

		//ReInit(catego
	}
}