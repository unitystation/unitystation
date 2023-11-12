
using System.Collections.Generic;
using Logs;
using UnityEngine;

namespace UI.Core.NetUI
{
	/// <summary>
	/// For containing objects that are spawned ingame
	/// </summary>
	public class SpawnedObjectList : NetUIDynamicList
	{
		public ObjectChangeEvent OnObjectChange { get; } = new ObjectChangeEvent();

		public bool AddObjects(List<GameObject> objects)
		{
			//fixme: duplicate of RadarList code:
			var objectSet = new HashSet<GameObject>(objects);
			var duplicates = new HashSet<GameObject>();
			for (var i = 0; i < Entries.Count; i++)
			{
				var item = Entries[i] as SpawnedObjectEntry;
				if (!item)
				{
					continue;
				}

				if (objectSet.Contains(item.TrackedObject))
				{
					duplicates.Add(item.TrackedObject);
				}
			}

			for (var i = 0; i < objects.Count; i++)
			{
				var obj = objects[i];
				//skipping already found objects
				if (duplicates.Contains(obj))
				{
					continue;
				}

				//add new entry
				SpawnedObjectEntry newEntry = Add() as SpawnedObjectEntry;
				if (!newEntry)
				{
					Loggy.LogWarning($"SpawnedObjectList: Added {newEntry} is not an SpawnedObjectEntry!", Category.NetUI);
					return false;
				}

				//set its elements
				newEntry.OnObjectChange = this.OnObjectChange;
				newEntry.TrackedObject = obj;
			}

			//rescan elements and notify
			NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
			//		RefreshTrackedPos();

			return true;
		}
	}
}
