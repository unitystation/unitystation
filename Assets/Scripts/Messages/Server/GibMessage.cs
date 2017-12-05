using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GibMessage : ServerMessage<GibMessage>
{
    public NetworkInstanceId Subject;

    public override IEnumerator Process()
    {
        Debug.Log(ToString());

        yield return WaitFor(Subject);

        foreach (var living in Object.FindObjectsOfType<HealthBehaviour>())
        {
            living.Death();
        }
    }

    public static GibMessage Send()
    {
        var msg = new GibMessage();
        msg.SendToAll();
        return msg;
    }

    public override string ToString()
    {
        return string.Format("[GibMessage Subject={0} Type={1}]", Subject, MessageType);
    }
}
