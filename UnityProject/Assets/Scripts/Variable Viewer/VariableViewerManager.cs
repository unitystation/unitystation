using System;
using System.Collections.Generic;
using Initialisation;
using SecureStuff;
using UnityEngine;

public class VariableViewerManager : MonoBehaviour, IInitialise
{
	public List<PageElement> AvailableElementsToInitialise;

	public InitialisationSystems Subsystem => InitialisationSystems.VariableViewerManager;

	void IInitialise.Initialise()
	{
		MapSaver.MapSaver.CodeClass.ThisCodeClass.Reset();
		VVUIElementHandler.ReSet();
		VVUIElementHandler.VariableViewerManager = this;
		VVUIElementHandler.Initialise(AvailableElementsToInitialise);
	}

	void OnEnable()
	{
		EventManager.AddHandler(Event.RoundEnded, Librarian.Reset);
	}

	void OnDisable()
	{
		Librarian.Reset();
		EventManager.RemoveHandler(Event.RoundEnded, Librarian.Reset);
	}
}