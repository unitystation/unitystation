using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

public class Scrubber : AdvancedPipe
{
	// minimum pressure needs to be a little higher because of floating point inaccuracies
	public float MinimumPressure = 101.3251f;
	private MetaDataNode metaNode;
	private MetaDataLayer metaDataLayer;

	public override bool ServerAttach()
	{
		if(base.ServerAttach() == false)
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
		if (metaNode.GasMix.Pressure > MinimumPressure)
		{
			//TODO: Can restore this when pipenets are implemented so they actually pull from what
			//they are connected to. In the meantime
			//we are reverting scrubbers / airvents to the old behavior of just shoving or removing air
			//regardless of what they are connected to.
			// var suckedAir =  metaNode.GasMix / 2;
			// pipenet.gasMix += suckedAir;
			// metaNode.GasMix -= suckedAir;

			//suck out a fraction of the current gasmix
			var suckedAir = metaNode.GasMix / 4;
			metaNode.GasMix -= suckedAir;
			metaDataLayer.UpdateSystemsAt(RegisterTile.LocalPositionServer);
		}
	}
}
