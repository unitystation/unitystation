using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class AirVent : AdvancedPipe
{
	public float MinimumPressure = 101.325f;
	private MetaDataNode metaNode;
	private MetaDataLayer metaDataLayer;

	public override bool Attach()
	{
		if (base.Attach() == false)
		{
			return false;
		}
		LoadTurf();
		return true;
	}

	private void LoadTurf()
	{
		metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
		metaNode = metaDataLayer.Get(registerTile.WorldPositionServer, false);
	}

	public override void UpdateMe()
	{
		if (anchored)
		{
			CheckAtmos();
		}
	}

	private void CheckAtmos()
	{
		if (metaNode.GasMix.Pressure < MinimumPressure)
		{
			GasMix gasMix = pipenet.gasMix;
			pipenet.gasMix = gasMix / 2;
			metaNode.GasMix = metaNode.GasMix + gasMix;
			metaDataLayer.UpdateSystemsAt(registerTile.WorldPositionServer);
		}
	}
}
