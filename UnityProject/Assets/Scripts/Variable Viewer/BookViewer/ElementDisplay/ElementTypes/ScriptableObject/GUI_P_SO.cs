using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Messages.Client.VariableViewer;
using SecureStuff;
using Tiles;
using TMPro;
using UISearchWithPreview;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools.VariableViewer
{

	//TODO
	//Better position for SearchWithPreview
	//Overflow??

	public class GUI_P_SO : PageElement
	{
		public override PageElementEnum PageElementType => PageElementEnum.ScriptableObject;

		public static Dictionary<Type, List<ISOTracker>> IndividualDropDownOptions =
			new Dictionary<Type, List<ISOTracker>>();

		public List<ISOTracker> ActiveList;

		public SearchWithPreview DropDownSearch;

		public TMP_Text DisplayText;

		public SpriteHandler Preview;

		public bool IsSentence;
		public bool iskey;

		public void Awake()
		{
			DropDownSearch.ItemChosen += SetValue;
		}


		public override bool IsThisType(Type TType)
		{
			return typeof(ISOTracker).IsAssignableFrom(TType);
		}

		public override void SetUpValues(
			Type ValueType, VariableViewerNetworking.NetFriendlyPage Page = null,
			VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
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

			if (IndividualDropDownOptions.Count == 0)
			{
				InitialiseIndividualDropDownOptions();
			}

			base.SetUpValues(ValueType, Page, Sentence, Iskey);
			var data = VVUIElementHandler.ReturnCorrectString(Page, Sentence, Iskey);
			//TODO Populate drop-down

			var Found = IndividualDropDownOptions[ValueType].FirstOrDefault(x => x.ForeverID == data);
			SetupValues(Found);

			if (data != null)
			{
				SetupValues(Found);
			}
			else
			{
				SetupValues(null);
			}

			ActiveList = IndividualDropDownOptions[ValueType];
		}

		public void SetupValues(ISearchSpritePreview ISearchSpritePreview)
		{
			DisplayText.text = ISearchSpritePreview == null ? "Null" : ISearchSpritePreview.Name;
			if (ISearchSpritePreview == null)
			{
				Preview.PushClear();
			}
			else
			{
				if (ISearchSpritePreview.Sprite == null)
				{
					Preview.SetSprite(ISearchSpritePreview.OldSprite);
				}
				else
				{
					Preview.SetSpriteSO(ISearchSpritePreview.Sprite);
				}

			}
		}

		public void Open()
		{
			DropDownSearch.Setup(ActiveList.Select( x => (ISearchSpritePreview)x).ToList());
		}

		public void SetValue(ISearchSpritePreview change)
		{
			SetupValues(change);
			if (PageID != 0)
			{
				RequestChangeVariableNetMessage.Send(PageID, Serialise(change), UISendToClientToggle.toggle);
			}
		}

		public void RequestOpenBookOnPage()
		{
			OpenPageValueNetMessage.Send(PageID, SentenceID, IsSentence, iskey);
		}


		public override string Serialise(object Data)
		{
			if (IndividualDropDownOptions.Count == 0)
			{
				InitialiseIndividualDropDownOptions();
			}

			if (Data is string)
			{
				return null;
			}

			return ((ISOTracker)Data)?.ForeverID;
		}

		public void InitialiseIndividualDropDownOptions()
		{
			foreach (var SO in SOListTracker.Instance.SOTrackers)
			{
				if (SO == null) continue;
				var Type = ((object) SO).GetType();
				if (IndividualDropDownOptions.ContainsKey(Type) == false)
				{
					IndividualDropDownOptions[Type] = new List<ISOTracker>();
				}
				IndividualDropDownOptions[Type].Add(SO);
			}

			var TypeTile = typeof(LayerTile);
			foreach (var TileType in 	TileManager.Instance.Tiles)
			{
				foreach (var Tile in TileType.Value)
				{
					if (IndividualDropDownOptions.ContainsKey(TypeTile) == false)
					{
						IndividualDropDownOptions[TypeTile] = new List<ISOTracker>();
					}
					IndividualDropDownOptions[TypeTile].Add(Tile.Value);
				}
			}


		}

		public override object DeSerialise(string StringVariable, Type InType, object InObject, bool SetUI = false)
		{
			return IndividualDropDownOptions[InType].FirstOrDefault(x => x.ForeverID == StringVariable);
		}

		public override void Pool()
		{
			base.Pool();
		}
	}
}