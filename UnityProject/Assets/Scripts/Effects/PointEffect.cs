using UnityEngine;
using System.Collections;

public class PointEffect : CustomEffectBehaviour
{
	private float travelTime = 0.45f;
	private Coroutine slowDown;
	public override void RunEffect(Vector2 target)
	{
		particleSystem.Clear();
		this.TryStopCoroutine(ref slowDown);

		Vector2 pos = transform.position;

		var particleMain = particleSystem.main;
		particleMain.startRotation = Mathf.Atan2(pos.x - target.x, pos.y - target.y)
			;
		Vector2 difference = new Vector2(target.x - pos.x, target.y - pos.y);
		difference.x /= travelTime;
		difference.y /= travelTime;
		SetSpeed(difference);

		this.StartCoroutine(SlowDown(difference), ref slowDown);
		particleSystem.Play();
	}

	private IEnumerator SlowDown(Vector2 speed)
	{
		yield return new WaitForSeconds(travelTime);
		speed = Vector3.Normalize(speed) * 10; //reasonable speed for arrow bouncing
		SetSpeed(speed);
	}

	void SetSpeed(Vector2 newSpeed)
	{
		var velocityOverLifetime = particleSystem.velocityOverLifetime;

		var curveX = velocityOverLifetime.x;
		curveX.curveMultiplier = newSpeed.x;
		velocityOverLifetime.x = curveX;

		var curveY = velocityOverLifetime.y;
		curveY.curveMultiplier = newSpeed.y;
		velocityOverLifetime.y = curveY;
	}
}