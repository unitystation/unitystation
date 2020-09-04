using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Atmospherics;
using Chemistry;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;

namespace Pipes
{
	public class LiquidPipeNet
	{
		public bool ISNewNet = true;
		public NetUpdateProxy NetUpdateProxy = new NetUpdateProxy();
		public MixAndVolume mixAndVolume = new MixAndVolume();
		public List<PipeData> Covering = new List<PipeData>();

		public List<PipeData> CanEqualiseWith = new List<PipeData>();

		//public List<PipeData> Inputs = new List<PipeData>();
		public PipeNetAction pipeNetAction = null;

		public void AddEqualiseWith(PipeData pipeData)
		{
			if (CanEqualiseWith.Contains(pipeData) == false)
			{
				CanEqualiseWith.Add(pipeData);
			}
		}

		/*public void AddInput(PipeData pipeData)
		{
			Inputs.Add(pipeData);
		}*/

		public void RemoveEqualiseWith(PipeData pipeData)
		{
			CanEqualiseWith.Remove(pipeData);
			//Inputs.Remove(pipeData);
		}

		public void AddPipe(PipeData pipeData)
		{
			if (ISNewNet)
			{
				pipeData.OnNet = this;
				this.mixAndVolume = pipeData.mixAndVolume.Clone();
				pipeData.mixAndVolume.Empty();
				this.Covering.Add(pipeData);
				this.NetUpdateProxy.OnNet = this;
				this.NetUpdateProxy.OnEnable();
				if (pipeData.CustomLogic == CustomLogic.CoolingPipe)
				{
					this.pipeNetAction = new CoolingNet();
					this.pipeNetAction.LiquidPipeNet = this;
				}

				ISNewNet = false;

			}
			else
			{
				if (pipeData.OnNet == null)
				{
					pipeData.OnNet = this;
					mixAndVolume.Add(pipeData.mixAndVolume);
					Covering.Add(pipeData);
				}
				else
				{
					if (this != pipeData.OnNet)
					{
						this.CombinePipeNets(pipeData.OnNet);
					}
				}
			}
		}

		public void RemovePipe(PipeData pipeData)
		{
			pipeData.OnNet = null;

			var Outmix = mixAndVolume.Take(pipeData.mixAndVolume);
			Covering.Remove(pipeData);

			pipeData.SpillContent(Outmix);

			SplitPipeNets();
		}

		public void CombinePipeNets(LiquidPipeNet LiquidPipeNet)
		{
			CanEqualiseWith = CanEqualiseWith.Union(LiquidPipeNet.CanEqualiseWith).ToList();
			Covering.AddRange(LiquidPipeNet.Covering);
			foreach (var pipe in LiquidPipeNet.Covering)
			{
				pipe.OnNet = this;
			}

			mixAndVolume.Add(LiquidPipeNet.mixAndVolume);
			LiquidPipeNet.DisableThis();
		}

		public void SpreadPipenet(PipeData pipe)
		{
			List<PipeData> foundPipes = new List<PipeData>();
			foundPipes.Add(pipe);
			while (foundPipes.Count > 0)
			{
				var foundPipe = foundPipes[0];
				AddPipe(foundPipe);
				foundPipes.Remove(foundPipe);
				for (int i = 0; i < foundPipe.ConnectedPipes.Count; i++)
				{
					var nextPipe = foundPipe.ConnectedPipes[i];
					if (nextPipe.NetCompatible && nextPipe.OnNet == null)
					{
						foundPipes.Add(nextPipe);
					}
				}
			}
		}

		public void SplitPipeNets()
		{
			if (mixAndVolume.TheVolume == 0) return; //Assuming that there was only one pipe and it's already been removed from It Spilling
			foreach (var pipe in Covering)
			{
				//mixAndVolume.Take(pipe.mixAndVolume);
				pipe.OnNet = null;
			}

			LiquidPipeNet newPipenet = new LiquidPipeNet();
			var separatedPipenets = new List<LiquidPipeNet>(){newPipenet};
			for (int i = 0; i < Covering.Count; i++)
			{
				var pipe = Covering[i];
				if(pipe.OnNet == null)
				{
					if(newPipenet == null)
					{
						newPipenet = new LiquidPipeNet();
						separatedPipenets.Add(newPipenet);
					}
					newPipenet.SpreadPipenet(pipe);
					newPipenet = null;
				}
			}

			mixAndVolume.Divide(mixAndVolume.TheVolume);
			for (int i = 0; i < separatedPipenets.Count; i++)
			{
				var pipenet = separatedPipenets[i];
				var MultiplyMixed = mixAndVolume.Clone();
				MultiplyMixed.Multiply(pipenet.mixAndVolume.TheVolume);
				pipenet.mixAndVolume.Add(MultiplyMixed, false);
			}

			DisableThis();
		}


