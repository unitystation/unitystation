using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This class enables an object to be processed by the processor.
/// After the processor is finished with processing, it will despawn the parent object and spawns the processedProduct.
/// </summary>
public class Processable : MonoBehaviour
{
	[SerializeField]
	[Tooltip("What this GameObject becomes when processed. If not set, this GameObject will not change GameObject when cooked.")]
	private GameObject processedProduct;
	/// <summary>
	/// Get the processed product of this object.
	/// </summary>
	public GameObject ProcessedProduct => processedProduct;

	[SerializeField]
	[Tooltip("How many items are produced per gameObject (assuming a tier 1 matter bin is used.)")]
	private int productAmount = 1;

	/// <summary>
	/// How many items are produced per gameObject (assuming a tier 1 matter bin is used.)
	/// </summary>
	public int ProductAmount => productAmount;
}
