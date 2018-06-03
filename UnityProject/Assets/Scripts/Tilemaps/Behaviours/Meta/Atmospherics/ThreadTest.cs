﻿using System.Collections.Concurrent;
using System.Threading;
using Tilemaps.Behaviours.Layers;
using UnityEngine;

namespace Tilemaps.Behaviours.Meta
{
	public class ThreadTest
	{
		private bool running = true;

		private Object lockGetWork = new Object();

		private Atmospherics atmos;
		
		public ThreadTest(MetaDataLayer metaDataLayer)
		{
			atmos = new Atmospherics(metaDataLayer);
		}

		public void Enqueue(Vector3Int position)
		{
			atmos.AddToUpdateList(position);
			
			lock (lockGetWork)
			{
				Monitor.PulseAll(lockGetWork);
			}
		}

		public void Run()
		{
			while (running)
			{
				if (!atmos.IsIdle)
				{
					atmos.run();
				}
				else
				{
					lock (lockGetWork)
					{
						Monitor.Wait(lockGetWork);
					}
				}
			}
		}

		public void Stop()
		{
			running = false;

			lock (lockGetWork)
			{
				Monitor.PulseAll(lockGetWork);
			}
		}
	}
}