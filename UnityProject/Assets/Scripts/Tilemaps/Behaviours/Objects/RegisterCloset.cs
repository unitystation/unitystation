using UnityEngine;

namespace Objects
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(ClosetControl))]
	public class RegisterCloset : RegisterObject
	{
		[Tooltip("Type of closet, affects passability / collision detection when open vs. closed.")]
		public ClosetType closetType;

		/// <summary>
		/// Cached closet control for this closet
		/// </summary>
		private ClosetControl closetControl;

		protected override void Awake()
		{
			base.Awake();
			closetControl = GetComponent<ClosetControl>();
			OnParentChangeComplete.AddListener(ReparentContainedObjectsOnParentChangeComplete);
			closetControl.OnClosedChanged.AddListener(OnClosedChanged);
			OnClosedChanged(closetControl.IsClosed);
		}

		private void OnClosedChanged(bool isClosed)
		{
			if (closetType == ClosetType.LOCKER || closetType == ClosetType.SCANNER)
			{
				//become passable to bullets and people when open
				Passable = !isClosed;
				CrawlPassable = !isClosed;
				//switching to item layer if open so bullets pass through it
				if (Passable)
				{
					gameObject.layer = LayerMask.NameToLayer("Items");
				}
				else
				{
					gameObject.layer = LayerMask.NameToLayer("Machines");
				}

			}
		}

		private void ReparentContainedObjectsOnParentChangeComplete()
		{
			if (closetControl != null)
			{
				// update the parent of each of the items in the closet
				closetControl.OnParentChangeComplete(NetworkedMatrixNetId);
			}
		}
	}

	public enum ClosetType
	{
		LOCKER,
		CRATE,
		SCANNER,
		OTHER
	}
}
