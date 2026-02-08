using System.Collections.Generic;
using SecureStuff;
using UnityEngine;
using US13.Core.Initialisation;
using US13.Managers;
using US13.Variable_Viewer.BookViewer.ElementDisplay;
using Event = US13.Managers.Event;

namespace US13.Variable_Viewer
{
	public class VariableViewerManager : MonoBehaviour, IInitialise
	{
		public List<PageElement> AvailableElementsToInitialise;

		public InitialisationSystems Subsystem => InitialisationSystems.VariableViewerManager;

		public static bool TestLoad = false;
		public static bool INITEDED = false;
		void IInitialise.Initialise()
		{
			if (TestLoad == false)
			{
				VVUIElementHandler.ReSet();
				VVUIElementHandler.VariableViewerManager = this;
				VVUIElementHandler.Initialise(AvailableElementsToInitialise);
			}
			else
			{
				if (INITEDED == false)
				{
					VVUIElementHandler.ReSet();
					VVUIElementHandler.VariableViewerManager = this;
					VVUIElementHandler.Initialise(AvailableElementsToInitialise);
					INITEDED = true;
				}
			}

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
}