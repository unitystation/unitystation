using System.Collections.Generic;
using UnityEngine;
using Systems.Pipes;
using Objects.Wallmounts;
using Managers;
using UnityEngine.Profiling;

namespace Systems.Atmospherics
{
	public class AtmosManager : SingletonManager<AtmosManager>
	{
		public HashSet<PipeData> inGameNewPipes = new HashSet<PipeData>();
		public HashSet<FireAlarm> inGameFireAlarms = new HashSet<FireAlarm>();
		public ThreadSafeList<PipeData> pipeList = new ThreadSafeList<PipeData>();
		public GenericDelegate<PipeData> processPipeDelegator;

		public List<ReactionManager> reactionManagerList = new List<ReactionManager>();

		private AtmosThread atmosThread;
		public AtmosSimulation simulation;
		public CustomSampler sampler;

		public bool StopPipes = false;

		//TODO: move these prefabs somewhere else more appropiate
		public GameObject fireLight = null;
		public GameObject iceShard = null;
		public GameObject hotIce = null;

		public override void Awake()
		{
			base.Awake();
			processPipeDelegator = ProcessPipe;
			atmosThread = new AtmosThread();
			atmosThread.TickSpeed = 40;
			simulation = new AtmosSimulation();
			sampler = CustomSampler.Create("AtmosphericsStep");
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer)
			{
				EventManager.AddHandler(Event.PostRoundStarted, OnPostRoundStart);
			}
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer)
			{
				EventManager.RemoveHandler(Event.PostRoundStarted, OnPostRoundStart);
				Stop();
			}
		}

		private void OnPostRoundStart()
		{
			atmosThread.StartThread();
		}

		private void Stop()
		{
			atmosThread.StopThread();
			GasReactions.ResetReactionList();
			simulation.ClearUpdateList();
			inGameNewPipes.Clear();
			inGameFireAlarms.Clear();
			reactionManagerList.Clear();
		}

		private void ProcessPipe(PipeData pipeData)
		{
			pipeData.TickUpdate();
		}

		public void AddPipe(PipeData pipeData)
		{
			pipeList.Add(pipeData);
		}

		public void RemovePipe(PipeData pipeData)
		{
			pipeList.Remove(pipeData);
		}

		public void UpdateNode(MetaDataNode node)
		{
			simulation.AddToUpdateList(node);
		}
	}
}
