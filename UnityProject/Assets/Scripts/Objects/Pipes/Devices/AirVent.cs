using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Atmospherics;
using Pipes;

namespace Pipes
{
	public class AirVent : MonoPipe, IServerSpawn
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

		public void OnSpawnServer(SpawnInfo info)
		{
			metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
			metaNode = metaDataLayer.Get(registerTile.LocalPositionServer, false);
		}

		public override void TickUpdate()
		{
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

			float molesTransferred;

			if (metaNode.GasMix.Pressure != 0)
			{
				molesTransferred =	((MaxOutletPressure / metaNode.GasMix.Pressure) * metaNode.GasMix.Moles) - metaNode.GasMix.Moles;
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
			GasMix.TransferGas(metaNode.GasMix, pipeMix, molesTransferred);
			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer, SystemType.AtmosSystem);
		}
	}
}
