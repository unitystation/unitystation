using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Atmospherics;
using Pipes;

namespace Pipes
{
	public class AirVent : MonoPipe
	{
		public bool SelfSufficient = false;

		// minimum pressure needs to be a little lower because of floating point inaccuracies
		public float MaxTransferMoles = 10;
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
			if (metaNode.gasMix.Pressure > MaxOutletPressure)
			{
				return;
			}

			float molesTransferred;

			if (metaNode.gasMix.Pressure != 0)
			{
				molesTransferred =	((MaxOutletPressure / metaNode.gasMix.Pressure) * metaNode.gasMix.Moles) - metaNode.gasMix.Moles;
				if (MaxTransferMoles < molesTransferred)
				{
					molesTransferred = MaxTransferMoles;
				}
			}
			else
			{
				molesTransferred = MaxTransferMoles;
			}

			GasMix pipeMix;
			if (SelfSufficient)
			{
				pipeMix = GasMix.NewGasMix(GasMixes.Air); //TODO: get some immutable gasmix to avoid GC
				if (molesTransferred > GasMixes.Air.Moles)
				{
					molesTransferred = GasMixes.Air.Moles;
				}

			}
			else
			{
				pipeMix = pipeData.mixAndVolume.GetGasMix();
				if (molesTransferred > pipeMix.Moles)
				{
					molesTransferred = pipeMix.Moles;
				}
			}
			GasMix.TransferGas(metaNode.gasMix, pipeMix, molesTransferred);
			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer, SystemType.AtmosSystem);
		}
	}
}
