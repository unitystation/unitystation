using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;

namespace Pipes
{
	public class Filter : MonoPipe
	{
		public SpriteHandler spriteHandlerOverlay = null;

		private MixAndVolume IntermediateMixAndVolume = new MixAndVolume();

		public int ToMaxPressure = 9999;
		private float TransferMoles = 500f;

		public bool IsOn = false;

		public Gas GasIndex = Gas.Oxygen;
		public Chemistry.Reagent FilterReagent;
		public override void Start()
		{
			pipeData.PipeAction = new MonoActions();
			spriteHandlerOverlay.PushClear();
			base.Start();
		}

		public void TogglePower()
		{
			IsOn = !IsOn;
			if (IsOn)
			{
				spriteHandlerOverlay.PushTexture();
			}
			else
			{
				spriteHandlerOverlay.PushClear();
			}
		}

		public override void Interaction(HandApply interaction)
		{
			TabUpdateMessage.Send( interaction.Performer, gameObject, NetTabType.Filter, TabAction.Open );
		}

		public override void TickUpdate()
		{


			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);

			if (IsOn == false)
			{
				return;
			}

			if (pipeData.mixAndVolume.Density().x == 0 && pipeData.mixAndVolume.Density().y == 0)
			{
				return;
			}

			foreach (var Connection in pipeData.Connections.Directions)
			{
				if (Connection.flagLogic == FlagLogic.UnfilteredOutput)
				{
					if (Connection.Connected == null)
					{
						return;
					}

					var PressureDensity = Connection.Connected.GetMixAndVolume.Density();


					var tomove = new Vector2(Mathf.Abs((PressureDensity.x / ToMaxPressure) - 1),
						Mathf.Abs((PressureDensity.y / ToMaxPressure) - 1));

					Vector2 AvailableReagents = new Vector2(0f, 0f);
					AvailableReagents+= pipeData.mixAndVolume.Total;


					Vector2 TotalRemove = Vector2.zero;
					TotalRemove.x = (TransferMoles) > AvailableReagents.x ? AvailableReagents.x : TransferMoles;
					TotalRemove.y = (TransferMoles) > AvailableReagents.y ? AvailableReagents.y : TransferMoles;

					TotalRemove.x = tomove.x > 1 ? 0 : TotalRemove.x;
					TotalRemove.y = tomove.y > 1 ? 0 : TotalRemove.y;
					pipeData.mixAndVolume.TransferTo(IntermediateMixAndVolume, TotalRemove);
					foreach (var GetConnection in pipeData.Connections.Directions)
					{
						if (GetConnection.flagLogic == FlagLogic.FilteredOutput)
						{
							if (GetConnection.Connected == null)
							{
								IntermediateMixAndVolume.TransferTo(pipeData.mixAndVolume, IntermediateMixAndVolume.Total);
								return;
							}
							var FilteredPressureDensity = GetConnection.Connected.GetMixAndVolume.Density();

							if (FilteredPressureDensity.x > ToMaxPressure ||  FilteredPressureDensity.y > ToMaxPressure)
							{
								IntermediateMixAndVolume.TransferSpecifiedTo(pipeData.mixAndVolume,
									GasIndex, FilterReagent);
								if (PressureDensity.x > ToMaxPressure && PressureDensity.y > ToMaxPressure)
								{
									IntermediateMixAndVolume.TransferTo(pipeData.mixAndVolume, IntermediateMixAndVolume.Total);
								}
								else
								{
									IntermediateMixAndVolume.TransferTo(Connection.Connected.GetMixAndVolume, IntermediateMixAndVolume.Total);
								}

								return;
							}

							if (PressureDensity.x > ToMaxPressure && PressureDensity.y > ToMaxPressure)
							{
								IntermediateMixAndVolume.TransferSpecifiedTo(GetConnection.Connected.GetMixAndVolume,
									GasIndex, FilterReagent);
								IntermediateMixAndVolume.TransferTo(pipeData.mixAndVolume, IntermediateMixAndVolume.Total);
								return;
							}
							IntermediateMixAndVolume.TransferSpecifiedTo(GetConnection.Connected.GetMixAndVolume,
								GasIndex, FilterReagent);

							IntermediateMixAndVolume.TransferTo(Connection.Connected.GetMixAndVolume, IntermediateMixAndVolume.Total);
						}
					}
				}
			}
		}

		public bool CanEqualiseWithThis(PipeData Pipe)
		{
			if (Pipe.NetCompatible == false)
			{
				return PipeFunctions.CanEqualiseWith(this.pipeData, Pipe);
			}

			return true;
		}
	}
}

