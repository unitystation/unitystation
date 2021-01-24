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

	[Tooltip("What this GameObject becomes when processed. If not set, this GameObject will not change GameObject when cooked.")]
	public GameObject CookedProduct;

}
