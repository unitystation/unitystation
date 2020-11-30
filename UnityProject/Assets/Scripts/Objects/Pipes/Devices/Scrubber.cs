using System.Collections;
using System.Collections.Generic;
using Systems.Atmospherics;
using UnityEngine;

namespace Pipes
{
	public class Scrubber : MonoPipe
	{
		public bool SelfSufficient = false;
		// minimum pressure needs to be a little lower because of floating point inaccuracies
		public float MMinimumPressure = 90.00f;

		public float MaxInternalPressure = 10000f;

		public float MaxTransferMoles = 10;

		private MetaDataNode metaNode;
		private MetaDataLayer metaDataLayer;


		public override void Start()
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
			if (SelfSufficient == false)
			{
				var pressureDensity = pipeData.mixAndVolume.Density();
				if (pressureDensity.y > MaxInternalPressure || metaNode.gasMix.Pressure < MMinimumPressure )
				{
					return;
				}
			}
			else
			{
				if (metaNode.gasMix.Pressure < MMinimumPressure)
				{
					return;
				}
			}

			if (metaNode.gasMix.Pressure == 0)
				return;

			float available = MMinimumPressure / metaNode.gasMix.Pressure * metaNode.gasMix.Moles;

			if (available < 0)
				return;

			if (MaxTransferMoles < available)
			{
				available = MaxTransferMoles;
			}

			var gasOnNode = metaNode.gasMix;
			GasMix pipeMix;

			if (SelfSufficient)
			{
				pipeMix = GasMix.NewGasMix(GasMixes.Air); //TODO: get some immutable gasmix to avoid GC
			}
			else
			{
				pipeMix = pipeData.mixAndVolume.GetGasMix();
			}

			GasMix.TransferGas(pipeMix, gasOnNode, available);

			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer, SystemType.AtmosSystem);
		}
	}
}
