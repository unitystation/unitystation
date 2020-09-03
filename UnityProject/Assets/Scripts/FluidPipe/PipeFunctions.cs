using System;
using System.Collections.Generic;
using System.Linq;
using Atmospherics;
using Chemistry;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;


namespace Pipes
{
	public static class PipeFunctions
	{
		//Even how Funny it may be to have Oddly rotated tiles It would be a pain
		public static int GetOffsetAngle(float z)
		{
			if (z > 255)
			{
				return (270);
			}
			else if (z > 135)
			{
				return (180);
			}
			else if (z > 45)
			{
				return (90);
			}
			else if (z > -45)
			{
				return (0);
			}
			else if (z > -135)
			{
				return (90);
			}
			else if (z > -255)
			{
				return (180);
			}
			else if (z > -345)
			{
				return (270);
			}
			else if (z < -345)
			{
				return (0);
			}

			return (0);
		}


		public static List<PipeData> GetConnectedPipes(List<PipeData> ToPutInto, PipeData pipeData, Vector3Int Location,
			Matrix LocatedOn)
		{
			for (var i = 0; i < pipeData.Connections.Directions.Length; i++)
			{
				if (pipeData.Connections.Directions[i].Bool)
				{
					Vector3Int SearchVector = Vector3Int.zero;
					switch (i)
					{
						case (int) PipeDirection.North:
							SearchVector = Vector3Int.up;
							break;

						case (int) PipeDirection.East:
							SearchVector = Vector3Int.right;
							break;

						case (int) PipeDirection.South:
							SearchVector = Vector3Int.down;
							break;

						case (int) PipeDirection.West:
							SearchVector = Vector3Int.left;
							break;
					}

					SearchVector = Location + SearchVector;
					SearchVector.z = 0;
					var PipesOnTile = LocatedOn.GetPipeConnections(SearchVector);
					foreach (var pipe in PipesOnTile)
					{
						if (ArePipeCompatible(pipeData, i, pipe, out var pipe1ConnectAndType))
						{
							pipe1ConnectAndType.Connected = pipe;
							ToPutInto.Add(pipe);
						}
					}
				}
			}

			return (ToPutInto);
		}

		public static bool ArePipeCompatible(PipeData pipe1, int Direction, PipeData pipe2,
			out ConnectAndType ConnectAndType)
		{
			if (pipe1.PipeLayer == pipe2.PipeLayer)
			{
				int pipe2Direction = Direction + 2;
				if (pipe2Direction > 3)
				{
					pipe2Direction -= 4;
				}

				if (pipe2.Connections.Directions[pipe2Direction].Bool)
				{
					if (pipe2.Connections.Directions[pipe2Direction].pipeType
						.HasFlag(pipe1.Connections.Directions[Direction].pipeType))
					{
						ConnectAndType = pipe1.Connections.Directions[Direction];
						return true;
					}
				}
			}

			ConnectAndType = null;
			return false;
		}

		public static bool IsPipeOutputTo(PipeData pipe1, PipeData pipe2)
		{
			var Data = pipe1.Connections.Directions[(int) PipesToDirections(pipe1, pipe2)];
			return Data.Bool && Data.PortType.HasFlag(OutputType.Output_Allowed);
		}

		public static bool CanEqualiseWith(PipeData pipe1, PipeData pipe2)
		{
			var PipeDirectio = PipesToDirections(pipe1, pipe2);
			int pipe2Direction = (int) PipeDirectio + 2;
			if (pipe2Direction > 3)
			{
				pipe2Direction -= 4;
			}

			return pipe2.Connections.Directions[pipe2Direction].PortType.HasFlag(OutputType.Can_Equalise_With);
		}


		public static PipeDirection PipesToDirections(PipeData pipe1, PipeData pipe2)
		{
			var VectorDifference = pipe2.MatrixPos - pipe1.MatrixPos;
			if (VectorDifference == Vector3Int.up)
			{
				return PipeDirection.North;
			}
			else if (VectorDifference == Vector3Int.right)
			{
				return PipeDirection.East;
			}
			else if (VectorDifference == Vector3Int.down)
			{
				return PipeDirection.South;
			}
			else if (VectorDifference == Vector3Int.left)
			{
				return PipeDirection.West;
			}

			return PipeDirection.North;
		}

