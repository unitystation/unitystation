using NaughtyAttributes;
using UnityEngine;
using US13.Managers;

namespace US13.ScriptableObjects.Telecomms
{
	[CreateAssetMenu(fileName = "SignalData", menuName = "ScriptableObjects/SignalData/SignalData")]
	public class SignalDataSO : ScriptableObject
	{
		[Tooltip("Is this signal global or do we want to check how far it is from a receiver?")]
		public bool UsesRange = true;
		[Tooltip("Measured in tiles"), ShowIf(nameof(UsesRange))]
		public int SignalRange = 300;
		public SignalType EmittedSignalType = SignalType.PING;
		[Tooltip("If the frequancy of the receiver is inbetween these values then they'll go through.")]
		public Vector2 MinMaxFrequancy = new Vector2(100, 144); //in Khz
	}
}

