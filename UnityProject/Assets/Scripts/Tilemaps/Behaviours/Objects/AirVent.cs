using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class AirVent : AdvancedPipe
{
	// minimum pressure needs to be a little lower because of floating point inaccuracies
	public float MinimumPressure = 101.3249f;
	private MetaDataNode metaNode;
	private MetaDataLayer metaDataLayer;

	public override bool ServerAttach()
	{
		if (base.ServerAttach() == false)
		{
			return false;
		}

		LoadTurf();
		return true;
	}

	private void LoadTurf()
	{
		metaDataLayer = MatrixManager.AtPoint(RegisterTile.WorldPositionServer, true).MetaDataLayer;
		metaNode = metaDataLayer.Get(RegisterTile.LocalPositionServer, false);
	}

	public override void TickUpdate()
	{
		base.TickUpdate();
		if (anchored)
		{
			CheckAtmos();
		}
	}

	private void CheckAtmos()
	{
		if (metaNode.GasMix.Pressure < MinimumPressure)
		{
			//TODO: Can restore this when pipenets are implemented so they actually pull from what
			//they are connected to. In the meantime
			//we are reverting scrubbers / airvents to the old behavior of just shoving or removing air
			//regardless of what they are connected to.
			// GasMix gasMix = pipenet.gasMix;
			// pipenet.gasMix = gasMix / 2;
			// metaNode.GasMix = metaNode.GasMix + gasMix;
			metaNode.GasMix = new GasMix(GasMixes.Air);
			metaDataLayer.UpdateSystemsAt(RegisterTile.LocalPositionServer);
		}
	}
}