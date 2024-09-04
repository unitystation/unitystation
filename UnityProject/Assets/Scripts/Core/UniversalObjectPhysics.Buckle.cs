using Mirror;

namespace Core
{
	public partial class UniversalObjectPhysics
	{
		// netid of the game object we are buckled to, NetId.Empty if not buckled
		[field: SyncVar(hook = nameof(SyncObjectIsBuckling))]
		public UniversalObjectPhysics ObjectIsBuckling { get; protected set; } //If your chair the person buckled to you

		public UniversalObjectPhysics BuckledToObject; //If you're a person the chair you are buckle to
		public bool IsBuckled => BuckledToObject != null;

		public virtual void BuckleToChange(UniversalObjectPhysics newBuckledTo) { }

		// syncvar hook invoked client side when the buckledTo changes
		private void SyncObjectIsBuckling(UniversalObjectPhysics oldBuckledTo, UniversalObjectPhysics newBuckledTo)
		{
			// unsub if we are subbed
			if (oldBuckledTo != null)
			{
				var directionalObject = this.GetComponent<Rotatable>();
				if (directionalObject != null)
				{
					directionalObject.OnRotationChange.RemoveListener(oldBuckledTo.OnBuckledObjectDirectionChange);
				}

				oldBuckledTo.BuckleToChange(null);
				oldBuckledTo.BuckledToObject = null;
			}

			ObjectIsBuckling = newBuckledTo;

			// sub
			if (ObjectIsBuckling != null)
			{
				ObjectIsBuckling.BuckledToObject = this;
				ObjectIsBuckling.BuckleToChange(this);
				var directionalObject = GetComponent<Rotatable>();
				if (directionalObject != null)
				{
					directionalObject.OnRotationChange.AddListener(newBuckledTo.OnBuckledObjectDirectionChange);
				}

				var directionalBuckledObject = ObjectIsBuckling.GetComponent<Rotatable>();
				if (directionalBuckledObject != null && rotatable != null)
				{
					directionalBuckledObject.FaceDirection(rotatable.CurrentDirection);
				}
			}
		}

		private void OnBuckledObjectDirectionChange(OrientationEnum newDir)
		{
			if (rotatable == null) rotatable = gameObject.GetComponent<Rotatable>();
			rotatable.OrNull()?.FaceDirection(newDir);
		}

		// ReSharper disable Unity.PerformanceAnalysis
		/// <summary>
		/// Server side logic for unbuckling a player
		/// </summary>
		[Server]
		public void Unbuckle()
		{
			SyncObjectIsBuckling(ObjectIsBuckling, null);
			BuckleToChange(ObjectIsBuckling);
		}

		/// <summary>
		/// Server side logic for buckling a player
		/// </summary>
		[Server]
		public void BuckleTo(UniversalObjectPhysics newBuckledTo)
		{
			if (newBuckledTo == null)
			{
				Unbuckle();
				return;
			}
			SyncObjectIsBuckling(ObjectIsBuckling, newBuckledTo);
			BuckleToChange(ObjectIsBuckling);
			ObjectIsBuckling.AppearAtWorldPositionServer(transform.position);
		}
	}
}