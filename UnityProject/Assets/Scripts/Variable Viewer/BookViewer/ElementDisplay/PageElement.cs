using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class PageElement : MonoBehaviour
{
	public ulong PageID;
	public uint SentenceID;
	public PageElementEnum PageElementType;
	public bool IsPoolble = true;

	public virtual bool IsThisType(Type TType) {
		return (false);
	}

	public virtual void SetUpValues(Type ValueType,VariableViewerNetworking.NetFriendlyPage Page = null, VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
	{
		if (Page != null)
		{
			PageID = Page.ID;
		}
		else {
			SentenceID = Sentence.SentenceID;
		}

	}

	public virtual void Pool()
	{
	}
}
 