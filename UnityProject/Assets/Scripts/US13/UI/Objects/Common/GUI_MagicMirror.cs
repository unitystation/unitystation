using System.Collections;
using UnityEngine;
using US13.Objects.Other;
using US13.ScriptableObjects;
using US13.UI.Core;
using US13.UI.Core.Net;

namespace US13.UI.Objects.Common
{
	/// <summary>
	/// GUI for MagicMirror.
	/// </summary>
	public class GUI_MagicMirror : NetTab
	{
		[SerializeField]
		private InputFieldFocus inputField = default;

		[SerializeField]
		private StringList wizardFirstNames = default;
		[SerializeField]
		private StringList wizardLastNames = default;

		private MagicMirror mirror;

		#region Lifecycle

		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			mirror = Provider.GetComponent<MagicMirror>();
		}

		#endregion Lifecycle

		public void SetName(string inputName)
		{
			mirror.SetPlayerName(inputName);
		}

		public void ClientInputRandomName()
		{
			inputField.text = $"{wizardFirstNames.GetRandom()} {wizardLastNames.GetRandom()}";
		}
	}
}
