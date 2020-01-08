
/// <summary>
/// Encapsulates information about a matrix rotation event. This can be one of 3 kinds -
/// starting rotation, ending rotation, or the object is being newly registered as being on a matrix.
/// </summary>
public class MatrixRotationInfo
{
	/// <summary>
	/// Matrix move in which this rotation is occurring.
	/// </summary>
	public readonly MatrixMove MatrixMove;

	/// <summary>
	/// How much we are rotating from the current orientation of the matrix.
	/// When RotationEvent.Register, this indicates the offset from the matrix's initially
	/// mapped rotation.
	/// </summary>
	public readonly RotationOffset RotationOffset;

	/// <summary>
	/// Whether this is for client side or server side rotation logic.
	/// </summary>
	public readonly NetworkSide NetworkSide;

	/// <summary>
	/// What kind of rotation event is this?
	/// </summary>
	public readonly RotationEvent RotationEvent;

	/// <summary>
	/// Is this for the start of rotation?
	/// </summary>
	public bool IsStarting => RotationEvent == RotationEvent.Start;
	/// <summary>
	/// Is this for the end of rotation?
	/// </summary>
	public bool IsEnding => RotationEvent == RotationEvent.End;
	/// <summary>
	/// Is this for when the object is being registered as being on a new matrix?
	/// </summary>
	public bool IsObjectBeingRegistered => RotationEvent == RotationEvent.Register;

	/// <summary>
	/// Is this for client side rotation logic?
	/// </summary>
	public bool IsClientside => NetworkSide == NetworkSide.Client;
	/// <summary>
	/// Is this for server side rotation logic?
	/// </summary>
	public bool IsServerside => !IsClientside;

	/// <summary>
	/// Offset from the matrix's initially mapped facing. For things which depend on their local rotation within
	/// the matrix rather than their absolute orientation.
	/// </summary>
	public RotationOffset RotationOffsetFromInitial => MatrixMove.FacingOffsetFromInitial;

	public MatrixRotationInfo(MatrixMove matrixMove, RotationOffset rotationOffset, NetworkSide networkSide, RotationEvent rotationEvent)
	{
		MatrixMove = matrixMove;
		RotationOffset = rotationOffset;
		NetworkSide = networkSide;
		RotationEvent = rotationEvent;
	}

	/// <summary>
	/// Matrix rotation info where a rotation is performed from the matrixmove's initial facing to its current facing.
	/// </summary>
	/// <returns></returns>
	public static MatrixRotationInfo FromInitialRotation(MatrixMove matrixMove, NetworkSide side, RotationEvent rotationEvent)
	{
		return new MatrixRotationInfo(matrixMove, matrixMove.FacingOffsetFromInitial, side, rotationEvent);
	}
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
