using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Messages.Client.VariableViewer;
using ScriptableObjects;
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

		public static Dictionary<Type, Dictionary<string, ISOTracker>> OptimiseIndividualDropDownOptions =
			new Dictionary<Type, Dictionary<string, ISOTracker>>();

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


		public override bool CanDeserialise(Type TType)
		{
			return typeof(ISOTracker).IsAssignableFrom(TType);
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
				SentenceID = uint.MaxValue;
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
			var usedValueType = ValueType;
			if (ValueType.IsSubclassOf(typeof(LayerTile)))
			{
				usedValueType = typeof(LayerTile);
			}


			var Found = IndividualDropDownOptions[usedValueType].FirstOrDefault(x => x.ForeverID == data);
			SetupValues(Found);

			if (data != null)
			{
				SetupValues(Found);
			}
			else
			{
				SetupValues(null);
			}

			ActiveList = IndividualDropDownOptions[usedValueType];
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
			RequestChangeVariableNetMessage.Send(PageID, Serialise(change) , UISendToClientToggle.toggle, SentenceID);
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

		private static bool InheritsFrom(Type type, Type baseType)
		{
			while (type != null && type != typeof(object))
			{
				if (type == baseType)
				{
					return true;
				}
				type = type.BaseType;
			}
			return false;
		}

		public static Type GetImmediateBaseType(Type type, Type stopType)
		{
			// Get the base type
			Type CorrectType = type;
			Type baseType = type.BaseType;
			while (baseType != stopType)
			{
				CorrectType = baseType;
				baseType = baseType.BaseType;
			}

			return CorrectType;
		}

		public void InitialiseIndividualDropDownOptions()
		{
			foreach (var SO in SOListTracker.Instance.SOTrackers)
			{
				if (SO == null) continue;
				var Type = ((object) SO).GetType();

				if (InheritsFrom(Type, typeof(SOTracker)))
				{
					Type = GetImmediateBaseType(Type, typeof(SOTracker));
				}


				if (IndividualDropDownOptions.ContainsKey(Type) == false)
				{
					IndividualDropDownOptions[Type] = new List<ISOTracker>();
					OptimiseIndividualDropDownOptions[Type] = new Dictionary<string, ISOTracker>();
				}

				IndividualDropDownOptions[Type].Add(SO);
				OptimiseIndividualDropDownOptions[Type][SO.ForeverID] = SO;
			}

			var TypeTile = typeof(LayerTile);
			foreach (var TileType in TileManager.Instance.Tiles)
			{
				foreach (var Tile in TileType.Value)
				{
					if (IndividualDropDownOptions.ContainsKey(TypeTile) == false)
					{
						IndividualDropDownOptions[TypeTile] = new List<ISOTracker>();
						OptimiseIndividualDropDownOptions[TypeTile] = new Dictionary<string, ISOTracker>();
					}
					IndividualDropDownOptions[TypeTile].Add(Tile.Value);
					OptimiseIndividualDropDownOptions[TypeTile][Tile.Value.ForeverID] = Tile.Value;
				}
			}


		}

		public override object DeSerialise(string StringVariable, Type InType, bool SetUI = false)
		{
			if (IndividualDropDownOptions.Count == 0)
			{
				InitialiseIndividualDropDownOptions();
			}

			return OptimiseIndividualDropDownOptions[InType].GetValueOrDefault(StringVariable);
		}

		public override void Pool()
		{
			base.Pool();
		}
	}
}