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

		private GasMix selfSufficientGas;
		public override void OnSpawnServer(SpawnInfo info)
		{
			metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
			metaNode = metaDataLayer.Get(registerTile.LocalPositionServer, false);
			if (SelfSufficient)
			{
				selfSufficientGas = GasMix.NewGasMix(GasMixes.Air);
			}
			base.OnSpawnServer(info);
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

			if (SelfSufficient)
			{
				TransferGas(selfSufficientGas, molesTransferred);
				selfSufficientGas.Copy(GasMixes.Air);
			}
			else
			{
				var pipeMix = pipeData.mixAndVolume.GetGasMix();
				TransferGas(pipeMix, molesTransferred);
			}

			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer, SystemType.AtmosSystem);
		}

		private void TransferGas(GasMix pipeMix, float molesTransferred)
		{
			if (molesTransferred > pipeMix.Moles)
			{
				molesTransferred = pipeMix.Moles;
			}
			GasMix.TransferGas(metaNode.GasMix, pipeMix, molesTransferred);
		}
	}
}
