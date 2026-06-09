using System;
using UnityEngine;
using US13.Systems.InGameEvents;

namespace US13.UI.Systems.AdminTools
{
	/// <summary>
	/// The association of a parameter page type and the UI page itself
	/// </summary>
	[Serializable]
	public struct EventParameterPage
	{
		/// <summary>
		/// The parameter page type.  You should associate to your EventScriptBase derived component.
		/// </summary>
		public ParametersPageType ParametersPageType;

		/// <summary>
		/// The UI page to show
		/// </summary>
		public GameObject ParameterPage;
	}

	/// <summary>
	/// List of event parameter page type and the UI page itself
	/// </summary>
	public class EventParameterPages : MonoBehaviour
	{
		public EventParameterPage[] eventParameterPages;
	}
}
