using UnityEngine;

public static class LeanTweenAnimations
{
	public static void DeEffectClient(string EffectName, GameObject TargetObject, GameObject Player )
	{
		switch (EffectName)
		{
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
						.setEase(LeanTweenType.easeInOutQuad);
				}

// Return cleanly
				LeanTween.moveLocal(TargetObject, originalLocalPos, 0.08f)
					.setDelay(shakes * step);
				break;
		}
	}

}
