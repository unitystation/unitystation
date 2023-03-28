using UnityEngine;
using UnityEngine.Rendering;

namespace _3D
{
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
}