		public static MixAndVolume PipeOrNet(PipeData pipe)
		{
			if (pipe.NetCompatible)
			{
				return (pipe.OnNet.mixAndVolume);
			}
			else
			{
				return (pipe.mixAndVolume);
			}
		}
	}

	[System.Serializable]
	public class Connections
	{
		public ConnectAndType[] Directions = new ConnectAndType[4];

		public void Rotate(float InRotate)
		{
			if (InRotate == 0)
			{
			}
			else
			{
				if (InRotate > 181)
				{
					PipeOffset(1);
				}
				else if (InRotate > 91)
				{
					PipeOffset(2);
				}
				else
				{
					PipeOffset(3);
				}
			}
		}

		private void PipeOffset(int OffSet)
		{
			var NewConnectionss = new ConnectAndType[4];


			for (int i = 0; i < Directions.Length; i++)
			{
				int Location = i + OffSet;
				if (Location > 3)
				{
					Location -= 4;
				}

				NewConnectionss[Location] = Directions[i];
			}

			Directions = NewConnectionss;
		}

		public override string ToString()
		{
			string DD = "";
			for (int i = 0; i < Directions.Length; i++)
			{
				DD += ("," + Directions[i]);
			}


			return DD + "\n";
		}

		public static Connections CopyFrom(Connections InConnection)
		{
			var newone = new Connections();
			for (int i = 0; i < InConnection.Directions.Length; i++)
			{
				newone.Directions[i] = InConnection.Directions[i].Copy();
			}

			return (newone);
		}

		public Connections Copy()
		{
			return (CopyFrom(this));
		}

		public ConnectAndType GetFlagToDirection(FlagLogic Flages)
		{
			foreach (var Direction in Directions)
			{
				if (Direction.flagLogic == Flages)
				{
					return Direction;
				}
			}

			return null;
		}
	}

	[System.Serializable]
	public class ConnectAndType
	{
		public bool Bool;

		[EnumFlags] public PipeType pipeType = PipeType.PipeRun;

		//This is ignored if its net compatible, Probably Should but I dont got time
		[EnumFlags] [FormerlySerializedAs("OutputType")]
		public OutputType PortType = OutputType.None;

		[System.NonSerialized] public PipeData Connected = null;


		public FlagLogic flagLogic = FlagLogic.None;


		public ConnectAndType Copy()
		{
			var Newone = new ConnectAndType();
			Newone.Bool = Bool;
			Newone.pipeType = pipeType;
			Newone.PortType = PortType;
			return (Newone);
		}
	}

	[System.Serializable]
	public class MixAndVolume
	{
		[SerializeField] private float Volume => gasMix.Volume;
		[SerializeField] private ReagentMix Mix = new ReagentMix();
		[SerializeField] private GasMix gasMix = new GasMix(GasMixes.Empty);


		public float InternalEnergy
		{
			get { return Mix.InternalEnergy + gasMix.InternalEnergy; }
			set
			{
				if (WholeHeatCapacity == 0)
				{
					return;
				}

				var Temperature = (value / WholeHeatCapacity);
				Mix.Temperature = Temperature;
				gasMix.Temperature = Temperature;
			}
		}


		public float TheVolume => Volume;

		public float Temperature
		{
			get
			{
				if (WholeHeatCapacity == 0)
				{
					return 0;
				}
				else
				{
					return Mathf.Clamp(InternalEnergy / WholeHeatCapacity, 0, Single.MaxValue);
				}
			}
			set
			{
				var InternalEnergy = (value * WholeHeatCapacity);
				Mix.InternalEnergy = InternalEnergy;
				gasMix.InternalEnergy = InternalEnergy;
			}
		}

		public float WholeHeatCapacity
		{
			get { return Mix.WholeHeatCapacity + gasMix.WholeHeatCapacity; }
		}

