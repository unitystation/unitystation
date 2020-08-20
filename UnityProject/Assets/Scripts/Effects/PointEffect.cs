using UnityEngine;

public class PointEffect : CustomEffectBehaviour
{

	public override void RunEffect(Vector2 target)
	{
		particleSystem.Clear();

		Vector2 pos = transform.position;
		Vector2 difference = new Vector2(target.x - pos.x, target.y - pos.y);

		var particleMain = particleSystem.main;
		particleMain.startRotation = Mathf.Atan2(pos.x - target.x, pos.y - target.y);
		var velocityOverLifetime = particleSystem.velocityOverLifetime;

		var curveX = velocityOverLifetime.x;
		curveX.curveMultiplier = difference.x/0.45f;
		velocityOverLifetime.x = curveX;

		var curveY = velocityOverLifetime.y;
		curveY.curveMultiplier = difference.y/0.45f;
		velocityOverLifetime.y = curveY;

		particleSystem.Play();
	}
}