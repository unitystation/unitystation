using System;
using UnityEngine;

namespace UI.Core.NetUI
{
	/// Sends client's touch coordinates (within element) over network
	[RequireComponent(typeof(TouchScreen))]
	[Serializable]
	public class NetTouchScreen : NetUIStringElement
	{
		public override ElementMode InteractionMode => ElementMode.ClientWrite;

		public override string Value {
			protected  set => Element.LastTouchPosition = value.Vectorized();
			get => Element.LastTouchPosition.Stringified();
		}

		private TouchScreen element;
		public TouchScreen Element => element ??= GetComponent<TouchScreen>();

		public StringEvent ServerMethod;

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke(Value);
		}
	}
}
