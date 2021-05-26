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

				if (ElectricalManager.DOCheck)
				{
					//Logger.Log("Seting cables!");
					var thing = GetComponent<CableInheritance>();
					thing.ConvertToTile(true);
					//ElectricalManager.DOCheck = false;
					/*Logger.Log("Cleaning cables!");
					var ElectricalOI = this.GetComponent<ElectricalOIinheritance>();
					if (bigDict.ContainsKey(this.transform.localPosition))
					{

						foreach (var con in bigDict[this.transform.localPosition])
						{
							if (ElectricalOI != con)
							{
								if ((ElectricalOI.InData.WireEndA == con.InData.WireEndA && ElectricalOI.InData.WireEndB == con.InData.WireEndB) ||
									(ElectricalOI.InData.WireEndA == con.InData.WireEndB && ElectricalOI.InData.WireEndB == con.InData.WireEndA))
								{
									DestroyImmediate(gameObject);
									return;
								}

							}
						}
						bigDict[this.transform.localPosition].Add(ElectricalOI);
					}
					else {
						bigDict[this.transform.localPosition] = new HashSet<ElectricalOIinheritance>();
						bigDict[this.transform.localPosition].Add(ElectricalOI);
					}*/
				}
			}
		}
#endif
	}
}
