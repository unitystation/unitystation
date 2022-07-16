using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects.Electrical;

namespace Systems.Electricity
{
#if UNITY_EDITOR
	using UnityEditor;
	[ExecuteInEditMode]
#endif
	public class OverlapDestroy : MonoBehaviour
	{
#if UNITY_EDITOR
		public static Dictionary<Vector3, HashSet<ElectricalOIinheritance>> bigDict = new Dictionary<Vector3, HashSet<ElectricalOIinheritance>>();
		public static ElectricalManager ElectricalManager;

		// Start is called before the first frame update
		void Update()
		{
			if (Application.isPlaying == false)
			{
				if (ElectricalManager == null)
				{
					ElectricalManager = FindObjectOfType<ElectricalManager>();
				}
			}
		}
#endif
	}
}
