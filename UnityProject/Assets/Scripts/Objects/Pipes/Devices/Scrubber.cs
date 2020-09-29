using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;

namespace Pipes
{
	public class Scrubber : MonoPipe
	{
		public bool SelfSufficient = false;
		// minimum pressure needs to be a little lower because of floating point inaccuracies
		public float MMinimumPressure = 90.00f;

		public float MaxInternalPressure = 10000f;

		public float MaxTransferMoles = 100;

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
				var PressureDensity = pipeData.mixAndVolume.Density();
				if (PressureDensity.y > MaxInternalPressure || metaNode.GasMix.Pressure < MMinimumPressure )
				{
					return;
				}
			}
			else
			{
				if (metaNode.GasMix.Pressure < MMinimumPressure)
				{
					return;
				}
			}

			float Available = 0;
			if (metaNode.GasMix.Pressure != 0)
			{
				Available =	((MMinimumPressure / metaNode.GasMix.Pressure) * metaNode.GasMix.Moles);
			}
			else
			{
				return;
			}

			if (Available < 0)
			{
				return;
			}

			if (MaxTransferMoles < Available)
			{
				Available = MaxTransferMoles;
			}

			var Gasonnnode = metaNode.GasMix;
			var TransferringGas = Gasonnnode.RemoveMoles(Available);
			metaNode.GasMix = Gasonnnode;
			if (SelfSufficient == false)
			{
				pipeData.mixAndVolume.Add(TransferringGas);
			}

			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer, SystemType.AtmosSystem);
		}
	}
}
