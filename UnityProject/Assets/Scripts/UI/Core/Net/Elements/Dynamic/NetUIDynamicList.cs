using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;
using UnityEngine.UI;
using Messages.Server;
using Tilemaps.Behaviours.Meta;

namespace UI.Core.NetUI
{
	/// <summary>
	/// Base class for List of dynamic entries, which can be added/removed at runtime.
	/// Setting Value actually creates/removes entries for client
	/// </summary>
	public class NetUIDynamicList : NetUIElement<string[]>
	{
		public override ElementMode InteractionMode => ElementMode.ServerWrite;
		private int entryCount = 0;
		public string EntryPrefix => gameObject.name; //= String.Empty;

		public List<DynamicEntry>  Entries = new List<DynamicEntry>();

		public GameObject EntryPrefab;

		public override string[] Value {
			get => EntryIndex.Keys.ToArray();
			protected set {
				externalChange = true;

				if (value.Length == 0)
				{
					Clear();
				}
				else
				{
					//add ones existing in proposed only, remove ones not existing in proposed
					//could probably be cheaper
					var existing = EntryIndex.Keys;
					var toRemove = existing.Except(value).ToArray();
					var toAdd = value.Except(existing).ToArray();
					Remove(toRemove);
					AddBulk(toAdd);
				}

				externalChange = false;
			}
		}

		public Dictionary<string, DynamicEntry> EntryIndex {
			get {
				var dynamicEntries = new Dictionary<string, DynamicEntry>();
				var entries = Entries;
				foreach (var entry in entries)
				{
					var entryName = entry.name;
					if (dynamicEntries.ContainsKey(entryName))
					{
						Loggy.LogWarning($"Duplicate entry name {entryName}, something's wrong", Category.NetUI);
						continue;
					}

					dynamicEntries.Add(entryName, entry);
				}

				return dynamicEntries;
			}
		}

		public override void Init()
		{
			if (!EntryPrefab)
			{
				var elementType = $"{containedInTab.Type}Entry";
				Loggy.LogFormat("{0} dynamic list: EntryPrefab not assigned, trying to find it as '{1}'", Category.NetUI,
					gameObject.name, elementType);
				EntryPrefab = NetworkTabManager.Instance.NetEntries.GetFromName(elementType);

				if (EntryPrefab == null)
				{
					Loggy.LogError($"Failed to find net entry {elementType} for {gameObject.name}", Category.NetUI);
				}
			}

			foreach (var value in Entries)
			{
				InitDynamicEntry(value);
			}
		}

		public override string ToString()
		{
			return string.Join(",", Value);
		}
		public virtual void Clear()
		{
			foreach (var entry in Entries)
			{
				DestroyImmediate(entry.gameObject);
			}
			Entries.Clear();
			entryCount = 0;
			RearrangeListItems();
		}

		/// <summary>
		/// [Server]
		/// Sets up proper layout for entries and sends coordinates to peepers
		/// </summary>
		private void RearrangeListItems()
		{
			if (containedInTab.IsMasterTab)
			{
				NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
				RefreshPositions();
				UpdatePeepers();
			}

			// rebuild layout to fix bug with moved UI elements position
			LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
		}

		/// <summary>
		/// Remove entry by its name-index
		/// </summary>
		public void Remove(string toBeRemoved)
		{
			Remove(new[] { toBeRemoved });
		}

		public void MasterRemoveItem(DynamicEntry EntryToRemove)
		{
			Remove(EntryToRemove.name);

			// rescan elements and notify
			NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
			UpdatePeepers();
		}

		public DynamicEntry AddItem()
		{
			var newEntry = Add();

			// rescan elements and notify
			NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
			UpdatePeepers();

			return newEntry;
		}

		/// <summary>
		/// Remove entries by their name-index
		/// </summary>
		public void Remove(string[] toBeRemoved)
		{
			var entries = EntryIndex;

			foreach (var itemName in toBeRemoved)
			{
				var entryToRemove = entries[itemName];
				entries.Remove(itemName);
				Entries.Remove(entryToRemove);
				DestroyImmediate(entryToRemove.gameObject);

			}

			RearrangeListItems();


		}

