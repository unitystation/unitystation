using System.Collections.Generic;
using UnityEngine;
using US13.Systems.Electricity.Electrical_processes;
using US13.Systems.Electricity.Inheritance;

namespace US13.Systems.Electricity.FunctionsAndClasses
{
#if UNITY_EDITOR
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
