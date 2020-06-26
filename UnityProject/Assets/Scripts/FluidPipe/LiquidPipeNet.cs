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
			if (pipeData.OnNet == null)
			{
				pipeData.OnNet = this;
				mixAndVolume.Mix.Add(pipeData.mixAndVolume.Mix);
				mixAndVolume.Volume += pipeData.mixAndVolume.Volume;
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

		public void RemovePipe(PipeData pipeData)
		{
			pipeData.OnNet = null;

			var Outmix = mixAndVolume.Mix.Take(mixAndVolume.Mix.Total / Covering.Count);
			Covering.Remove(pipeData);
			mixAndVolume.Volume -= pipeData.mixAndVolume.Volume;
			MatrixManager.ReagentReact(Outmix, pipeData.MatrixPos); //TODO AAAAAAAA Get the correct location
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
			mixAndVolume.Volume += LiquidPipeNet.mixAndVolume.Volume;
			mixAndVolume.Mix.Add(LiquidPipeNet.mixAndVolume.Mix);
			LiquidPipeNet.DisableThis();
		}

		public void SplitPipeNets()
		{

			//Not the most optimal way of doing it but the easiest TODO Optimise this
			mixAndVolume.Mix.Divide(Covering.Count);
			foreach (var pipe in Covering)
			{
				pipe.mixAndVolume.Mix = mixAndVolume.Mix.Clone();
				pipe.OnNet = null;
			}

			foreach (var pipe in Covering)
			{
				pipe.LiquidFindNetWork();
			}

			DisableThis();
		}


		public static LiquidPipeNet MakeNewNet(PipeData pipeData)
		{
			var Net = new LiquidPipeNet();
			pipeData.OnNet = Net;
			Net.mixAndVolume.Mix.Add(pipeData.mixAndVolume.Mix);
			Net.mixAndVolume.Volume = pipeData.mixAndVolume.Volume;
			Net.Covering.Add(pipeData);
			Net.NetUpdateProxy.OnNet = Net;
			Net.NetUpdateProxy.OnEnable();
			if (pipeData.CustomLogic == CustomLogic.CoolingPipe)
			{
				Net.pipeNetAction = new CoolingNet();
				Net.pipeNetAction.LiquidPipeNet = Net;

			}
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
			float EnergyChange = 0;
			if (LiquidPipeNet.mixAndVolume.Mix.Total > 0)
			{
				var SmallMix = LiquidPipeNet.mixAndVolume.Mix.Clone();
				SmallMix.Divide(LiquidPipeNet.Covering.Count);
				foreach (var pipe in LiquidPipeNet.Covering)
				{
					var Node = pipe.matrix.GetMetaDataNode(pipe.MatrixPos);
					EnergyChange += EqualisePipe(Node, SmallMix);
				}
				//needs a better equation for this so It doesnt go to minus
				LiquidPipeNet.mixAndVolume.Mix.InternalEnergy += (EnergyChange);
				if (LiquidPipeNet.mixAndVolume.Mix.InternalEnergy < 0)
				{
					LiquidPipeNet.mixAndVolume.Mix.InternalEnergy = LiquidPipeNet.mixAndVolume.Mix.Total;
				}
			}

		}

		public const float StefanBoltzmannConstant = 0.5f;
		public float EqualisePipe(MetaDataNode Node,ReagentMix Mix )
		{
			//add Radiation of Heat

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
		}


	}
}
