using UnityEngine;
using US13.ScriptableObjects.Atmospherics;

namespace US13.Tilemaps.Behaviours.Meta.Atmospherics
{
	public class MatrixGasSettings : ItemMatrixSystemInit
	{

		[SerializeField]
		private GasMixesSO defaultRoomGasMixOverride = null;

		[SerializeField]
		private bool overriderTileSpawnWithNoAir;

		// Start is called before the first frame update
		public override void Start()
		{
			base.Start();
			var AS = matrixMove.GetComponent<AtmosSystem>();
			AS.defaultRoomGasMixOverride = defaultRoomGasMixOverride;
			AS.overriderTileSpawnWithNoAir = overriderTileSpawnWithNoAir;

		}

	}
}
