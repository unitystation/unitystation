using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

namespace ScriptableObjects.Communications
{
	[CreateAssetMenu(fileName = "SignalData", menuName = "ScriptableObjects/SignalData")]
	public class SignalDataSO : ScriptableObject
	{
		public bool UsesRange = true;
		public int SignalRange = 1000; //Measured in tiles(?)
		public SignalType EmittedSignalType = SignalType.PING;

	}
}

