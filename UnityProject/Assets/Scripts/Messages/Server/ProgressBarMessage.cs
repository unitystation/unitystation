using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update the progress bar for crafting
/// </summary>
public class ProgressBarMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.ProgressBarMessage;

	public NetworkInstanceId Recipient;
	public int SpriteIndex;
	public Vector3 Position;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient);
		UIManager.ProgressBar.ClientUpdateProgress(Position, SpriteIndex);
	}

	public static ProgressBarMessage Send(GameObject recipient, int spriteIndex, Vector3 pos)
	{
		ProgressBarMessage msg = new ProgressBarMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				SpriteIndex = spriteIndex,
				Position = pos
		};
		msg.SendTo(recipient);
		return msg;
	}
}