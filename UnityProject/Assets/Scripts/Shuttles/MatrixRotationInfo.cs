
/// <summary>
/// Encapsulates information about a matrix rotation event
/// </summary>
public class MatrixRotationInfo
{
	/// <summary>
	/// Matrix move in which this rotation is occurring.
	/// </summary>
	public readonly MatrixMove MatrixMove;

	/// <summary>
	/// How much we are rotating from the current orientation of the matrix.
	/// </summary>
	public readonly RotationOffset RotationOffset;

	/// <summary>
	/// Whether this is for client side or server side rotation logic.
	/// </summary>
	public readonly NetworkSide NetworkSide;

	/// <summary>
	/// Is this the start of a rotation?
	/// </summary>
	public readonly bool IsStart;

	/// <summary>
	/// Is this the end of a rotation?
	/// </summary>
	public bool IsEnd => !IsStart;

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
	public RotationOffset RotationOffsetFromInitial => MatrixMove.ClientState.FacingOffsetFromInitial(MatrixMove);

	public MatrixRotationInfo(MatrixMove matrixMove, RotationOffset rotationOffset, NetworkSide networkSide, bool isStart)
	{
		MatrixMove = matrixMove;
		RotationOffset = rotationOffset;
		NetworkSide = networkSide;
		IsStart = isStart;
	}
}
