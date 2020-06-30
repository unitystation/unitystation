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

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundEnded, Librarian.Reset);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundEnded, Librarian.Reset);
	}

}