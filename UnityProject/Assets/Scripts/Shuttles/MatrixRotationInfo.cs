
/// <summary>
/// Encapsulates information about a matrix rotation event. This can be one of 3 kinds -
/// starting rotation, ending rotation, or the object is being newly registered as being on a matrix.
/// </summary>
public class MatrixRotationInfo
{

}

public enum RotationEvent {
	/// <summary>
	/// Rotation is beginning
	/// </summary>
	Start,
	/// <summary>
	/// Rotation is endinge
	/// </summary>
	End,
	/// <summary>
	/// Object became registered as being on a new matrix and is recieving the matrix's initial
	/// rotation status.
	/// </summary>
	Register

}
