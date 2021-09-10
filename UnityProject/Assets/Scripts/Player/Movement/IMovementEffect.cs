namespace Player.Movement
{
	public interface IMovementEffect
	{
		float RunningSpeedModifier { get; }
		float WalkingSpeedModifier { get; }
		float CrawlingSpeedModifier { get; }
	}
}