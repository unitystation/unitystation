using System.Threading;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;
using US13.Core.Chat;
using US13.Managers.NetworkManagement;

namespace US13.Tilemaps.Behaviours.Meta
{
	public class MatrixSystemManager : MonoBehaviour
	{
		private List<MatrixSystemBehaviour> systems = new List<MatrixSystemBehaviour>();
		private bool initialized;
		public bool IsInitialized => initialized;

		private List<IInitialiseSystem> InitSystems = new List<IInitialiseSystem>();

		public void SelfInitialize()
		{
			Initialize(this.GetCancellationTokenOnDestroy()).Forget(e => Logs.Loggy.Error(e.ToString()));
		}



		public async UniTask Initialize(CancellationToken cancellationToken = default)
		{
			systems = systems.OrderByDescending(s => s.Priority).ToList();
			foreach (var system in systems)
			{
				try
				{
					system.Initialize();
				}
				catch (Exception e)
				{
					Chat.AddGameWideSystemMsgToChat($"<color=red>Error when initialising  {nameof(system)} on {this.name}. Weird stuff might happen, check logs for error..</color>");
					Loggy.Error(e.ToString());
				}

				await UniTask.NextFrame(cancellationToken: cancellationToken);
			}

			InitSystems = InitSystems.OrderByDescending(s => s.Priority).ToList();
			foreach (var system in InitSystems)
			{
				try
				{
					system.Initialize();
				}
				catch (Exception e)
				{
					Chat.AddGameWideSystemMsgToChat($"<color=red>Error when initialising {nameof(system)} on {this.name}. Weird stuff might happen, check logs for error..</color>");
					Loggy.Error(e.ToString());
				}

				await UniTask.NextFrame(cancellationToken: cancellationToken);
			}
			initialized = true;
		}

		public void Register(MatrixSystemBehaviour system)
		{
			systems.Add(system);
		}

		public void Register(IInitialiseSystem system)
		{
			InitSystems.Add(system);
		}

		public void UpdateAt(Vector3Int localPosition, SystemType ToUpDate = SystemType.All)
		{
			if (CustomNetworkManager.IsServer == false) return;

			if (!initialized)
			{
				return;
			}

			//ensuring no metadata tiles are created at non-zero Z
			localPosition.z = 0;

			for (int i = 0; i < systems.Count; i++)
			{
				if ((ToUpDate & systems[i].SubsystemType) != 0)
				{
					systems[i].UpdateAt(localPosition);
				}
			}
		}
	}
	[Flags]
	public enum SystemType
	{
		None = 0,
		AtmosSystem = 1 << 0,
		MetaDataSystem = 1 << 1,
		All = ~None
	}
}