		public Vector2 Total
		{
			get { return new Vector2(Mix.Total, gasMix.Moles); }
		}

		public Vector2 Density()
		{
			return new Vector2(Mix.Total / Volume, gasMix.Pressure);
		}

		/// <summary>
		/// Only use this if you know what you're doing
		/// </summary>
		/// <returns></returns>
		public GasMix GetGasMix()
		{
			return gasMix;
		}


		/// <summary>
		/// Only use this if you know what you're doing
		/// </summary>
		/// <returns></returns>
		public void SetGasMix(GasMix Newgas)
		{
			gasMix = Newgas;
		}

		/// <summary>
		/// Only use this if you know what you're doing
		/// </summary>
		/// <returns></returns>
		public ReagentMix GetReagentMix()
		{
			return Mix;
		}

		public void Add(MixAndVolume mixAndVolume, bool ChangeVolume = true)
		{
			float internalEnergy = mixAndVolume.InternalEnergy + this.InternalEnergy;
			float GasVolume = gasMix.Volume;
			if (ChangeVolume)
			{
				GasVolume = GasVolume + mixAndVolume.gasMix.Volume;
			}

			Mix.Add(mixAndVolume.Mix);
			var Newone = new float[gasMix.Gases.Length];
			for (int i = 0; i < gasMix.Gases.Length; i++)
			{
				Newone[i] = gasMix.Gases[i] + mixAndVolume.gasMix.Gases[i];
			}

			gasMix = GasMix.FromTemperature(Newone, gasMix.Temperature, GasVolume);
			this.InternalEnergy = internalEnergy;
		}

		public Tuple<ReagentMix, GasMix> Take(MixAndVolume InmixAndVolume, bool removeVolume = true)
		{
			if (Volume == 0)
			{
				Logger.LogError(" divide by 0 in Take ");
			}

			float Percentage = InmixAndVolume.Volume / Volume;
			float RemoveGasVolume = gasMix.Volume;
			float GasVolume = gasMix.Volume;
			if (removeVolume)
			{
				RemoveGasVolume = RemoveGasVolume * Percentage;
				GasVolume = GasVolume * (1 - Percentage);
			}

			var ReturnMix = Mix.Take(Mix.Total * Percentage);

			var Newone = new float[gasMix.Gases.Length];
			var RemoveNewone = new float[gasMix.Gases.Length];
			for (int i = 0; i < gasMix.Gases.Length; i++)
			{
				RemoveNewone[i] = gasMix.Gases[i] * Percentage;
				Newone[i] = gasMix.Gases[i] * (1 - Percentage);
			}

			gasMix = GasMix.FromTemperature(Newone, gasMix.Temperature, GasVolume);

			return new Tuple<ReagentMix, GasMix>(ReturnMix,
				GasMix.FromTemperature(RemoveNewone, gasMix.Temperature, RemoveGasVolume));
		}

		public void Remove(Vector2 ToRemove)
		{
			Mix.RemoveVolume(ToRemove.x);
			gasMix.RemoveMoles(ToRemove.y);
		}

		public void Add(GasMix ToAdd)
		{
			gasMix = gasMix + ToAdd;
		}

		public void Divide(float DivideAmount, bool ChangeVolume = true)
		{
			if (DivideAmount == 0)
			{
				Logger.LogError(" divide by 0 in Divide");
			}

			float GasVolume = gasMix.Volume;
			if (ChangeVolume)
			{
				GasVolume = GasVolume / DivideAmount;
			}

			Mix.Divide(DivideAmount);

			var Newone = new float[gasMix.Gases.Length];
			for (int i = 0; i < gasMix.Gases.Length; i++)
			{
				Newone[i] = gasMix.Gases[i] / DivideAmount;
			}

			gasMix = GasMix.FromTemperature(Newone, gasMix.Temperature, GasVolume);
		}

