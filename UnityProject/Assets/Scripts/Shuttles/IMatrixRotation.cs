
/// <summary>
/// Implement this on a component to fire logic when matrix rotation starts and ends. This is invoked
/// on the server and client (and both when it's server player's game)
/// </summary>
public interface IMatrixRotation
{
	/// <summary>
	/// Invoked when matrix rotation starts or ends, on client and server side (and both when it's
	/// server player's game).
	/// Use rotationInfo.NetworkSide to distinguish between client / server logic.
	/// Use rotationInfo.IsStart or IsEnd to distinguish between start/ end of rotation.
	/// </summary>
	void OnMatrixRotate(MatrixRotationInfo rotationInfo);
}
