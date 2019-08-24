using System;
using System.Collections.Generic;
using UnityEngine;

public static class VVUIElementHandler
{
	public static VariableViewerManager VariableViewerManager;
	public static Dictionary<PageElementEnum, List<PageElement>> PoolDictionary = new Dictionary<PageElementEnum, List<PageElement>>();
	public static Dictionary<PageElementEnum, PageElement> AvailableElements = new Dictionary<PageElementEnum, PageElement>();
	public static List<PageElement> CurrentlyOpen = new List<PageElement>();
	public static List<PageElement> ToDestroy = new List<PageElement>();

	public static void ProcessElement(GameObject DynamicPanel, VariableViewerNetworking.NetFriendlyPage Page = null, VariableViewerNetworking.NetFriendlySentence Sentence = null, bool iskey = false)
	{
		Type ValueType;
		if (Page != null)
		{
			ValueType = Librarian.UEGetType(Page.VariableType);
		}
		else
		{
			if (iskey)
			{
				ValueType = Librarian.UEGetType(Sentence.KeyVariableType);
			}
			else
			{
				ValueType = Librarian.UEGetType(Sentence.ValueVariableType);
			}
		}

		foreach (PageElementEnum _Enum in Enum.GetValues(typeof(PageElementEnum)))
		{
			if (AvailableElements[_Enum].IsThisType(ValueType))
			{
				PageElement _PageElement;
				if (PoolDictionary[_Enum].Count > 0)
				{
					_PageElement = PoolDictionary[_Enum][0];
					PoolDictionary[_Enum].RemoveAt(0);
					_PageElement.gameObject.SetActive(true);
				}
				else
				{
					_PageElement = GameObject.Instantiate(AvailableElements[_Enum]) as PageElement;
				}
				_PageElement.transform.SetParent(DynamicPanel.transform);
				_PageElement.transform.localScale = Vector3.one;
				_PageElement.SetUpValues(ValueType, Page, Sentence, iskey);
				CurrentlyOpen.Add(_PageElement);
				break;
			}
		}
	}
	public static void Pool()
	{
		while (CurrentlyOpen.Count > 0)
		{
			if (CurrentlyOpen[0].IsPoolble)
			{
				CurrentlyOpen[0].gameObject.SetActive(false);
				PoolDictionary[CurrentlyOpen[0].PageElementType].Add(CurrentlyOpen[0]);
				CurrentlyOpen[0].transform.SetParent(VariableViewerManager.transform);
			}
			else
			{
				ToDestroy.Add(CurrentlyOpen[0]);
			}
			CurrentlyOpen.RemoveAt(0);
		}
		foreach (var Element in ToDestroy)
		{
			GameObject.Destroy(Element.gameObject);
		}
		ToDestroy.Clear();
	}

	public static void Initialise(List<PageElement> PageElements)
	{
		foreach (var Element in PageElements)
		{
			PoolDictionary[Element.PageElementType] = new List<PageElement>();
			AvailableElements[Element.PageElementType] = Element;
		}
	}
	public static string ReturnCorrectString(VariableViewerNetworking.NetFriendlyPage Page = null, VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
	{
		if (Page != null)
		{
			return (Page.Variable);
		}
		else
		{
			if (Iskey)
			{
				return (Sentence.KeyVariable);
			}
			else
			{
				return (Sentence.ValueVariable);
			}
		}
	}
}

public enum PageElementEnum
{
	Bool,
	Collection,
	Enum,
	Class,
	InputField, //This has to be the last option
}