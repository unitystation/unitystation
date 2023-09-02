using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	/// <summary>
	/// Allows toggling a button's interactivity.
	/// </summary>
	[Serializable]
	public class NetInteractiveButton : NetButton
	{
		//SetValue will set this and change the value for the client.
		public override string Value {
			get => Element.interactable.ToString();
			protected set {
				if (value.ToLower().Equals("false"))
				{
					externalChange = true;
					Element.interactable = false;
					externalChange = false;
				}
				else if (value.ToLower().Equals("true"))
				{
					externalChange = true;
					Element.interactable = true;
					externalChange = false;
				}
				else
				{
					Loggy.Log("Interactable shouldn't be set to anything other than true or false.", Category.Machines);
				}
			}
		}

		public Selectable Element => element ??= GetComponent<Selectable>();
		private Selectable element;

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke();
		}
	}
}
