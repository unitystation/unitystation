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
		}

		[SerializeField]
		private FilterValues initalFilterValue = default;

		public int MaxPressure = 9999;
		private float TransferMoles = 500f;

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

			foreach (var Connection in pipeData.Connections.Directions)
			{
				if (Connection.flagLogic == FlagLogic.UnfilteredOutput)
				{
					if (Connection.Connected == null)
					{
						return;
					}

					var PressureDensity = Connection.Connected.GetMixAndVolume.Density();


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

							if (FilteredPressureDensity.x > MaxPressure ||  FilteredPressureDensity.y > MaxPressure)
							{
								IntermediateMixAndVolume.TransferSpecifiedTo(pipeData.mixAndVolume,
									GasIndex, FilterReagent);
								if (PressureDensity.x > MaxPressure && PressureDensity.y > MaxPressure)
								{
									IntermediateMixAndVolume.TransferTo(pipeData.mixAndVolume, IntermediateMixAndVolume.Total);
								}
								else
								{
									IntermediateMixAndVolume.TransferTo(Connection.Connected.GetMixAndVolume, IntermediateMixAndVolume.Total);
								}

								return;
							}

							if (PressureDensity.x > MaxPressure && PressureDensity.y > MaxPressure)
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
