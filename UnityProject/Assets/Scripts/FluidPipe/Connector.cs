using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;
using Objects.GasContainer;

namespace Pipes
{
	public class Connector : MonoPipe
	{
		private Canister canister;

		public override void Start()
		{
			pipeData.PipeAction = new MonoActions();
			base.Start();
		}

		public override void TickUpdate()
		{
			// TODO: the connector now considers if the valve is open, but it should also take into
			// account the pressure release setting to set a limit on the max. transfer per tick, at some point.
			// Perhaps this behaviour should be appropriated by the canister itself?

			base.TickUpdate();
			if (canister != null && canister.ValveIsOpen)
			{
				canister.GasContainer.GasMix = pipeData.mixAndVolume.EqualiseWithExternal(canister.GasContainer.GasMix);
			}
			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);
		}

		public void DisconnectCanister()
		{
			canister = null;
		}

		public void ConnectCanister(Canister Incanister)
		{
			canister = Incanister;
		}

		public override void OnDisassembly(HandApply interaction)
		{
			if (canister != null)
			{
				canister.DisconnectFromConnector();
			}
		}
	}
}
