using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "InitialProductSO", menuName = "ScriptableObjects/Systems/Techweb/InitialProductSO")]
public class RDProInitialProductsSO : ScriptableObject
{
	[Header("Provide the exact Internal_ID of the design in the fields below:")]
	[InfoBox("DesignIDs in this list will always be available to produce regardless of techweb data," +
			 "unless the machine itself does not have the capability to produce them.")]
	public List<string> InitialDesigns = new List<string>();
}
