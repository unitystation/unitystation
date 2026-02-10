using US13.Tilemaps.Behaviours.Meta;

namespace US13.Shuttles
{
	public class AIShouldNavigateAround : ItemMatrixSystemInit
	{
		public bool ShouldNavigateAround = true;

		public override void Start()
		{
			base.Start();
			metaTileMap.matrix.AIShuttleShouldAvoid = ShouldNavigateAround;
		}

	}
}
