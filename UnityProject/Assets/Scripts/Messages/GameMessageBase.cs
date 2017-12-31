using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public abstract class GameMessageBase : MessageBase
{
	public GameObject NetworkObject;
	public GameObject[] NetworkObjects;

	protected IEnumerator WaitFor(NetworkInstanceId id)
	{
		if (id.IsEmpty()) 
		{
			Debug.LogError($"{this} tried to wait on an empty (0) id");
			yield break;
		}

		int tries = 0;
		while ((NetworkObject = ClientScene.FindLocalObject(id)) == null)
		{
			if (tries++ > 10)
			{
				Debug.LogWarning($"{this} could not find object with id {id}");
				yield break;
			}

			yield return YieldHelper.EndOfFrame;
		}
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
			GameObject obj = ClientScene.FindLocalObject(ids[i]);
			if (obj == null)
			{
				return false;
			}

			NetworkObjects[i] = obj;
		}

		return true;
	}
}
