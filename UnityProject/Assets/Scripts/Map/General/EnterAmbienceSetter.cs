using Audio.Containers;
using TileMap.Behaviours;

namespace Map.General
{
	public class EnterAmbienceSetter : ItemMatrixSystemInit
	{
		public AudioClipsArray EnteringSounds;

		public override void Initialize()
		{
			base.Initialize();
			networkedMatrix.matrix.EnteringSounds = EnteringSounds;
		}
	}
}