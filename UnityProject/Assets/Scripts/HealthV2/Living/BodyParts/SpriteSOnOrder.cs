using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpriteOrder
{
	[Tooltip("Down, Up, Right, Left")]
	public List<int> Orders = new List<int>() {0, 0, 0, 0};

	public SpriteOrder()
	{

	}

	public SpriteOrder(SpriteOrder Order)
	{
		Orders = new List<int>(Order.Orders);
	}

	public void Add(int Adder)
	{
		for (int i = 0; i < Orders.Count; i++)
		{
			Orders[i] += Adder;
		}
	}
}