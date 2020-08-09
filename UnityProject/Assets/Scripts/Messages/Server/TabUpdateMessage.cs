using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

public class TabUpdateMessage : ServerMessage
{
	public uint Provider;
	public NetTabType Type;
	public TabAction Action;

	public ElementValue[] ElementValues;

	public bool Touched;

	private static readonly ElementValue[] NoValues = new ElementValue[0];

	private static Dictionary<int, Tuple<ElementValue[], int>> ElementValuesCache = new Dictionary<int, Tuple<ElementValue[], int>>();

	private static HashSet<int> ServerUniqueIdCache = new HashSet<int>();

	public int ID;
	public int UniqueID;
	public int NumOfMessages;

	public override void Process()
	{
		Logger.LogTraceFormat("Processed {0}", Category.NetUI, this);
		LoadNetworkObject(Provider);

		//If start or middle of message add to cache then stop
		if (ID == 1)
		{
			if (ElementValuesCache.Count == 0 || !ElementValuesCache.ContainsKey(UniqueID))
			{
				ElementValuesCache.Add(UniqueID, new Tuple<ElementValue[], int>(ElementValues, 1));
				return;
			}

			if (NumOfMessages == ElementValuesCache[UniqueID].Item2 + 1)
			{
				Debug.LogError("This message didnt arrive in time before the end message!");
				ElementValuesCache.Remove(UniqueID);
				return;
			}

			ElementValuesCache[UniqueID] = new Tuple<ElementValue[], int>(Concat(ElementValuesCache[UniqueID].Item1, ElementValues), ElementValuesCache[UniqueID].Item2 + 1);
			return;
		}

		//If end of message add
		if(ID == 2)
		{
			ElementValuesCache[UniqueID] = new Tuple<ElementValue[], int>(Concat(ElementValuesCache[UniqueID].Item1, ElementValues), ElementValuesCache[UniqueID].Item2 + 1);

			if (NumOfMessages != ElementValuesCache[UniqueID].Item2)
			{
				Debug.LogError("Not all the messages arrived in time for the NetUI update.");
				return;
			}

			ElementValues = ElementValuesCache[UniqueID].Item1;
			ElementValuesCache.Remove(UniqueID);
		}


		switch (Action)
		{
			case TabAction.Open:
				ControlTabs.ShowTab(Type, NetworkObject, ElementValues);
				break;
			case TabAction.Close:
				ControlTabs.CloseTab(Type, NetworkObject);
				break;
			case TabAction.Update:
				ControlTabs.UpdateTab(Type, NetworkObject, ElementValues, Touched);
				break;
		}
	}

	public override string ToString()
	{
		return $"[TabUpdateMessage {nameof(Provider)}: {Provider}, {nameof(Type)}: {Type}, " +
		       $"{nameof(Action)}: {Action}, " +
		       $"{nameof(ElementValue)}: {string.Join("; ", ElementValues ?? NoValues)}]";
	}

	public static void SendToPeepers(GameObject provider, NetTabType type, TabAction tabAction, ElementValue[] values = null)
	{
		//Notify all peeping players of the change
		var list = NetworkTabManager.Instance.GetPeepers(provider, type);
		foreach (var connectedPlayer in list)
		{
			Send(connectedPlayer.GameObject, provider, type, tabAction, null, values);
		}
	}

