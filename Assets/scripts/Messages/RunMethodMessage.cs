using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class RunMethodMessage : ServerMessage<RunMethodMessage>
{
    public NetworkInstanceId Recipient;
    public NetworkInstanceId Parameter;

    public override IEnumerator Process()
    {
        Debug.Log(ToString());

        yield return WaitFor(Recipient, Parameter);


    }

    public static RunMethodMessage Send(GameObject recipient, GameObject parameter = null)
    {
//        var msg = new InteractMessage{ Subject = subject.GetComponent<NetworkIdentity>().netId };
//        msg.Send();
//        return msg;
        var msg = new RunMethodMessage{Parameter = parameter ? 
            parameter.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid};
        msg.SendTo(recipient);
        return msg;
    }

    public override string ToString()
    {
        return string.Format("[RunMethodMessage Recipient={0} Type={1}]", Recipient, MessageType);
    }
}