		public void Multiply(float MultiplyAmount, bool ChangeVolume = true)
		{
			float GasVolume = gasMix.Volume;
			if (ChangeVolume)
			{
				GasVolume = GasVolume * MultiplyAmount;
			}

			Mix.Multiply(MultiplyAmount);

			var Newone = new float[gasMix.Gases.Length];
			for (int i = 0; i < gasMix.Gases.Length; i++)
			{
				Newone[i] = gasMix.Gases[i] * MultiplyAmount;
			}

			gasMix = GasMix.FromTemperature(Newone, gasMix.Temperature, GasVolume);
		}

		public MixAndVolume Clone()
		{
			var MiXV = new MixAndVolume();
			MiXV.gasMix = new GasMix(gasMix);
			MiXV.Mix = Mix.Clone();
			return (MiXV);
		}

		public void Empty()
		{
			gasMix = gasMix * 0;
			Mix.Multiply(0);
		}

		public void TransferSpecifiedTo(MixAndVolume toTransfer, Gas? SpecifiedGas = null,
			Chemistry.Reagent Reagent = null, Vector2? amount = null)
		{
			if (SpecifiedGas != null)
			{
				float ToRemovegas = 0;
				var Gas = SpecifiedGas.GetValueOrDefault(Atmospherics.Gas.Oxygen);
				if (amount != null)
				{
					ToRemovegas = amount.Value.y;
				}
				else
				{
					ToRemovegas = gasMix.Gases[Gas];
				}

				float TransferredEnergy = ToRemovegas * Gas.MolarHeatCapacity * gasMix.Temperature;

				gasMix.RemoveGas(Gas, ToRemovegas);

				float CachedInternalEnergy = toTransfer.InternalEnergy + TransferredEnergy; //- TransferredEnergy;

				toTransfer.gasMix.AddGas(Gas, ToRemovegas);
				toTransfer.gasMix.InternalEnergy = CachedInternalEnergy;
			}

			if (Reagent != null)
			{
				float ToRemovegas = 0;

				if (amount != null)
				{
					ToRemovegas = amount.Value.x;
				}
				else
				{
					ToRemovegas = Mix[Reagent];
				}


				float TransferredEnergy = ToRemovegas * Reagent.heatDensity * Mix.Temperature;
				float CachedInternalEnergy = Mix.InternalEnergy;
				Mix.Subtract(Reagent, ToRemovegas);
				Mix.InternalEnergy = CachedInternalEnergy - TransferredEnergy;

				CachedInternalEnergy = toTransfer.Mix.InternalEnergy;
				toTransfer.Mix.Add(Reagent, ToRemovegas);
				CachedInternalEnergy += TransferredEnergy;
				toTransfer.Mix.InternalEnergy = CachedInternalEnergy;
			}
		}


		public void TransferTo(MixAndVolume toTransfer, Vector2 amount)
		{
			if (float.IsNaN(amount.x) == false)
			{
				Mix.TransferTo(toTransfer.Mix, amount.x);
			}

			if (float.IsNaN(amount.y) == false)
			{
				toTransfer.gasMix = toTransfer.gasMix + gasMix.RemoveMoles(amount.y);
			}
		}

		public GasMix EqualiseWithExternal(GasMix inGasMix)
		{
			return gasMix.MergeGasMix(inGasMix);
		}


		public void EqualiseWith(PipeData Another, bool EqualiseGas, bool EqualiseLiquid)
		{
			if (EqualiseGas)
			{
				PipeFunctions.PipeOrNet(Another).gasMix = gasMix.MergeGasMix(PipeFunctions.PipeOrNet(Another).gasMix);
			}

			if (EqualiseLiquid)
			{
				float TotalVolume = Volume + PipeFunctions.PipeOrNet(Another).Volume;
				float TotalReagents = Mix.Total + PipeFunctions.PipeOrNet(Another).Mix.Total;
				if (TotalVolume == 0)
				{
					Logger.LogError(" divide by 0 in EqualiseWith TotalVolume ");
				}

				float TargetDensity = TotalReagents / TotalVolume;

				float thisAmount = TargetDensity * Volume;
				float AnotherAmount = TargetDensity * PipeFunctions.PipeOrNet(Another).Volume;

				if (thisAmount > Mix.Total)
				{
					PipeFunctions.PipeOrNet(Another).Mix
						.TransferTo(Mix, PipeFunctions.PipeOrNet(Another).Mix.Total - AnotherAmount);
				}
				else
				{
					this.Mix.TransferTo(PipeFunctions.PipeOrNet(Another).Mix,
						AnotherAmount - PipeFunctions.PipeOrNet(Another).Mix.Total);
				}
			}
		}

