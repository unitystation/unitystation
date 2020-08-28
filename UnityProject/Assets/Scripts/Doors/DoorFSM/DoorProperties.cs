namespace Doors.DoorFSM
{
	public class DoorProperties
	{
		public string CurrentLayer;
		public bool DoesPressureWarning = false;
		public bool HasPower = true;
		public bool IsWeld = false;
		public bool HasPanelExposed = false;
		public bool HasBoltLights = true;
		public bool HasBoltsDown = false;
		public bool IsAutomatic = true;
		public bool HasAIControl = true;
		public float AutoCloseTime = 5;
	}
}