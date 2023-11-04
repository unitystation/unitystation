using System;
using Logs;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Core.NetUI
{
	///Text label, not modifiable by clients directly
	//[RequireComponent(typeof(TMP_Text))]
	[Serializable]
	public class NetText_label : NetUIStringElement
	{
		/// <summary>
		/// Invoked when the value synced between client / server is updated.
		/// </summary>
		[NonSerialized]
		public StringEvent OnSyncedValueChanged = new StringEvent();

		public override ElementMode InteractionMode => ElementMode.ServerWrite;

		//Do not use this!!!  use MasterSetValue
		public override string Value {
			get => ElementTmp != null ? ElementTmp.text : Element.text;
			protected set {
				externalChange = true;
				if (ElementTmp != null)
				{
					ElementTmp.text = value;
				}
				else if (Element != null)
				{
					Element.text = value;
				}
				else
				{
					Loggy.LogError($"Both Text and TMPText were null on {gameObject.name}, check stacktrace to see exact location");
				}

				externalChange = false;
				OnSyncedValueChanged?.Invoke(value);
			}
		}

		public Text Element
		{
			get
			{
				if (element == null)
				{
					element = GetComponent<Text>();
				}

				return element;
			}

			set => element = value;
		}

		public TMP_Text ElementTmp
		{
			get
			{
				if (elementTmp == null)
				{
					elementTmp = GetComponent<TMP_Text>();
				}

				return elementTmp;
			}

			set => elementTmp = value;
		}

		private Text element;
		private TMP_Text elementTmp;
	}
}
