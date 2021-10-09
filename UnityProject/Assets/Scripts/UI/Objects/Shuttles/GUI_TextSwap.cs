using System.Collections;
using System.Collections.Generic;
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
		private UISwapDictionary textSetupDict = default;

		private void Start()
		{
			if (shuttleControlScript == null || textToSet == null)
			{
				Logger.LogError("TextSwap script reference failure!", Category.UI);
				enabled = false;
				return;
			}

			UIType keyToCheck = shuttleControlScript.MatrixMove.uiType;
			if (textSetupDict.ContainsKey(keyToCheck))
			{
				textToSet.text = textSetupDict[keyToCheck].Replace("\\n", "\n");
			}
			else
			{
				Logger.LogWarning("No Key for UIType found in TextSwap. Leaving Text alone", Category.UI);
			}
		}
	}
}
