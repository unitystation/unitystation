using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;
using Newtonsoft.Json;

public class SUB_ElementHandler : MonoBehaviour
{
	public GameObject DynamicSizePanel;

	public VariableViewerNetworking.NetFriendlySentence Sentence;

	public void ValueSetUp()
	{
		VVUIElementHandler.ProcessElement(DynamicSizePanel, Sentence: Sentence, iskey: false);
		if (Sentence.KeyVariableType != "")
		{
			VVUIElementHandler.ProcessElement(DynamicSizePanel, Sentence: Sentence, iskey: true);
		}
	}

}
