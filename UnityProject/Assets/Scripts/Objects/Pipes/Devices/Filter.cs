using System;
using System.Collections.Generic;
using UnityEngine;
using Messages.Server;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;
using Systems.Interaction;
using Systems.Pipes;


namespace Objects.Atmospherics
{
	public class Filter : MonoPipe
	{
		public SpriteHandler spriteHandlerOverlay = null;

		private MixAndVolume IntermediateMixAndVolume = new MixAndVolume();

		public static Dictionary<string, GasSO> CapableFiltering;

		//This is only used to set the inital filter values, nothing else
		//the names contained within should always match the key of the above Dictionary
		private enum FilterValues
		{
			O2,
			N2,
			PLS,
			CO2,
			N2O,
			H2,
			H2O,
			BZ,
			MIAS,
			NO2,
			TRIT,
			HN,
			STIM,
			PLX,
			FRE,
			ASH,
			SMKE
		}

		[SerializeField]
		private FilterValues initalFilterValue = default;

		public int MaxPressure = 9999;
		private float TransferMoles = 5000f;

		public bool IsOn = false;

		[NonSerialized]
		public GasSO GasIndex;
		public Chemistry.Reagent FilterReagent;

		public override void Awake()
		{
			base.Awake();

			//Only needs to be set once by one instance
			if(CapableFiltering != null) return;

			CapableFiltering = new Dictionary<string, GasSO>()
			{
				{"O2",Gas.Oxygen},
				{"N2",Gas.Nitrogen},
				{"PLS",Gas.Plasma},
				{"CO2",Gas.CarbonDioxide},
				{"N2O",Gas.NitrousOxide},
				{"H2",Gas.Hydrogen},
				{"H2O",Gas.WaterVapor},
				{"BZ",Gas.BZ},
				{"MIAS",Gas.Miasma},
				{"NO2",Gas.Nitryl},
				{"TRIT",Gas.Tritium},
				{"HN",Gas.HyperNoblium},
				{"STIM",Gas.Stimulum},
				{"PLX",Gas.Pluoxium},
				{"FRE",Gas.Freon},
				{"ASH",Gas.Ash},
				{"SMKE",Gas.Smoke}
			};
		}

		public override void OnSpawnServer(SpawnInfo info)
		{
			GasIndex = CapableFiltering[initalFilterValue.ToString()];

			if (IsOn)
			{
				spriteHandlerOverlay.PushTexture();
			}
			else
			{
				spriteHandlerOverlay.PushClear();
			}
			base.OnSpawnServer(info);
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

		public override void HandApplyInteraction(HandApply interaction)
		{
			TabUpdateMessage.Send( interaction.Performer, gameObject, NetTabType.Filter, TabAction.Open );
		}

		//Ai interaction
		public override void AiInteraction(AiActivate interaction)
		{
			TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.Filter, TabAction.Open);
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

			foreach (var UnfilteredConnection in pipeData.Connections.Directions)
			{
				if (UnfilteredConnection.flagLogic == FlagLogic.UnfilteredOutput)
				{
					if (UnfilteredConnection.Connected == null)
					{
						return;
					}

					var PressureDensity = UnfilteredConnection.Connected.GetMixAndVolume.Density();


					var tomove = new Vector2(Mathf.Abs((PressureDensity.x / MaxPressure) - 1),
						Mathf.Abs((PressureDensity.y / MaxPressure) - 1));

					Vector2 AvailableReagents = new Vector2(0f, 0f);
					AvailableReagents+= pipeData.mixAndVolume.Total;


					Vector2 TotalRemove = Vector2.zero;
					TotalRemove.x = (TransferMoles) > AvailableReagents.x ? AvailableReagents.x : TransferMoles;
					TotalRemove.y = (TransferMoles) > AvailableReagents.y ? AvailableReagents.y : TransferMoles;

					TotalRemove.x = tomove.x > 1 ? 0 : TotalRemove.x;
					TotalRemove.y = tomove.y > 1 ? 0 : TotalRemove.y;
					pipeData.mixAndVolume.TransferTo(IntermediateMixAndVolume, TotalRemove);
					foreach (var FilteredConnection in pipeData.Connections.Directions)
					{
						if (FilteredConnection.flagLogic == FlagLogic.FilteredOutput)
						{
							if (FilteredConnection.Connected == null)
							{
								IntermediateMixAndVolume.TransferTo(pipeData.mixAndVolume, IntermediateMixAndVolume.Total);
								return;
							}
							var FilteredPressureDensity = FilteredConnection.Connected.GetMixAndVolume.Density();

							if (FilteredPressureDensity.x > MaxPressure ||  FilteredPressureDensity.y > MaxPressure)
							{
								IntermediateMixAndVolume.TransferSpecifiedTo(pipeData.mixAndVolume,
									GasIndex, FilterReagent); //Return FilterReagent Back to the internal pipe from intermediate mix

								if (PressureDensity.x > MaxPressure && PressureDensity.y > MaxPressure)
								{
									IntermediateMixAndVolume.TransferTo(pipeData.mixAndVolume, IntermediateMixAndVolume.Total); //Transfer all Intermediate back into internal pipe
								}
								else
								{
									IntermediateMixAndVolume.TransferTo(UnfilteredConnection.Connected.GetMixAndVolume, IntermediateMixAndVolume.Total); //Output to the unfiltered area ( Everything except the filtered reagent )
								}

								return;
							}


							IntermediateMixAndVolume.TransferSpecifiedTo(FilteredConnection.Connected.GetMixAndVolume,
								GasIndex, FilterReagent);  //Transfer filtered gas into filtered output

							if (PressureDensity.x > MaxPressure && PressureDensity.y > MaxPressure)
							{
								IntermediateMixAndVolume.TransferTo(pipeData.mixAndVolume, IntermediateMixAndVolume.Total);  //Transfer gas into pipe itself Returning it into the pipe it came from
								return;
							}
							else
							{
								IntermediateMixAndVolume.TransferTo(UnfilteredConnection.Connected.GetMixAndVolume, IntermediateMixAndVolume.Total); //Output to the unfiltered area ( Everything except the filtered reagent )
							}
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
