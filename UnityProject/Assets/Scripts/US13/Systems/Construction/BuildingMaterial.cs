using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.UI.Systems;

namespace US13.Systems.Construction
{

	/// <summary>
	/// Indicates an item which can be activated in order to display a build menu and build something
	/// if there is sufficient quantity in hand.
	/// </summary>
	public class BuildingMaterial : MonoBehaviour, IClientInteractable<HandActivate>
	{

		[Tooltip("List of things possible to build with this material.")] [SerializeField]
		private BuildList buildList = null;

		/// <summary>
		/// List of things that can be built.
		/// </summary>
		public BuildList BuildList => buildList;

		public bool Interact(HandActivate interaction)
		{
			//nothing to show
			if (buildList == null) return false;

			//client-side only since it merely displays a menu - the request is sent to the
			//server in a net message indicating what was chosen.
			UIManager.BuildMenu.ShowBuildMenu(this);

			return true;
		}
	}
}
