
/// <summary>
/// An interface for a class which can perform some operation on a register tile.
/// We use this instead of Action because lambdas create GC and operations on a lot of register tiles
/// tend to need to be performed a lot.
/// </summary>
public interface IRegisterTileAction
{
	void Invoke(RegisterTile registerTile);
}
