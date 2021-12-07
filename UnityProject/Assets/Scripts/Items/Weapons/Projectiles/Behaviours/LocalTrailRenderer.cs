using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Trail renderer which draws a trail behind the bullet in the bullet's parents local space, so it looks
	/// good even when on a moving matrix.
	///
	/// Credit to Eric Hodgson, 2017, for the code I repurposed to build this behavior.
	/// (from this post https://forum.unity.com/threads/trail-renderer-local-space.97756/)
	/// </summary>
	[RequireComponent(typeof(LineRenderer))]
	[RequireComponent(typeof(Bullet))]
	public class LocalTrailRenderer : MonoBehaviour, IOnShoot, IOnDespawn
	{

		/// <summary>
		/// Object leaving the trail
		/// </summary>
		private Transform objToFollow;

		/// <summary>
		/// Cached lineRenderer
		/// </summary>
		private LineRenderer lineRenderer;

		[Tooltip("How many seconds between drawing new segments for the trail.")]
		public float secondsPerSegment = 0.05f;

		[Tooltip("Whether the length of the trail should be limited")]
		public bool limitTrailLength = false;   // Toggle this to make trail be a finite number of segments
		[Tooltip("Number of segments the trail line should have. Sets the length of the trail.")]
		public int maxPositions = 10;           // Set the number of segments here
		private bool isShooting;
		private float secondsSinceLastSegment;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			ShotStarted();
		}

		/// <summary>
		/// BulletBehavior invokes this when a shot has started. Starts rendering the trail.
		/// </summary>
		public void ShotStarted()
		{
			isShooting = true;
			Reset();
		}

		public void OnDespawn(MatrixManager.CustomPhysicsHit hit, Vector2 point)
		{
			ShotDone();
		}

		/// <summary>
		/// Stops rendering the trail. Invoked by BulletBehavior when shot is complete.
		/// </summary>
		public void ShotDone()
		{
			isShooting = false;
		}

		private void Awake()
		{
			//using Awake instead of Start for init since this is managed by an object pool
			if (lineRenderer == null)
			{
				lineRenderer = GetComponent<LineRenderer>();
			}

			if (objToFollow == null)
			{
				//we follow the rigidbody only, not the root transform.
				objToFollow = transform.GetComponentInChildren<MovingProjectile>().transform;
			}
			Reset();
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void Reset() {
			// Wipe out any old positions in the LineRenderer
			lineRenderer.positionCount = 0;
			secondsSinceLastSegment = 0;
			// Then set the first position to our object's current local position
			AddPoint(objToFollow.localPosition);
		}

		private void UpdateMe() {
			if (isShooting)
			{
				//check if enough time has elapsed to draw the next segment
				secondsSinceLastSegment += Time.deltaTime;
				if (secondsSinceLastSegment > secondsPerSegment)
				{
					// ..and add the point to the trail if so
					AddPoint(objToFollow.localPosition);

				}
			}
		}

		// Add a new point to the line renderer on demand
		private void AddPoint(Vector3 newPoint) {
			secondsSinceLastSegment = 0;
			// Increase the number of positions to render by 1
			lineRenderer.positionCount += 1;
			// Set the new, last item in the Vector3 list to our new point
			lineRenderer.SetPosition(lineRenderer.positionCount - 1, newPoint);

			// Check to see if the list is too long
			if (limitTrailLength && lineRenderer.positionCount > maxPositions) {
				// ...and discard old positions if necessary
				TruncatePositions(maxPositions);
			}
		}

		// Shorten position list to the desired amount, discarding old values
		private void TruncatePositions(int newLength) {
			// Create a temporary list of the desired length
			Vector3[] tempList = new Vector3[newLength];
			// Calculate how many extra items will need to be cut out from the original list
			int nExtraItems = lineRenderer.positionCount - newLength;
			// Loop through original list and add newest X items to temp list
			for (int i=0; i<newLength; i++) {
				// shift index by nExtraItems... e.g., if 2 extras, start at index 2 instead of index 0
				tempList[i] = lineRenderer.GetPosition(i + nExtraItems);
			}

			// Set the LineRenderer's position list length to the appropriate amount
			lineRenderer.positionCount = newLength;
			// ...and use our tempList to fill it's positions appropriately
			lineRenderer.SetPositions(tempList);
		}
	}
}
