using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

public abstract class GameMessageBase : MessageBase
{
	public GameObject NetworkObject;
	public GameObject[] NetworkObjects;

	public abstract IEnumerator Process();

	protected IEnumerator WaitFor(NetworkInstanceId id)
	{
		if (id.IsEmpty())
		{
			Logger.LogWarning($"{this} tried to wait on an empty (0) id", Category.NetMessage);
			yield break;
		}

		int tries = 0;
		while ((NetworkObject = ClientScene.FindLocalObject(id)) == null)
		{
			if (tries++ > 10)
			{
				Logger.LogWarning($"{this} could not find object with id {id}", Category.NetMessage);
				yield break;
			}

			yield return YieldHelper.EndOfFrame;
		}
	}

	protected short GetMessageType()
	{
		const BindingFlags FLAGS = BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public;
		FieldInfo field = this.GetType().GetField("MessageType", FLAGS);
		return (short) field.GetValue(null);
	}

	protected IEnumerator WaitFor(params NetworkInstanceId[] ids)
	{
		NetworkObjects = new GameObject[ids.Length];

		while (!AllLoaded(ids))
		{
			yield return YieldHelper.EndOfFrame;
		}
	}

	private bool AllLoaded(NetworkInstanceId[] ids)
	{
		for (int i = 0; i < ids.Length; i++)
		{
			var netId = ids[i];
			if ( netId == NetworkInstanceId.Invalid ) {
				continue;
			}
			GameObject obj = ClientScene.FindLocalObject(netId);
			if (obj == null)
			{
				return false;
			}

			NetworkObjects[i] = obj;
		}

		return true;
	}
}
