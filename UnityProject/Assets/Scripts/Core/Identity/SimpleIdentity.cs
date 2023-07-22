using Mirror;
using UnityEngine;

namespace Core.Identity
{
	/// <summary>
	/// Implementation for the IIdentifiable interface. This is the most basic implementation of an identity and
	/// applies to most entities, such as objects, pickupable items and old mobs.
	/// </summary>
	[DisallowMultipleComponent]
	public class SimpleIdentity: NetworkBehaviour, IIdentifiable, IExaminable
	{
		[SerializeField] private string initialName = "Unknown";
		[SyncVar(hook = nameof(SetDisplayName))]
		private string displayName;

		[SerializeField]
		[TextArea(3, 5)]
		[Tooltip("Most basic description of this entity. Will be the first thing that shows when examining it." +
		         " {0} will be replaced with the current name of the entity.")]
		private string initialDescription = "This is a {0}.";

		[SerializeField]
		[Tooltip("If true, this entity can be labelled with a Hand Labeler. This can change its name for everyone who sees it.")]
		private bool canBeLabelled = true;

		public string DisplayName => displayName;
		public string InitialName => initialName;

		public override void OnStartServer()
		{
			base.OnStartServer();
			SetDisplayName(displayName, initialName);
		}

		public void SetInitialName(string newName)
		{
			initialName = newName;
		}

		// [Server]
		public void SetDisplayName(string oldName, string newName)
		{
			displayName = newName;
		}

		public void SetDescription(string oldDescription, string newDescription)
		{
			initialDescription = newDescription;
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return initialDescription.Replace("{0}", displayName);
		}
	}
}