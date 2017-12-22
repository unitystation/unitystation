using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Message that tells client what is the current round time
/// </summary>
public class UpdateRoundTimeMessage : ServerMessage<UpdateRoundTimeMessage>
{
    public float Time;
    public NetworkInstanceId Subject;
    public static short MessageType = (short) MessageTypes.UpdateRoundTimeMessage;
    
    public override IEnumerator Process()
    {
        yield return WaitFor(Subject);

        GameManager.Instance.SyncTimendResetCounter(Time);
    }

    public static UpdateRoundTimeMessage Send(float time)
    {
        UpdateRoundTimeMessage msg = new UpdateRoundTimeMessage
        {
            Time = time
        };
        msg.SendToAll();
        return msg;
    }

    public override string ToString()
    {
        return string.Format("[UpdateRoundTimeMessage Subject={0} Type={1} Time={2}]", Subject, MessageType, Time);
    }
}