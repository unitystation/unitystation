using System;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client.VariableViewer;


namespace AdminTools.VariableViewer
{
	public class GUI_P_Colour : PageElement
	{
		public ColorPicker ColorPicker;
		public GameObject ColourPickerwindow;

		public bool IsSentence;
		public bool iskey;
		public override PageElementEnum PageElementType => PageElementEnum.Colour;
		public Color thisColor = Color.white;

		public bool IgnoreUpdates = false;

		public HashSet<Type> CanDo = new HashSet<Type>()
		{
			typeof(Color),
		};

		public override HashSet<Type> GetCompatibleTypes()
		{
			return CanDo;
		}

		public override void SetUpValues(Type ValueType,
			VariableViewerNetworking.NetFriendlyPage Page = null,
			VariableViewerNetworking.NetFriendlySentence Sentence = null,
			bool Iskey = false)
		{
			if (Page != null)
			{
				PageID = Page.ID;
				SentenceID = 0;
				IsSentence = false;
				iskey = false;
			}
			else
			{
				PageID = Sentence.OnPageID;
				SentenceID = Sentence.SentenceID;
				IsSentence = true;
				iskey = Iskey;
			}

			var Data = VVUIElementHandler.ReturnCorrectString(Page, Sentence, Iskey);
			DeSerialise(Data, true);
		}

		public void UpdateColour()
		{
			if (IgnoreUpdates) return;
			if (PageID != 0)
			{
				thisColor = ColorPicker.CurrentColor;
				string Outstring = "" + Convert.ToChar(Mathf.RoundToInt(thisColor.r * 255));
				Outstring += Convert.ToChar(Mathf.RoundToInt(thisColor.g * 255));
				Outstring += Convert.ToChar(Mathf.RoundToInt(thisColor.b * 255));
				Outstring += Convert.ToChar(Mathf.RoundToInt(thisColor.a * 255));

				RequestChangeVariableNetMessage.Send(PageID, Outstring, UISendToClientToggle.toggle);
			}
		}

		public void ToggleObject()
		{
			ColourPickerwindow.SetActive(!ColourPickerwindow.activeSelf);
		}

		public override void Pool()
		{
			IsSentence = false;
			iskey = false;
		}

		public override string Serialise(object Data)
		{
			var inType = Data.GetType();
			if (CanDo.Contains(inType))
			{
				Color Color = (Color) Data;
				string newstring = "" + Convert.ToChar(Mathf.RoundToInt((float)Color.r * 255));
				newstring += Convert.ToChar(Mathf.RoundToInt((float)Color.g * 255));
				newstring += Convert.ToChar(Mathf.RoundToInt((float)Color.b * 255));
				newstring += Convert.ToChar(Mathf.RoundToInt((float)Color.a * 255));
				return newstring;
			}

			return Data.ToString();
		}

		public override object DeSerialise(string Data, bool SetUI = false)
		{
			Color TheColour = Color.white;
			TheColour.r = Data[0] / 255f;
			TheColour.g = Data[1] / 255f;
			TheColour.b = Data[2] / 255f;
			TheColour.a = Data[3] / 255f;
			if (SetUI)
			{
				IgnoreUpdates = true;
				thisColor = TheColour;
				ColorPicker.CurrentColor = TheColour;
				IgnoreUpdates = false;
			}

			return TheColour;
		}
	}
}
