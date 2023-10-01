using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Objects.Wallmounts;
using Shared.Managers;
using Systems.Pipes;
using UnityEngine;
using UnityEngine.Profiling;

namespace Systems.Atmospherics
{
	public class AtmosManager : SingletonManager<AtmosManager>
	{

		public List<Action> removeingUpdates = new List<Action>();
		public List<Action> addingUpdates = new List<Action>();

		public List<Action> atmosphericsUpdates = new List<Action>();

		public List<PipeData> removeingpipeList = new List<PipeData>();
		public List<PipeData> addingpipeList = new List<PipeData>();

		public List<PipeData> pipeList = new List<PipeData>();


		public List<ReactionManager> reactionManagerList = new List<ReactionManager>();

		private AtmosThread atmosThread;
		public AtmosSimulation simulation;
		public CustomSampler sampler;

		public bool StopPipes = false;

		//TODO: move these prefabs somewhere else more appropiate
		[field: SerializeField]
		public GameObject FireLight { get; private set; }

		[field: SerializeField]
		public GameObject IceShard { get; private set; }

		[field: SerializeField]
		public GameObject HotIce { get; private set; }

		[field: SerializeField]
		public GameObject MetalHydrogen { get; private set; }

		public override void Awake()
		{
			base.Awake();
			atmosThread = gameObject.AddComponent<AtmosThread>();
			atmosThread.tickDelay = 40;
			simulation = new AtmosSimulation();
			sampler = CustomSampler.Create("AtmosphericsStep");
		}

		private void OnEnable()
		{
			EventManager.AddHandler(Event.PostRoundStarted, OnPostRoundStart);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.PostRoundStarted, OnPostRoundStart);
			Stop();
		}

		private void OnPostRoundStart()
		{
			if (CustomNetworkManager.IsServer == false) return;

			atmosThread.StartThread();
		}

		private void Stop()
		{
			atmosThread.StopThread();
			GasReactions.ResetReactionList();
			simulation.ClearUpdateList();
			reactionManagerList.Clear();
			atmosphericsUpdates.Clear();
		}

		public void ProcessAction(Action action)
		{
			action?.Invoke();
		}

		public void ProcessPipe(PipeData pipeData)
		{
			pipeData.TickUpdate();
		}

		public void AddPipe(PipeData pipeData)
		{
			lock (addingpipeList)
			{
				if (removeingpipeList.Contains(pipeData))
				{
					removeingpipeList.Remove(pipeData);
				}

				if (addingpipeList.Contains(pipeData) == false)
				{
					addingpipeList.Add(pipeData);
				}

			}
		}

		public void RemovePipe(PipeData pipeData)
		{
			lock (addingpipeList)
			{
				if (addingpipeList.Contains(pipeData))
				{
					addingpipeList.Remove(pipeData);
				}

				if (removeingpipeList.Contains(pipeData) == false)
				{
					removeingpipeList.Add(pipeData);
				}
			}
		}

		public void AddUpdate(Action updateAction)
		{
			lock (addingUpdates)
			{
				if (removeingUpdates.Contains(updateAction))
				{
					removeingUpdates.Remove(updateAction);
				}

				if (addingUpdates.Contains(updateAction) == false)
				{
					addingUpdates.Add(updateAction);
				}
			}

		}

		public void RemoveUpdate(Action updateAction)
		{
			lock (addingUpdates)
			{
				if (addingUpdates.Contains(updateAction))
				{
					addingUpdates.Remove(updateAction);
				}

				if (removeingUpdates.Contains(updateAction) == false)
				{
					removeingUpdates.Add(updateAction);
				}
			}

		}


		public void UpdateNode(MetaDataNode node)
		{
			simulation.AddToUpdateList(node);
		}
	}
}
