using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Object = System.Object;


public class PageElement : MonoBehaviour
{
	public ulong PageID;
	public uint SentenceID = uint.MaxValue;
	public virtual PageElementEnum PageElementType => PageElementEnum.InputField;
	public bool IsPoolble = true;

	public virtual HashSet<Type> GetCompatibleTypes()
	{
		return (new HashSet<Type>());
	}

	public virtual bool CanDeserialise(Type TType)
	{
		return (false);
	}

	public virtual bool IsThisType(Type TType)
	{
		return (false);
	}

	public virtual void SetUpValues(Type ValueType,
		VariableViewerNetworking.NetFriendlyPage Page = null,
		VariableViewerNetworking.NetFriendlySentence Sentence = null,
		bool Iskey = false)
	{
		if (Page != null)
		{
			PageID = Page.ID;
		}
		else
		{
			PageID = Sentence.OnPageID;
			SentenceID = Sentence.SentenceID;
		}
	}

	public virtual void Pool()
	{
	}

	public virtual string Serialise(object Data)
	{
		if (Data == null)
		{
			return "null";
		}
		else
		{
			return (Data.ToString());
		}

	}

	public virtual object DeSerialise(string StringVariable, Type InType, bool SetUI = false)
	{
		if (InType.IsEnum)
		{
			return Enum.Parse(InType, StringVariable);
		}
		else
		{
			return null;
		}
	}
}