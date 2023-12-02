using Objects.Lighting;

namespace Core.Lighting
{
	public interface ILightAnimation
	{
		public bool AnimationActive { get; set; }

		public SpriteHandler SpriteHandler { get; protected set; }
		public LightSource Source { get; protected set; }
		public int ID { get; set; }
		public void AnimateLight();
		public void StopAnimation();
		public void StartAnimation();
	}
}