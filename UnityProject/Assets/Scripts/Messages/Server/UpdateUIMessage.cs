using System.Collections;
using Mirror;

/// <summary>
///     Message that tells client which UI action to perform
/// </summary>
public class UpdateUIMessage : ServerMessage
{
	public struct UpdateUIMessageNetMessage : NetworkMessage
	{
		public ControlDisplays.Screens Screen;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public UpdateUIMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as UpdateUIMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		UIManager.Display.SetScreenFor(newMsg.Screen);
	}

	public static UpdateUIMessageNetMessage Send(ControlDisplays.Screens screen)
	{
		UpdateUIMessageNetMessage msg = new UpdateUIMessageNetMessage
		{
			Screen = screen
		};
		new UpdateUIMessage().SendToAll(msg);
		return msg;
	}
}