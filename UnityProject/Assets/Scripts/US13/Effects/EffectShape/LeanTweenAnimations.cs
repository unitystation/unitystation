using Logs;
using UnityEngine;

public enum LeanTweenAnimationsEnum
{
	TransparencyFade,
	InteractionShake,
	HitDustSplatter

}

public static class LeanTweenAnimations
{



	public static void DeEffectClient(LeanTweenAnimationsEnum EffectName, GameObject TargetObject, GameObject Player,
		LeanTweenType? useLeanTweenType = null, float? Duration = null)

	{
		DeEffectClient(EffectName.ToString(),  TargetObject, Player, useLeanTweenType, Duration);
	}

	public static void DeEffectClient(string EffectName, GameObject TargetObject, GameObject Player,
		LeanTweenType? useLeanTweenType = null, float? Duration = null)

	{
		switch (EffectName)
		{
			case "TransparencyFade":
				SpriteRenderer[] spriteRenderers = TargetObject.GetComponentsInChildren<SpriteRenderer>();

				LeanTween.value(TargetObject, 1f, 0f, Duration ?? 1)
					.setOnUpdate((float val) =>
					{
						foreach (SpriteRenderer sr in spriteRenderers)
						{
							Color c = sr.color;
							c.a = val;
							sr.color = c;
						}
					})
					.setEase(useLeanTweenType ?? LeanTweenType.easeInOutQuad);
				break;
			case "InteractionShake":
				LeanTween.cancel(TargetObject);
				var originalLocalPos = TargetObject.transform.localPosition;

				float amp = 0.015f;
				float step = 0.06f;
				int shakes = 2;

				// Direction from player to target (world space, then to local)
				Vector3 worldDir = (TargetObject.transform.position - Player.transform.position).normalized;
				Vector3 localDir = TargetObject.transform.parent != null
					? TargetObject.transform.parent.InverseTransformDirection(worldDir)
					: worldDir;

				for (int i = 0; i < shakes; i++)
				{
					// Alternate push/pull along the away-direction
					float sign = (i % 2 == 0) ? 1f : -0.5f;
					Vector3 offset = localDir * (amp * sign);

					LeanTween.moveLocal(TargetObject, originalLocalPos + offset, step)
						.setDelay(i * step)
						.setEase(useLeanTweenType ?? LeanTweenType.easeInOutQuad);
				}

// Return cleanly
				LeanTween.moveLocal(TargetObject, originalLocalPos, 0.08f)
					.setDelay(shakes * step);
				break;
			case "HitDustSplatter":

				float radians = (TargetObject.transform.localRotation.eulerAngles.z +45) * Mathf.Deg2Rad;
				Vector3 localDirection = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * 0.25f;



				Vector3 targetPos = TargetObject.transform.localPosition + localDirection;

				targetPos.z = 0;
				Loggy.Error(targetPos.ToString());

				LeanTween.moveLocal(TargetObject, targetPos, Duration ?? 0.3f).setEase( useLeanTweenType ?? LeanTweenType.easeOutQuad);
				break;
		}
	}

}
