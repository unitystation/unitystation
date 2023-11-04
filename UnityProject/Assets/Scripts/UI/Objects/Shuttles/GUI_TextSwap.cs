using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Objects.Shuttles
{
	/// <summary>
	/// A generic TextSwap script for different UITypes
	/// </summary>
	public class GUI_TextSwap : MonoBehaviour
	{
		[Header("References")]
		[SerializeField]
		private GUI_ShuttleControl shuttleControlScript = default;
		[SerializeField]
		private Text textToSet = default;

		[Header("Settings")]
		[SerializeField]
		private SerializableDictionary<UIType, string> textSetupDict = default;

		private void Start()
		{
			if (shuttleControlScript == null || textToSet == null)
			{
				Loggy.LogError("TextSwap script reference failure!", Category.UI);
				enabled = false;
				return;
			}

			UIType keyToCheck = shuttleControlScript.matrixMove.uiType;
			if (textSetupDict.ContainsKey(keyToCheck))
			{
				textToSet.text = textSetupDict[keyToCheck].Replace("\\n", "\n");
			}
			else
			{
				Loggy.LogWarning("No Key for UIType found in TextSwap. Leaving Text alone", Category.UI);
			}
		}
	}
}
