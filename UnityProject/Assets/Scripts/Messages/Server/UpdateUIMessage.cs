using System.Collections;

/// <summary>
///     Message that tells client which UI action to perform
/// </summary>
public class UpdateUIMessage : ServerMessage
{
	public class UpdateUIMessageNetMessage : ActualMessage
	{
		public ControlDisplays.Screens Screen;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as UpdateUIMessageNetMessage;
		if(newMsg == null) return;

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