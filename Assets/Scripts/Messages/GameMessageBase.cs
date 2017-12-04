using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class GameMessageBase : MessageBase
{
    protected static short msgTypeCounter = 1000;

    public GameObject NetworkObject;
    public GameObject[] NetworkObjects;

    protected IEnumerator WaitFor(NetworkInstanceId id)
    {
        int tries = 0;
        while ((NetworkObject = ClientScene.FindLocalObject(id)) == null)
        {
            if (tries++ > 10)
            {
                Debug.Log("Could not find " + id);
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

    bool AllLoaded(NetworkInstanceId[] ids)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            var obj = ClientScene.FindLocalObject(ids[i]);
            if (obj == null)
                return false;

            NetworkObjects[i] = obj;
        }

        return true;
    }
}
