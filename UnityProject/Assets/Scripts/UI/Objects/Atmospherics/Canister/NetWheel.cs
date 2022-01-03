using System;
using UnityEngine;


namespace UI.Core
{
	/// <summary>
	/// NetUI component for Wheel, handles syncing the value.
	/// </summary>
	public class NetWheel : NetUIStringElement
	{
		public Wheel Element;

		public override string Value
		{
			set
			{
				externalChange = true;
				Element.RotateToValue(Convert.ToInt32(Convert.ToDouble(value)));
				externalChange = false;
			}
			get => Element.KPA.ToString();
		}

		public FloatEvent ServerMethod;

		public override void ExecuteServer(ConnectedPlayer subject)
		{
			ServerMethod.Invoke(Element.KPA);
		}
	}
}
