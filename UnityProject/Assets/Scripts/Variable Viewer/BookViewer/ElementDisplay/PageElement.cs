using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class PageElement : MonoBehaviour
{
	public PageElementEnum PageElementType;
	public bool IsPoolble = true;

	public virtual bool IsThisType(Type TType) {
		return (false);
	}

	public virtual void SetUpValues(Type ValueType,VariableViewerNetworking.NetFriendlyPage Page = null, VariableViewerNetworking.NetFriendlySentence Sentence = null, bool Iskey = false)
	{
	}
}
 