namespace Tilemaps.Behaviours.Meta.Data
{
	public struct AirTile
	{
		public float Temperature;
		public float Moles;
		public bool Obstructed;
		public bool Space;

		public AirTile(float temperature, float moles, bool obstructed, bool space)
		{
			Temperature = temperature;
			Moles = moles;
			Obstructed = obstructed;
			Space = space;
		}

		public float Pressure => Moles * Gas.R * Temperature / 2 / 1000;

		public static readonly AirTile SpaceTile = new AirTile(2.7f, 0.000000316f, false, true);
	}
}