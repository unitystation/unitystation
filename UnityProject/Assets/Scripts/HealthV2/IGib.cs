namespace HealthV2
{
	/// <summary>
	/// An interface for creatures and objects that trigger gibbing/destruction logic.
	/// </summary>
	public interface IGib
	{
		/// <summary>
		/// Called when the creature/object is gibbed.
		/// </summary>
		/// <param name="ignoreNoGibRule">If true, the NoGibbing server rule is ignored. Useful for things such as admins.</param>
		public void OnGib(bool ignoreNoGibRule = false);
	}
}