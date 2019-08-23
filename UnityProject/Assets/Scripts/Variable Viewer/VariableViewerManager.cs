using System.Collections.Generic;
using UnityEngine;

public class VariableViewerManager : MonoBehaviour
{
	public List<PageElement> AvailableElementsToInitialise;

	void Start()
	{
		VVUIElementHandler.VariableViewerManager = this;
		VVUIElementHandler.Initialise(AvailableElementsToInitialise);
	}
}