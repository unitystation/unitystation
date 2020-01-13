using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class GameModePage : MonoBehaviour
	{
		[SerializeField]
		private Text currentText;
		[SerializeField]
		private Dropdown nextDropDown;
		[SerializeField]
		private Toggle isSecretToggle;
		
		//Next GM change via drop down box
		public void OnNextChange()
		{

		}

		public void OnSecretChange()
		{

		}
	}
}