using US13.Core.Transform;

namespace US13.Shuttles
{
	[System.Serializable]
	public class GuidanceBuoyMoveStep
	{
		//On set
		public bool UseConnectorAsCentreOfShuttle;
		public OrientationEnum DesiredFaceDirection = OrientationEnum.Default;


		//On Reach
		public ShuttleConnector ConnectTo;
		public GuidanceBuoy NextInLine;
	}
}

