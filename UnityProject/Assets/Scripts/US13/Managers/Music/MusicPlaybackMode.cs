namespace US13.Managers.Music
{
	/// <summary>
	/// Determines how a music track is spatialised and how it competes with other tracks.
	/// </summary>
	public enum MusicPlaybackMode
	{
		/// <summary>
		/// Played only for the local client (e.g. lobby, personal headphones).
		/// Lower priority than Global.
		/// </summary>
		Local = 0,

		/// <summary>
		/// Played for every connected client (e.g. admin announcements, round-start theme).
		/// Overrides Local tracks when scores compete.
		/// </summary>
		Global = 1,

		/// <summary>
		/// Emitted from a world position (e.g. jukebox, radio). Spatialised per-listener.
		/// Does not compete with Local/Global; runs in parallel via the ambient system.
		/// </summary>
		Diagetic = 2
	}
}
