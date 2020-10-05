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

	private static int Counter = 0;

	public TabMessageType ID;
	public int UniqueID;
	public int NumOfMessages;

	public override void Process()
	{
		Logger.LogTraceFormat("Processed {0}", Category.NetUI, this);
		LoadNetworkObject(Provider);

		//If start or middle of message add to cache then stop
		if (ID == TabMessageType.MoreIncoming)
		{
			//If Unique Id doesnt exist make new entry
			if (ElementValuesCache.Count == 0 || !ElementValuesCache.ContainsKey(UniqueID))
			{
				ElementValuesCache.Add(UniqueID, new Tuple<ElementValue[], int>(ElementValues, 1));
				return;
			}

			//Sanity check to make sure this isnt the last message
			if (NumOfMessages == ElementValuesCache[UniqueID].Item2 + 1)
			{
				Debug.LogError("This message didnt arrive in time before the end message!");
				ElementValuesCache.Remove(UniqueID);
				return;
			}

			//Unique Id already exists so add arrays to each other
			ElementValuesCache[UniqueID] = new Tuple<ElementValue[], int>(Concat(ElementValuesCache[UniqueID].Item1, ElementValues), ElementValuesCache[UniqueID].Item2 + 1);
			return;
		}

		//If end of message add and continue
		if(ID == TabMessageType.EndOfMessage)
		{
			//Add the arrays together
			ElementValuesCache[UniqueID] = new Tuple<ElementValue[], int>(Concat(ElementValuesCache[UniqueID].Item1, ElementValues), ElementValuesCache[UniqueID].Item2 + 1);

			//Check to make sure its the last message
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
				var instance = NetworkTabManager.Instance.Get(provider, type);
				if (instance == null) return null;
				values = instance.ElementValues;
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

		// SingleMessage, MoreIncoming, EndOfMessage
		var id = TabMessageType.SingleMessage;

		Counter++;
		var uniqueID = Counter;

		if (Counter > 10000)
		{
			Counter = 0;
		}

		var elementValuesLists = new Dictionary<List<ElementValue>, TabMessageType>();

		if (values != null && tabAction != TabAction.Close)
		{
			// get max possible packet size from current transform
			var maxPacketSize = Transport.activeTransport.GetMaxPacketSize(0);

			// set currentSize start value to max TCP header size (60b)
			var currentSize = 100;

			//Stores the current cycle of ElementValues
			var elementValues = new List<ElementValue>();

			//How many values are being sent
			var length = values.Length;

			//Total packet size if all values sent together
			var totalSize = 0;

			//Work out totalSize
			foreach (var value in values)
			{
				var size = value.GetSize();

				//If a single value is bigger than max packet size cannot proceed
				if (size + 60 >= maxPacketSize)
				{
					Debug.LogError($"This value is above the max mirror packet limit, and cannot be split. Is {size + 60} bytes");
					return null;
				}

				totalSize += size;
			}

			//Rounds up to the max number of divisions of the max packet size will be needed for values
			var divisions = (int)Math.Ceiling((float)totalSize / maxPacketSize);

			//Counter for which division is currently being made
			var currentDivision = 0;

			//The loop for making the messages from the values
			for (var i = 0; i < length; i++)
			{
				//Keep adding values until bigger than packet size
				currentSize += values[i].GetSize();

				if (currentSize > maxPacketSize)
				{
					currentDivision ++;
					currentSize = 100;

					//Id MoreIncoming, means it is a multimessage but not the end.
					id = TabMessageType.MoreIncoming;

					//If last division then this will be the end, set to end Id of EndOfMessage
					if (currentDivision == divisions)
					{
						id = TabMessageType.EndOfMessage;
					}

					//Add value list to the message list
					elementValuesLists.Add(elementValues, id);
					elementValues = new List<ElementValue>();
				}

				elementValues.Add(values[i]);
			}

			//Single message
			if (elementValuesLists.Count == 0)
			{
				values = elementValues.ToArray();
			}
			//Multimessage, if end division hasnt been reached yet then this last list must be end.
			else if (currentDivision != divisions)
			{
				elementValuesLists.Add(elementValues, TabMessageType.EndOfMessage);
			}
		}

		var count = elementValuesLists.Count;

		//Single message
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
	public int GetSize()
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

public enum TabMessageType
{
	SingleMessage,
	MoreIncoming,
	EndOfMessage
}
