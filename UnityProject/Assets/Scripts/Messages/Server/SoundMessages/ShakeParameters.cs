namespace Assets.Scripts.Messages.Server.SoundMessages
{
	/// <summary>
	/// Parameters to shake the ground when playing a sound.
	/// </summary>
	public class ShakeParameters
	{
		/// <summary>
		/// Should the ground shake?
		/// </summary>
		public bool ShakeGround = false;

		/// <summary>
		/// At what intensity should the ground shake?
		/// </summary>
		public byte ShakeIntensity = 64;

		/// <summary>
		/// At what distance should the shake be perceived?
		/// </summary>
		public int ShakeRange = 30;

		public override string ToString()
		{
			return $"{nameof(ShakeGround)}: {ShakeGround}, {nameof(ShakeIntensity)}: {ShakeIntensity}, {nameof(ShakeRange)}: {ShakeRange}";
		}
	}
}