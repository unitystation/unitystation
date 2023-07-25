using UnityEngine;
using Random = System.Random;

namespace ScriptableObjects.Communications
{
	[CreateAssetMenu(fileName = "EncryptionData", menuName = "ScriptableObjects/SignalData/EncryptionData")]
	public class EncryptionDataSO : ScriptableObject
	{
		/// <summary>
		/// The string of characters that is used to encrypt messages
		/// </summary>
		public int EncryptionCode;

		/// <summary>
		/// Flaw for the decryption/hacking minigame.
		/// </summary>
		public string EncryptionFlaw;

		private void Awake()
		{
			//TODO (Max): This will be expanded upon in the future to allow for shared random keys When we start working on the telecomms update
			//Encryption flaws are meant for the telecomms updates so this right now doesn't really have much use at the moment
			EncryptionFlaw = new Random().Next(10, 99).ToString();
		}
	}
}