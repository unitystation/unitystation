using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "SurgeryProcedureBaseSingleton", menuName = "ScriptableObjects/Surgery/SurgeryProcedureBaseSingleton")]
	public class SurgeryProcedureBaseSingleton : SingletonScriptableObject<SurgeryProcedureBaseSingleton>
	{
		public List<SurgeryProcedureBase> StoredReferences = new List<SurgeryProcedureBase>();
	}
}