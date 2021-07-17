public static class AtmosConstants
{
	public const float MinPressureDifference = 0.0001f;
	/// <summary>
	/// Minimal pressure difference to occur in order to be registered as Wind
	/// </summary>
	public const float MinWindForce = 3f;
	/// <summary>
	/// Minimal force(speed) to push things with when exposed to wind.
	/// Doesn't push if force is less than that
	/// </summary>
	public const float MinPushForce = 2.5f;
	public const float TileVolume = 2.5f;

	public const float HAZARD_HIGH_PRESSURE = 550;
	public const float WARNING_HIGH_PRESSURE = 325;
	public const float WARNING_LOW_PRESSURE = 50;
	public const float HAZARD_LOW_PRESSURE = 20;

	public const float MINIMUM_OXYGEN_PRESSURE = 16;
	public const float LOW_PRESSURE_DAMAGE = 4;
	public const float MAX_HIGH_PRESSURE_DAMAGE = 4;
	public const float PRESSURE_DAMAGE_COEFFICIENT = 4;
	public const float BREATH_VOLUME = 0.00005f;
	public const float ONE_ATMOSPHERE = 101.325f;

	public const float MINIMUM_HEAT_CAPACITY = 0.0003f;
}