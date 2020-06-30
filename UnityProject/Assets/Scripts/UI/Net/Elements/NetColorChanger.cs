using System;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Slap this on image/text label gameobject to control its color
/// </summary>
[RequireComponent(typeof(Graphic))]
[Serializable]
public class NetColorChanger : NetUIElement<Color>
{
	public override ElementMode InteractionMode => ElementMode.ServerWrite;

	public override Color Value
	{
		get => Element.color;
		set {
			externalChange = true;
			Element.color = value;
			externalChange = false;
		}
	}

	public override byte[] BinaryValue
	{
		get
		{
			// Using the manual approach since it seemed to be a bottleneck when profiling
			var color = Element.color;
			var bytes = new byte[sizeof(float) * 4];
			BitConverter.GetBytes(color.r).CopyTo(bytes, sizeof(float) * 0);
			BitConverter.GetBytes(color.g).CopyTo(bytes, sizeof(float) * 1);
			BitConverter.GetBytes(color.b).CopyTo(bytes, sizeof(float) * 2);
			BitConverter.GetBytes(color.a).CopyTo(bytes, sizeof(float) * 3);
			return bytes;
		}
		set
		{
			Element.color = new Color(
				BitConverter.ToSingle(value, sizeof(float) * 0),
				BitConverter.ToSingle(value, sizeof(float) * 1),
				BitConverter.ToSingle(value, sizeof(float) * 2),
				BitConverter.ToSingle(value, sizeof(float) * 3)
				);
		}
	}

	private Graphic element;
	public Graphic Element {
		get {
			if ( !element ) {
				element = GetComponent<Graphic>();
			}
			return element;
		}
	}

	public override void ExecuteServer(ConnectedPlayer subject) {	}

	/// <summary>
	/// Server-only method for updating element (i.e. changing label text) from server GUI code
	/// </summary>
	public override void SetValueServer(Color value)
	{
		if (Value != value)
		{
			Value = value;
			UpdatePeepers();
		}
	}

}