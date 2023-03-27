using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ConvertTo3D : MonoBehaviour
{

	public void DoConvertTo3D()
	{
		this.gameObject.AddComponent<Billboard>();

		var  sorting = this.gameObject.GetComponent<SortingGroup>();

		if (sorting == null)
		{
			sorting = this.gameObject.AddComponent<SortingGroup>();
		}

		sorting.sortingOrder = 1;

		sorting.sortingLayerName = "Walls";
	}
}
