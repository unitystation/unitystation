namespace Messages.Server.SoundMessages
{
	/// <summary>
	/// Parameters to shake the ground when playing a sound.
	/// </summary>
	public struct ShakeParameters
	{
		/// <summary>
		/// Should the ground shake?
		/// </summary>
		public bool ShakeGround;

		/// <summary>
		/// At what intensity should the ground shake?
		/// </summary>
		public byte ShakeIntensity;

		/// <summary>
		/// At what distance should the shake be perceived?
		/// </summary>
		public int ShakeRange;

		/// <summary>
		/// Constructor for the ShakeParameters Struct
		/// </summary>
		public ShakeParameters(bool shakeGround, byte shakeIntensity, int shakeRange)
		{
			ShakeGround = shakeGround;
			ShakeIntensity = shakeIntensity;
			ShakeRange = shakeRange;
		}

		public override string ToString()
		{
			return $"{nameof(ShakeGround)}: {ShakeGround}, {nameof(ShakeIntensity)}: {ShakeIntensity}, {nameof(ShakeRange)}: {ShakeRange}";
		}
	}
}