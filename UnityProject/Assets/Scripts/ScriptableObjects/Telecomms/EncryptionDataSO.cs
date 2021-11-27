using System;
using UnityEngine;
using file = System.IO.File;
using Random = System.Random;

namespace ScriptableObjects.Communications
{
	[CreateAssetMenu(fileName = "EncryptionData", menuName = "ScriptableObjects/SignalData/EncryptionData")]
	public class EncryptionDataSO : ScriptableObject
	{
		/// <summary>
		/// The string of characters that is used to encrypt messages
		/// </summary>
		public string EncryptionSecret;

		/// <summary>
		/// Flaw for the decryption/hacking minigame.
		/// </summary>
		public string EncryptionFlaw;

		[SerializeField] private TextAsset randomWords;

		private void Awake()
		{
			if (randomWords != null)
			{
				string[] lines = file.ReadAllLines(randomWords.text);
				EncryptionFlaw = lines[new Random().Next(lines.Length)];
				return;
			}
			EncryptionFlaw = new Random().Next(10, 99).ToString();
		}
	}
}