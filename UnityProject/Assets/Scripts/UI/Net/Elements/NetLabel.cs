using System;
using UnityEngine;
using UnityEngine.UI;

///Text label, not modifiable by clients directly
[RequireComponent(typeof(TMPro.TMP_Text))]
[Serializable]
public class NetLabel : NetUIStringElement
{
	/// <summary>
	/// Invoked when the value synced between client / server is updated.
	/// </summary>
	[NonSerialized]
	public StringEvent OnSyncedValueChanged = new StringEvent();

	public override ElementMode InteractionMode => ElementMode.ServerWrite;

	public override string Value
	{
		get
		{
			if (ElementTMP != null)
			{
				return (ElementTMP.text);
			}
			else
			{
				return (Element.text);
			}
		}
		set
		{
			externalChange = true;
			if (ElementTMP != null)
			{
				ElementTMP.text = value;
			}
			else if (Element != null)
			{
				Element.text = value;
			}

			externalChange = false;
			OnSyncedValueChanged?.Invoke(value);
		}
	}

	private Text element;

	public Text Element
	{
		get
		{
			if (!element)
			{
				element = GetComponent<Text>();
			}
			return element;
		}
	}

	private TMPro.TMP_Text elementTMP;

	public TMPro.TMP_Text ElementTMP
	{
		get
		{
			if (!elementTMP)
			{
				elementTMP = GetComponent<TMPro.TMP_Text>();
			}
			return elementTMP;
		}
	}


	public override void ExecuteServer(ConnectedPlayer subject)
	{
	}
}