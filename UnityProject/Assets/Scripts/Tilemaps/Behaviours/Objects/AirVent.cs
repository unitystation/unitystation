using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;
using Pipes;

namespace Pipes
{
	public class AirVent : MonoPipe
	{
		public bool SelfSufficient = false;

		// minimum pressure needs to be a little lower because of floating point inaccuracies
		public float MaxTransferMoles = 100;
		public float MaxOutletPressure = 110;

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
			//metaNode.GasMix = pipeData.mixAndVolume.EqualiseWithExternal(metaNode.GasMix);
			if (metaNode.GasMix.Pressure > MaxOutletPressure)
			{
				return;
			}

			float Available = 0;

			if (metaNode.GasMix.Pressure != 0)
			{
				Available =	((MaxOutletPressure / metaNode.GasMix.Pressure) * metaNode.GasMix.Moles) - metaNode.GasMix.Moles;
			}
			else
			{
				Available = MaxTransferMoles;
			}




			if (MaxTransferMoles < Available)
			{
				Available = MaxTransferMoles;
			}

			if (SelfSufficient)
			{
				if (Available > GasMixes.Air.Moles)
				{
					Available = GasMixes.Air.Moles;
				}
			}
			else
			{
				if (Available > pipeData.mixAndVolume.Total.y)
				{
					Available = pipeData.mixAndVolume.Total.y;
				}
			}

			var Gasonnnode = metaNode.GasMix;
			var pipeMix = new GasMix(GasMixes.Empty);
			if (SelfSufficient)
			{
				pipeMix = new GasMix(GasMixes.Air);
			}
			else
			{
				pipeMix = pipeData.mixAndVolume.GetGasMix();
			}


			var TransferringGas = pipeMix.RemoveMoles(Available);
			if (!SelfSufficient)
			{
				pipeData.mixAndVolume.SetGasMix(pipeMix);
			}


			metaNode.GasMix = (Gasonnnode + TransferringGas);
			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer, SystemType.AtmosSystem);
		}
	}
}
