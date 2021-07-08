using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Randomizes damage whenever a swing occurs.
/// </summary>
public class ExtradimBlade : MonoBehaviour, ICustomDamageCalculation
{


	[SerializeField]
	private int minDamage = 1;

	[SerializeField]
	private int maxDamage = 30;

	private static System.Random rnd = new System.Random();

	public int ServerPerformDamageCalculation()
	{
		return rnd.Next(minDamage, maxDamage);
	}
}
