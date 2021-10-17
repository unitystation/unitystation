using UnityEngine;


namespace Objects.Atmospherics
{
	/// <summary>
	/// Simple device that samples the ambient atmosphere for reporting to a connected <see cref="AirController"/>.
	/// </summary>
	public class AcuSensor : MonoBehaviour, IServerSpawn, IAcuControllable
	{
		public AcuSample AtmosphericSample => atmosphericSample.FromGasMix(metaNode.GasMix);

		private readonly AcuSample atmosphericSample = new AcuSample();
		private MetaDataNode metaNode;

		public void OnSpawnServer(SpawnInfo info)
		{
			var registerTile = gameObject.RegisterTile();
			var metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
			metaNode = metaDataLayer.Get(registerTile.LocalPositionServer, false);
			
			registerTile.OnLocalPositionChangedServer.AddListener((newLocalPosition) =>
			{
				metaNode = metaDataLayer.Get(newLocalPosition, false);
			});
		}

		// Don't care about the ACU operating mode.
		public void SetOperatingMode(AcuMode mode) { }
	}
}
