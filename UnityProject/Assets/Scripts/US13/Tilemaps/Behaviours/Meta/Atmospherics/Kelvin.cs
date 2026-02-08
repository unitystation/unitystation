namespace US13.Tilemaps.Behaviours.Meta.Atmospherics
{
	public static class Kelvin
	{
		public static float FromC(float temp) => TemperatureUtils.ToKelvin(temp, TemeratureUnits.C);
	}
}