	public static TabUpdateMessage Send(GameObject recipient, GameObject provider, NetTabType type, TabAction tabAction,
		GameObject changedBy = null,
		ElementValue[] values = null)
	{

		switch (tabAction)
		{
			case TabAction.Open:
				NetworkTabManager.Instance.Add(provider, type, recipient);
				values = NetworkTabManager.Instance.Get(provider, type).ElementValues;
				break;
			case TabAction.Close:
				NetworkTabManager.Instance.Remove(provider, type, recipient);
				break;
			case TabAction.Update:
				//fixme: duplication of NetTab.ValidatePeepers
				//Not sending updates and closing tab for players that don't pass the validation anymore
				var validate = Validations.CanApply(recipient, provider, NetworkSide.Server);
				if (!validate)
				{
					Send(recipient, provider, type, TabAction.Close);
					return null;
				}
				break;
		}

		// SingleMessage: 0, start/middle: 1, end: 2
		var id = 0;

		var uniqueID = Random.Range(0, 2000);

		if (ServerUniqueIdCache.Contains(uniqueID))
		{
			uniqueID = Random.Range(2001, 20000);
		}

		ServerUniqueIdCache.Add(uniqueID);

		var elementValuesLists = new Dictionary<List<ElementValue>, int>();

		if (values != null && tabAction != TabAction.Close)
		{
			// get max possible packet size from current transform
			int maxPacketSize = Transport.activeTransport.GetMaxPacketSize(0);

			// set currentSize start value to max TCP header size (60b)
			float currentSize = 60f;

			var elementValues = new List<ElementValue>();

			var length = values.Length;

			var totalSize = 0f;

			foreach (var value in values)
			{
				var size = value.GetSize();

				if (size + 60 >= maxPacketSize)
				{
					Debug.LogError("This value is above the max mirror packet limit, and cannot be split.");
					return null;
				}

				totalSize += size;
			}

			var divisions = (int)Math.Ceiling(totalSize / maxPacketSize);

			var currentDivision = 0;

			for (var i = 0; i < length; i++)
			{
				currentSize += values[i].GetSize();

				if (currentSize > maxPacketSize)
				{
					currentDivision ++;
					currentSize = 60f;

					id = 1;

					if (currentDivision == divisions)
					{
						id = 2;
					}

					elementValuesLists.Add(elementValues, id);
					elementValues = new List<ElementValue>();
				}

				elementValues.Add(values[i]);
			}

			if (elementValuesLists.Count == 0)
			{
				values = elementValues.ToArray();
			}
			else if (currentDivision != divisions)
			{
				elementValuesLists.Add(elementValues, 2);
			}
		}

		var count = elementValuesLists.Count;

		if (count == 0)
		{
			var msg = new TabUpdateMessage
			{
				Provider = provider.NetId(),
				Type = type,
				Action = tabAction,
				ElementValues = values,
				Touched = changedBy != null,
				ID = id,
				UniqueID = uniqueID
			};

			msg.SendTo(recipient);
			Logger.LogTraceFormat("{0}", Category.NetUI, msg);
			return null;
		}

		foreach (var value in elementValuesLists)
		{
			var msg = new TabUpdateMessage
			{
				Provider = provider.NetId(),
				Type = type,
				Action = tabAction,
				ElementValues = value.Key.ToArray(),
				Touched = changedBy != null,
				ID = value.Value,
				UniqueID = uniqueID,
				NumOfMessages = count
			};

			msg.SendTo(recipient);
			Logger.LogTraceFormat("{0}", Category.NetUI, msg);

			if (value.Value == 2)
			{
				ServerUniqueIdCache.Remove(uniqueID);
			}
		}

		return null;
	}

	//Merge arrays together
	public static T[] Concat<T>(params T[][] arrays)
	{
		var result = new T[arrays.Sum(a => a.Length)];
		int offset = 0;
		for (int x = 0; x < arrays.Length; x++)
		{
			arrays[x].CopyTo(result, offset);
			offset += arrays[x].Length;
		}
		return result;
	}
}

public struct ElementValue
{
	public string Id;
	public byte[] Value;

	public override string ToString()
	{
		return $"[{Id}={Value}]";
	}

	/// <summary>
	/// Get size of this object (in bytes)
	/// </summary>
	/// <returns>size of this object (in bytes)</returns>
	public float GetSize()
	{
		return sizeof(char) * Id.Length		// Id
			+ sizeof(byte) * Value.Length;	// Value
	}
}

public enum TabAction
{
	Open,
	Close,
	Update
}