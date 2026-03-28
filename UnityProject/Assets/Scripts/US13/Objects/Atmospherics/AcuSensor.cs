using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers.MatrixManager;
using US13.Tilemaps.Behaviours.Meta;
using Util;

namespace US13.Objects.Atmospherics
{
	/// <summary>
	/// Simple device that samples the ambient atmosphere for reporting to a connected <see cref="AirController"/>.
	/// </summary>
	public class AcuSensor : MonoBehaviour, IServerSpawn, IAcuControllable
	{
		public AcuSample AtmosphericSample => atmosphericSample.FromGasMix(metaNode.GasMixLocal);

		private readonly AcuSample atmosphericSample = new AcuSample();
		private MetaDataNode metaNode;

		public void OnSpawnServer(SpawnInfo info)
		{
			var registerTile = gameObject.RegisterTile();
			metaNode = MatrixManager.GetMetaDataAt(registerTile.WorldPosition);

			registerTile.OnLocalPositionChangedServer.AddListener((newLocalPosition) =>
			{
				metaNode = MatrixManager.GetMetaDataAt(registerTile.WorldPosition);
			});
		}

		// Don't care about the ACU operating mode.
		public void SetOperatingMode(AcuMode mode, bool SetBypower) { }
	}
}
