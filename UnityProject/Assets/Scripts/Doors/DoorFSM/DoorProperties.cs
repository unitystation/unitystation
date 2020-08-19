namespace Doors.DoorFSM
{
	public class DoorProperties
	{
		public DoorSpriteData doorSpriteData;
		public string CurrentLayer;
		public bool DoesPressureWarning = false;
		public bool HasPower = true;
		public bool IsWeld = false;
		public bool HasPanelExposed = false;
		public bool HasBoltLights = true;
		public bool HasBoltsDown = false;
		public bool IsAutomatic = true;
		public float AutoCloseTime = 5;

		public static DoorProperties operator +(DoorProperties a, DoorProperties b)
		{
			var properties = new DoorProperties
			{
				doorSpriteData = b.doorSpriteData,
				CurrentLayer = b.CurrentLayer,
				DoesPressureWarning = b.DoesPressureWarning,
				IsWeld = b.IsWeld,
				HasPanelExposed = b.HasPanelExposed,
				HasBoltLights = b.HasBoltLights,
				HasBoltsDown = b.HasBoltsDown,
				IsAutomatic = b.IsAutomatic,
				AutoCloseTime = b.AutoCloseTime
			};

			return properties;
		}
	}
}