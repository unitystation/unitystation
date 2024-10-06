using Mirror;
using UnityEngine;

namespace Core.Physics
{
	public partial class UniversalObjectPhysics
	{
		// netid of the game object we are buckled to, NetId.Empty if not buckled
		[field: SyncVar(hook = nameof(SyncObjectIsBuckling))]
		public Physics.UniversalObjectPhysics ObjectIsBuckling { get; protected set; } //If your chair the person buckled to you

		public Physics.UniversalObjectPhysics BuckledToObject; //If you're a person the chair you are buckle to
		public bool IsBuckled => BuckledToObject != null;

		public Vector3 BuckleOffset = Vector3.zero;

		public virtual void BuckleToChange(Physics.UniversalObjectPhysics newBuckledTo) { }

		// syncvar hook invoked client side when the buckledTo changes
		private void SyncObjectIsBuckling(Physics.UniversalObjectPhysics oldBuckledTo, Physics.UniversalObjectPhysics newBuckledTo)
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

				ObjectIsBuckling.SetTransform(transform.localPosition + BuckleOffset, false);

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
			GameObjectExtensions.OrNull<Rotatable>(rotatable)?.FaceDirection(newDir);
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
		public void BuckleTo(Physics.UniversalObjectPhysics newBuckledTo)
		{
			if (newBuckledTo == null)
			{
				Unbuckle();
				return;
			}
			newBuckledTo.SetTransform(transform.localPosition + BuckleOffset, false);
			SyncObjectIsBuckling(ObjectIsBuckling, newBuckledTo);
			BuckleToChange(ObjectIsBuckling);
			ObjectIsBuckling.AppearAtWorldPositionServer(transform.localPosition + BuckleOffset.ToWorld(registerTile.Matrix));
		}
	}
}