		public void EqualiseWithMultiple(List<PipeData> others, bool EqualiseGas, bool EqualiseLiquid)
		{
			if (EqualiseGas)
			{
				gasMix.MergeGasMixes(others);
			}

			if (EqualiseLiquid)
			{
				float TotalVolume = Volume;
				foreach (var Pipe in others)
				{
					TotalVolume += PipeFunctions.PipeOrNet(Pipe).Volume;
				}

				float TotalReagents = Mix.Total;
				foreach (var Pipe in others)
				{
					TotalReagents += PipeFunctions.PipeOrNet(Pipe).Mix.Total;
				}

				if (TotalVolume == 0)
				{
					Logger.LogError(" divide by 0 in EqualiseWithMultiple TotalVolume ");
				}

				float TargetDensity = TotalReagents / TotalVolume;


				foreach (var Pipe in others)
				{
					PipeFunctions.PipeOrNet(Pipe).Mix.TransferTo(Mix, PipeFunctions.PipeOrNet(Pipe).Mix.Total);
				}

				foreach (var Pipe in others)
				{
					Mix.TransferTo(PipeFunctions.PipeOrNet(Pipe).Mix,
						TargetDensity * PipeFunctions.PipeOrNet(Pipe).Volume);
				}
			}
		}

		public void EqualiseWithOutputs(List<PipeData> others)
		{
			bool DifferenceInPressure = false;
			bool DifferenceInDensity = false;
			Vector2 density = this.Density();
			foreach (var Pipe in others)
			{
				var DensityDelta = (PipeFunctions.PipeOrNet(Pipe).Density() - density);
				if (Mathf.Abs(DensityDelta.x) > 0.001f)
				{
					DifferenceInDensity = true;
					if (DifferenceInPressure)
					{
						break;
					}
				}

				if (Mathf.Abs(DensityDelta.y) > 0.001f)
				{
					DifferenceInPressure = true;
					if (DifferenceInDensity)
					{
						break;
					}
				}
			}

			if (DifferenceInPressure || DifferenceInDensity)
			{
				if (others.Count > 1)
				{
					EqualiseWithMultiple(others, DifferenceInPressure, DifferenceInDensity);
				}
				else if (others.Count > 0)
				{
					EqualiseWith(others[0], DifferenceInPressure, DifferenceInDensity);
				}
			}
		}

		public void SetVolume(float NewVolume)
		{
			gasMix.Volume = NewVolume;
		}

		public override string ToString()
		{
			return "Volume > " + Volume + " Mix > " + Mix.ToString() + " gasmix > " + gasMix.ToString();
		}
	}

	public enum PipeDirection
	{
		North = 0,
		East = 1,
		South = 2,
		West = 3,
	}

	[Flags]
	public enum PipeType
	{
		None = 0,
		PipeRun = 1 << 0,
		CoolingPipe = 1 << 1,
		//Used for stopping cooling pipes to connect to usual pipes
	}


	public enum PipeLayer
	{
		First = 0,
		Second = 1,
		Third = 2
	}

	[Flags]
	//Used to keeping track of output connections
	public enum OutputType
	{
		None = 0,
		Output_Allowed = 1 << 0,
		Input_Allowed = 1 << 1,
		Something_Else = 1 << 2,
		Can_Equalise_With = Input_Allowed | Output_Allowed
	}

	public enum FlagLogic
	{
		None,
		UnfilteredOutput,
		FilteredOutput,
		InputOne,
		InputTwo
	}


	public enum CustomLogic
	{
		None,
		CoolingPipe,
	}
}