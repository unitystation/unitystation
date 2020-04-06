using System.Collections;

/// <summary>
///     Message that tells client which UI action to perform
/// </summary>
public class UpdateUIMessage : ServerMessage
{
	public ControlDisplays.Screens Screen;

	public override IEnumerator Process()
	{
		UIManager.Display.SetScreenFor(Screen);
		yield return null;
	}

	public static UpdateUIMessage Send(ControlDisplays.Screens screen)
	{
		UpdateUIMessage msg = new UpdateUIMessage
		{
			Screen = screen
		};
		msg.SendToAll();
		return msg;
	}
}