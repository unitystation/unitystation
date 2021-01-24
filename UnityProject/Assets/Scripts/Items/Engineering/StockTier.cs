using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component used for stock parts. Higher tier stock parts are more efficient and
/// improve machine functionality.
/// </summary>
public class StockTier : MonoBehaviour
{
	[Tooltip("Tier of this stock part.")]
	[SerializeField]
	private int tier = 1;
	/// <summary>
	/// Tier of the stock part.
	/// </summary>
	public int Tier => tier;

}
