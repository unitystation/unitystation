using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ScriptableObjects.Communications
{
	[CreateAssetMenu(fileName = "SignalData", menuName = "ScriptableObjects/SignalData")]
	public class SignalDataSO : ScriptableObject
	{
		[Tooltip("Is this signal global or do we want to check how far it is from a receiver?")]
		public bool UsesRange = true;
		[Tooltip("Measured in tiles"), ShowIf("UsesRange")]
		public int SignalRange = 300;
		public SignalType EmittedSignalType = SignalType.PING;
		[Tooltip("If the frequancy of the receiver is inbetween these values then they'll go through.")]
		public Vector2 MinMaxFrequancy = new Vector2(100, 144); //in Khz
		[Tooltip("Encryption keys prevent people from spying on radios and ping devices they're not supposed to mess with.")]
		public EncryptionDataSO EncryptionData;



#if UNITY_EDITOR
		[Button("Generate Encryption Key", EButtonEnableMode.Editor), HideIf("HasEncryption")]
		private void CreateEncryptionData()
		{
			EncryptionDataSO data = new EncryptionDataSO();
			data.name = $"{this.name} - Encryption";
			EncryptionData = data;
			AssetDatabase.AddObjectToAsset(data, this);
			AssetDatabase.SaveAssets();
			EditorUtility.SetDirty(this);
			EditorUtility.SetDirty(data);
		}
#endif
	}
}

