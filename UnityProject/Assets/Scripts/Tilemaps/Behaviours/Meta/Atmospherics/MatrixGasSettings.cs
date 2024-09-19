using System.Collections;
using System.Collections.Generic;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;
using TileMap.Behaviours;
using UnityEngine;

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
        var AS = MatrixMove.GetComponent<AtmosSystem>();
        AS.defaultRoomGasMixOverride = defaultRoomGasMixOverride;
        AS.overriderTileSpawnWithNoAir = overriderTileSpawnWithNoAir;

    }

}
