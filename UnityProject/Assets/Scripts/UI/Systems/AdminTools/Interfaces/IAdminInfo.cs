/// <summary>
/// Used to provide information to the AdminOverlay canvas
/// Add the interface and prepare the data as a string with new lines
/// The info panel can only show 3 lines of text with 20 chars each
/// </summary>
public interface IAdminInfo
{
	/// <summary>
	/// Called serverside when gathering the display text to be sent to each admin client
	/// </summary>
	string AdminInfoString();
}
