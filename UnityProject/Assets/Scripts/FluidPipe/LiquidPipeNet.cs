using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using Chemistry;
using UnityEngine;

namespace Pipes
{
	public class LiquidPipeNet
	{
		public NetUpdateProxy NetUpdateProxy = new NetUpdateProxy();
		public MixAndVolume mixAndVolume = new MixAndVolume();
		public List<PipeData> Covering = new List<PipeData>();
		public List<PipeData> Outputs = new List<PipeData>();
		//public List<PipeData> Inputs = new List<PipeData>();

		public void AddOutput(PipeData pipeData)
		{
			Outputs.Add(pipeData);
		}

		/*public void AddInput(PipeData pipeData)
		{
			Inputs.Add(pipeData);
		}*/

		public void RemoveOutput(PipeData pipeData)
		{
			Outputs.Remove(pipeData);
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
			Outputs.AddRange(LiquidPipeNet.Covering);
			Covering.AddRange(LiquidPipeNet.Covering);
			foreach (var pipe in LiquidPipeNet.Covering)
			{
				pipe.OnNet = LiquidPipeNet;
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
			Net.mixAndVolume.Volume += pipeData.mixAndVolume.Volume;
			Net.Covering.Add(pipeData);
			Net.NetUpdateProxy.OnNet = Net;
			Net.NetUpdateProxy.OnEnable();
			return (Net);
		}

		public void DisableThis()
		{
			NetUpdateProxy.OnDisable();
		}

		public void TickUpdate()
		{
			mixAndVolume.EqualiseWithOutputs(Outputs);
		}

		public override string ToString()
		{
			return "Covering > " + Covering.Count + " Outputs " + Outputs.Count + " mixAndVolume > " +
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
}
