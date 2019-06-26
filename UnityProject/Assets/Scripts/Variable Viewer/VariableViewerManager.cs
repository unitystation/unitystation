using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;
using Newtonsoft.Json;

public class VariableViewerManager : MonoBehaviour
{
	public List<PageElement> AvailableElementsToInitialise;
	   
    void Start()
	{
		VVUIElementHandler.VariableViewerManager = this;
		VVUIElementHandler.initialise(AvailableElementsToInitialise);
	}

	// Update is called once per frame
	//void Update()
	//{

	//}
}
