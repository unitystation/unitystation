using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

namespace Pipes
{
	public class Scrubber : MonoPipe
	{
		// minimum pressure needs to be a little lower because of floating point inaccuracies
		public float MMinimumPressure = 80.00f;

		public float MaxInternalPressure = 10000f;

		public float MaxTransferMoles = 100;


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

			var PressureDensity = pipeData.mixAndVolume.Density();
			if (PressureDensity.y > MaxInternalPressure || metaNode.GasMix.Pressure < MMinimumPressure )
			{
				return;
			}

			float Available = metaNode.GasMix.Moles;


			if (MaxTransferMoles < Available)
			{
				Available = MaxTransferMoles;
			}

			var Gasonnnode = metaNode.GasMix;
			var TransferringGas = Gasonnnode.RemoveMoles(Available);
			metaNode.GasMix = Gasonnnode;
			pipeData.mixAndVolume.Add(TransferringGas);
			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer);
		}
	}
}