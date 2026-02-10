using System;
using UnityEngine;
using UnityEngine.Events;

namespace Standard_Assets.HSVPicker.Events
{
	[Serializable]
	public class ColorChangedEvent : UnityEvent<Color>
	{

	}
}