		protected DynamicEntry[] AddBulk(string[] proposedIndices)
		{
			var dynamicEntries = new DynamicEntry[proposedIndices.Length];
			var mode = proposedIndices.Length > 1 ? "Bulk" : "Single";

			for (var i = 0; i < proposedIndices.Length; i++)
			{
				var proposedIndex = proposedIndices[i];
				var dynamicEntry = SpawnEntry();
				var resultIndex = InitDynamicEntry(dynamicEntry, proposedIndex);

				if (resultIndex != string.Empty)
				{
					Loggy.LogTraceFormat("{0} spawning dynamic entry #[{1}]: proposed: [{2}], entry: {3}", Category.NetUI,
						mode, resultIndex, proposedIndex, dynamicEntry);
				}
				else
				{
					Loggy.LogWarningFormat(
						"Dynamic entry \"{0}\" {1} spawn failure, something is wrong with {2}", Category.NetUI,
						proposedIndex, mode, dynamicEntry);
				}

				dynamicEntries[i] = dynamicEntry;
				Entries.Add(dynamicEntry);
			}

			RearrangeListItems();
			return dynamicEntries;
		}

		private DynamicEntry SpawnEntry()
		{
			DynamicEntry dynamicEntry = null;

			var entryObject = Instantiate(EntryPrefab, transform, false);
			dynamicEntry = entryObject.GetComponent<DynamicEntry>();

			return dynamicEntry;
		}

		/// <summary>
		/// Adds new entry at given index (or generates index if none is provided)
		/// Does NOT notify players implicitly
		/// </summary>
		protected DynamicEntry Add(string proposedIndex = "")
		{
			return AddBulk(new[] { proposedIndex })[0];
		}

		/// <summary>
		/// Need to run this on list change to ensure no gaps are present
		/// </summary>
		protected virtual void RefreshPositions()
		{
			//Adding new entries to the end by default
			var entries = Entries;
			var orderByDescending = entries.OrderByDescending(x => x.name).ToList();
			for (var i = 0; i < orderByDescending.Count; i++)
			{
				SetProperPosition(entries[i], i);
			}
		}

		/// <summary>
		/// Defines the way list items are positioned.
		/// Adds next entries directly below (using height) by default
		/// </summary>
		protected virtual void SetProperPosition(DynamicEntry entry, int sortIndex = 0)
		{
			var rect = entry.gameObject.GetComponent<RectTransform>();
			rect.anchoredPosition = Vector3.down * rect.rect.height * sortIndex;
		}

		/// <summary>
		///Not just own value, include inner elements' values as well
		/// </summary>
		protected override void UpdatePeepersLogic()
		{
			var valuesToSend = new List<ElementValue> { ElementValue };
			// Don't use LINQ, this is in the hot path
			foreach (var entry in Entries)
			{
				foreach (var element in entry.Elements)
				{
					valuesToSend.Add(element.ElementValue);
				}
			}

			TabUpdateMessage.SendToPeepers(containedInTab.Provider, containedInTab.Type, TabAction.Update, new []{ElementValue} );
		}

		/// <summary>
		/// Sets entry name to index, also makes its elements unique by appending index as postfix
		/// </summary>
		private string InitDynamicEntry(DynamicEntry entry, string desiredName = "")
		{
			if (!entry)
			{
				return string.Empty;
			}

			var index = desiredName;
			if (string.IsNullOrEmpty(desiredName))
			{
				index = EntryPrefix == string.Empty ? entryCount++.ToString() : EntryPrefix + ":" + entryCount++;
			}

			entry.name = index;

			//Making inner elements' names unique by adding "index" to the end
			foreach (var innerElement in entry.Elements)
			{
				if (innerElement == entry)
				{
					//not including self!
					continue;
				}

				if (innerElement.name.Contains(DELIMITER))
				{
					if (innerElement.name.Contains(DELIMITER + index))
					{
						//Same index - ignore
						return index;
					}
					else
					{
						Loggy.LogTraceFormat("Reuse: Inner element {0} already had indexed name, while {1} was expected",
							Category.NetUI, innerElement, index);
						//Different index - cut and let set it again
						innerElement.name = innerElement.name.Split(DELIMITER)[0];
					}
				}

				//postfix and not prefix because of how NetKeyButton works
				innerElement.name = innerElement.name + DELIMITER + index;
				if (entry == innerElement)
				{
					Loggy.LogError("Multiple net elements on one gameobject this is not supported");
				}

			}

			return index;
		}

		/// <summary>
		/// Deactivate entry gameobject after putting it to pool and vice versa
		/// </summary>
		/// <typeparam name="T"></typeparam>
		protected class UniqueEntryQueue<T> : UniqueQueue<T> where T : MonoBehaviour
		{
			protected override void AfterEnqueue(T enqueuedItem)
			{
				enqueuedItem.gameObject.SetActive(false);
			}

			protected override void AfterDequeue(T dequeuedItem)
			{
				dequeuedItem.gameObject.SetActive(true);
			}
		}

		public override void ExecuteServer(PlayerInfo subject) { }
	}
}
