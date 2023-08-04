using System.Collections.Generic;
using UnityEngine;

namespace UI.Core.RightClick
{
	public interface IRightClickMenu
	{
		/// <summary>
		/// For some reason, Unity keeps throwing an NRE when accessing the gameObject property when this
		/// interface is implemented. So we assign Self on awake to avoid those annoying NRE issues in controllers.
		/// </summary>
		public GameObject Self { get; protected set; }

		/// <summary>
		/// The list of items that can be interacted with in the menu.
		/// </summary>
		public List<RightClickMenuItem> Items { get; set; }

		/// <summary>
		/// Shows the right click menu.
		/// </summary>
		/// <param name="items">The list of items that can be interacted with</param>
		/// <param name="radialPosition">The position of the menu on screen relative to world space.</param>
		/// <param name="radialOptions"></param>
		public void SetupMenu(List<RightClickMenuItem> items, IRadialPosition radialPosition,
			RightClickRadialOptions radialOptions);
	}
}