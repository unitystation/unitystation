using US13.ScriptableObjects.Audio;
using US13.Tilemaps.Behaviours.Meta;

namespace US13.Map.General
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