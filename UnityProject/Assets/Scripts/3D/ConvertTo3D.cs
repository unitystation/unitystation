using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace _3D
{
	public class ConvertTo3D : MonoBehaviour
	{
		private RegisterTile registerTile;

		public void Awake()
		{
			registerTile = this.GetComponent<RegisterTile>();
		}

		public void DoConvertTo3D()
		{
			if (registerTile != null && registerTile.LiesFlat3D)
			{
				for (int i = 0; i < gameObject.transform.childCount; i++)
				{
					gameObject.transform.GetChild(i).transform.localPosition =
						gameObject.transform.GetChild(i).transform.localPosition + new Vector3(0, 0, 0.5f);
				}
			}
			else
			{
				this.gameObject.AddComponent<Billboard>();


			}
			var  sorting = this.gameObject.GetComponent<SortingGroup>();

			if (sorting == null)
			{
				sorting = this.gameObject.AddComponent<SortingGroup>();
			}

			sorting.sortingOrder = 1;

			sorting.sortingLayerName = "Walls";

		}
	}
}
