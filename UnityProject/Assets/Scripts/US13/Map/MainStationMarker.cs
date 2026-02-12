using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Tilemaps.Behaviours.Meta;

namespace US13.Map
{
	public class MainStationMarker : ItemMatrixSystemInit
	{
		public override void Start()
		{
			base.Start();
			if (CustomNetworkManager.IsServer)
			{
				MatrixManager.Instance.InternalMainStationMatrix = metaTileMap.matrix;
				metaTileMap.matrix.NetworkedMatrix.MatrixSync.IsMainStationMatrix = true;
			}
		}
	}
}
