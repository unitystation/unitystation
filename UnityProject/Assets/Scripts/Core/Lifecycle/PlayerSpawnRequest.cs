
/// <summary>
/// A viewer's request to spawn into the game as a new player. Doesn't necessarily guarantee they will actually
/// spawn with what they requested, depending on game mode!
/// </summary>
public class PlayerSpawnRequest
{

	/// <summary>
	/// Occupation requested to spawn as (won't necessarily be what they get if they
	/// end up spawning as an antag)
	/// </summary>
	public readonly Occupation RequestedOccupation;

	/// <summary>
	/// JoinedViewer component of the player attempting to spawn.
	/// </summary>
	public readonly JoinedViewer JoinedViewer;

	/// <summary>
	/// Character settings the viewer is attempting to spawn with.
	/// </summary>
	public readonly CharacterSettings CharacterSettings;

	/// <summary>
	/// UserID the viewer is attempting to spawn with.
	/// </summary>
	public readonly string UserID;

	private PlayerSpawnRequest(Occupation requestedOccupation, JoinedViewer joinedViewer, CharacterSettings characterSettings, string userID)
	{
		RequestedOccupation = requestedOccupation;
		JoinedViewer = joinedViewer;
		CharacterSettings = characterSettings;
		UserID = userID;
	}

	/// <summary>
	/// Create a new player spawn info indicating a request to spawn with the
	/// selected occupation and settings.
	/// </summary>
	/// <returns></returns>
	public static PlayerSpawnRequest RequestOccupation(JoinedViewer requestedBy, Occupation requestedOccupation, CharacterSettings characterSettings, string userID)
	{
		return new PlayerSpawnRequest(requestedOccupation, requestedBy, characterSettings, userID);
	}

	public static PlayerSpawnRequest RequestOccupation(ConnectedPlayer requestedBy, Occupation requestedOccupation)
	{
		return new PlayerSpawnRequest(requestedOccupation, requestedBy.ViewerScript, requestedBy.CharacterSettings, requestedBy.UserId);
	}

	public override string ToString()
	{
		return $"{nameof(RequestedOccupation)}: {RequestedOccupation}, {nameof(JoinedViewer)}: {JoinedViewer}, {nameof(CharacterSettings)}: {CharacterSettings}";
	}
}
