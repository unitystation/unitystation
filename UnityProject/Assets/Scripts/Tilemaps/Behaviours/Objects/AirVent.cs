using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;
using Pipes;

namespace Pipes
{
	public class AirVent : MonoPipe
	{
		// minimum pressure needs to be a little lower because of floating point inaccuracies
		public float MaxTransferMoles = 100;
		public float MaxOutletPressure = 120;

		private MetaDataNode metaNode;
		private MetaDataLayer metaDataLayer;


		private void Start()
		{
			pipeData.PipeAction = new MonoActions();
			registerTile = this.GetComponent<RegisterTile>();


			base.Start();
		}

		public override void TickUpdate()
		{
			if (metaDataLayer == null)
			{
				metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
			}

			if (metaNode == null)
			{
				metaNode = metaDataLayer.Get(registerTile.LocalPositionServer, false);
			}


			base.TickUpdate();
			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);
			CheckAtmos();
		}

		private void CheckAtmos()
		{
			//metaNode.GasMix = pipeData.mixAndVolume.EqualiseWithExternal(metaNode.GasMix);

			var PressureDensity = pipeData.mixAndVolume.Density();
			if (metaNode.GasMix.Pressure> MaxOutletPressure)
			{
				return;
			}

			float Available = pipeData.mixAndVolume.Density().y;
			if (Available == 0)
			{
				return;
			}

			if (MaxTransferMoles < Available)
			{
				Available = MaxTransferMoles;
			}

			var Gasonnnode = metaNode.GasMix;
			var pipeMix = pipeData.mixAndVolume.GetGasMix();
			var TransferringGas = pipeMix.RemoveMoles(Available);
			pipeData.mixAndVolume.SetGasMix(pipeMix);
			metaNode.GasMix = (Gasonnnode + TransferringGas);
			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer);
			//if (metaNode.GasMix.Pressure < MinimumPressure)
			//{
				//TODO: Can restore this when pipenets are implemented so they actually pull from what
				//they are connected to. In the meantime
				//we are reverting scrubbers / airvents to the old behavior of just shoving or removing air
				//regardless of what they are connected to.
				// GasMix gasMix = pipenet.gasMix;
				// pipenet.gasMix = gasMix / 2;
				// metaNode.GasMix = metaNode.GasMix + gasMix;
				//metaNode.GasMix = new GasMix(GasMixes.Air);

			//}
		}
	}
}