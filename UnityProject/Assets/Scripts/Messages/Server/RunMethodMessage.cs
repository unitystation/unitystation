using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Message that tells clent to run some method
/// </summary>
public class RunMethodMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.RunMethodMessage;
	public string Method;
	public NetworkInstanceId Parameter;
	public NetworkInstanceId Recipient;

	public override IEnumerator Process()
	{
		//To be run on client
		//        Debug.Log("Processed " + ToString());

		yield return WaitFor(Recipient, Parameter);

		NetworkObjects[0].BroadcastMessage(Method, NetworkObjects[1]);
	}

	public static RunMethodMessage Send(GameObject recipient, string method, GameObject parameter = null)
	{
		RunMethodMessage msg = new RunMethodMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId, //?
			Method = method,
			Parameter = parameter != null
				? parameter.GetComponent<NetworkIdentity>().netId
				: NetworkInstanceId.Invalid
		};
		msg.SendTo(recipient);
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[RunMethodMessage Recipient={0} Method={2} Parameter={3} Type={1}]",
			Recipient, MessageType, Method, Parameter);
	}
}