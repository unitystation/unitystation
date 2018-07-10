namespace Tilemaps.Behaviours.Meta.Data
{
	public enum AtmosState
	{
		None, Edge, Updating
	}
	
	public class AtmosValues
	{
		public AtmosState State = AtmosState.None;

		public float[] AirMix = new float[Gas.Count];

		private float _moles;
		private float _temperature;

		public float Temperature
		{
			get { return _temperature; }
			set
			{
				_temperature = value;
				CalcPressure();
			}
		}

		public float Moles
		{
			get { return _moles; }
			set
			{
				_moles = value;
				CalcPressure();
			}
		}

		public float Pressure { get; private set; }

		private void CalcPressure()
		{
			Pressure = Moles * Gas.R * Temperature / 2 / 1000;
		}
	}
}