		public static LiquidPipeNet MakeNewNet(PipeData pipeData)
		{
			var Net = new LiquidPipeNet();
			Net.AddPipe(pipeData);
			return (Net);
		}

		public void DisableThis()
		{
			NetUpdateProxy.OnDisable();
		}

		public void TickUpdate()
		{
			mixAndVolume.EqualiseWithOutputs(CanEqualiseWith);
			if (pipeNetAction != null)
			{
				pipeNetAction.TickUpdate();
			}
		}

		public string ToAnalyserExamineString()
		{
			return " mixAndVolume > " +
			       mixAndVolume.ToString();
		}

		public override string ToString()
		{
			return "Covering > " + Covering.Count + " Outputs " + CanEqualiseWith.Count + " mixAndVolume > " +
			       mixAndVolume.ToString();
		}
	}

	public class NetUpdateProxy : PipeData
	{
		public override void OnEnable()
		{
			AtmosManager.Instance.inGameNewPipes.Add(this);
		}

		public override void OnDisable()
		{
			AtmosManager.Instance.inGameNewPipes.Remove(this);
		}

		public override void TickUpdate()
		{
			OnNet.TickUpdate();
		}
	}

	public class PipeNetAction
	{
		public LiquidPipeNet LiquidPipeNet;

		public virtual void TickUpdate()
		{
		}
	}

	public class CoolingNet : PipeNetAction
	{
		public override void TickUpdate()
		{
			// TODO Why was this commented out? Remove it or state a reason.
			// float EnergyChange = 0;
			// if (LiquidPipeNet.mixAndVolume.Mix.Total > 0)
			// {
			// var SmallMix = LiquidPipeNet.mixAndVolume.Mix.Clone();
			// SmallMix.Divide(LiquidPipeNet.Covering.Count);
			// foreach (var pipe in LiquidPipeNet.Covering)
			// {
			// var Node = pipe.matrix.GetMetaDataNode(pipe.MatrixPos);
			// EnergyChange += EqualisePipe(Node, SmallMix);
			// }
			// needs a better equation for this so It doesnt go to minus
			// LiquidPipeNet.mixAndVolume.Mix.InternalEnergy += (EnergyChange);
			// if (LiquidPipeNet.mixAndVolume.Mix.InternalEnergy < 0)
			// {
			// LiquidPipeNet.mixAndVolume.Mix.InternalEnergy = LiquidPipeNet.mixAndVolume.Mix.Total;
			// }
			// }
		}

		public const float StefanBoltzmannConstant = 0.5f;

		public float EqualisePipe(MetaDataNode Node, ReagentMix Mix)
		{
			//add Radiation of Heat
/*
			float EnergyChange = 0f; //Minuses energy taken away, + is energy added to the pipe
			if (Node.IsSpace)
			{
				//Radiation
				//Invisible pipes to radiation
				EnergyChange = -(StefanBoltzmannConstant * (Mix.InternalEnergy));
				Node.GasMix = GasMixes.Space;
			}

			float EnergyTransfered =
				((390 * (Node.GasMix.Temperature -Mix.Temperature)) / 0.01f);

			float EqualiseTemperature = (Node.GasMix.InternalEnergy + Mix.InternalEnergy) /
				(Node.GasMix.WholeHeatCapacity + Mix.WholeHeatCapacity);

			var ChangeInInternalEnergy = Mix.WholeHeatCapacity* (EqualiseTemperature -Mix.Temperature);

			if (Math.Abs( EnergyTransfered) > Math.Abs( ChangeInInternalEnergy))
			{
				EnergyChange += ChangeInInternalEnergy;
			}
			else
			{
				EnergyChange += EnergyTransfered;
			}
			//Logger.Log("EnergyChange > " + EnergyChange);
			var gas = Node.GasMix;
			gas.InternalEnergy = gas.InternalEnergy + (-EnergyChange);
			if (gas.InternalEnergy < 0)
			{
				Logger.LogWarning("OHHHH", Category.Atmos);
				gas.InternalEnergy = 1;
			}
			Node.GasMix = gas;
			return (EnergyChange);
			*/
			return (0);
		}
	}
}