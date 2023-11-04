using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	/// Slider, used to call methods with float as arg
	[RequireComponent(typeof(Slider))]
	[Serializable]
	public class NetSlider : NetUIStringElement
	{

		public ElementMode SetInteractionMode = ElementMode.Normal;

		public override ElementMode InteractionMode => SetInteractionMode;

		public override string Value {
			get => ((int)(Element.value * 100)).ToString();
			protected set {
				externalChange = true;
				Element.value = int.Parse(value) / 100f;
				externalChange = false;
			}
		}

		public FloatEvent ServerMethod;

		private Slider element;
		public Slider Element => element ??= GetComponent<Slider>();

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke(Element.value);
		}
	}

	/// <inheritdoc />
	/// "If you wish to use a generic UnityEvent type you must override the class type."
	[Serializable]
	public class FloatEvent : UnityEvent<float> { }
}
