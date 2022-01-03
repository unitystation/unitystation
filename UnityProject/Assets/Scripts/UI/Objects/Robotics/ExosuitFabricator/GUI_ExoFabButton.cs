using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Objects.Robotics
{
	[Serializable]
	public class GUI_ExoFabButton : NetButton
	{
		//SetValue will set this and change the value for the client.
		public override string Value {
			get { return Element.interactable.ToString(); }
			set {
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
					Logger.Log("Interactable shouldn't be set to anything other than true or false.", Category.Machines);
				}
			}
		}

		private Selectable element;

		public Selectable Element {
			get {
				if (!element)
				{
					element = GetComponent<Selectable>();
				}
				return element;
			}
		}

		public override void ExecuteServer(ConnectedPlayer subject)
		{
			ServerMethod.Invoke();
		}
	}
}
