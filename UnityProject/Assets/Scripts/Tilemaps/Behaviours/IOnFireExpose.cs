namespace Tilemaps.Behaviours
{
	public interface  IOnFireExpose
	{
		void ExposeToFire(FireExposure fireExposure, MetaDataNode data, TileChangeManager tileChangeManager);
	}
}