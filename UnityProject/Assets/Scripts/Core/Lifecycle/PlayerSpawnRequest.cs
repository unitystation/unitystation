
namespace Player
{
	/// <summary>
	/// A viewer's request to spawn into the game as a new player. Doesn't necessarily guarantee they will actually
	/// spawn with what they requested, depending on game mode!
	/// </summary>
	public class PlayerSpawnRequest
	{
		/// <summary>
		/// The player that is requesting the spawn.
		/// </summary>
		public readonly PlayerInfo Player;

		/// <summary>
		/// Occupation requested to spawn as (won't necessarily be what they get if they
		/// end up spawning as an antag)
		/// </summary>
		public readonly Occupation RequestedOccupation;

		/// <summary>
		/// Character the viewer is attempting to spawn with.
		/// </summary>
		public readonly CharacterSettings CharacterSettings;

		public PlayerSpawnRequest(PlayerInfo player, Occupation requestedOccupation, CharacterSettings character = default)
		{
			Player = player;
			RequestedOccupation = requestedOccupation;
			CharacterSettings = character ?? Player.CharacterSettings;
		}

		public override string ToString()
		{
			return $"Player: {Player.Username}. {nameof(RequestedOccupation)}: {RequestedOccupation}.";
		}
	}
}
