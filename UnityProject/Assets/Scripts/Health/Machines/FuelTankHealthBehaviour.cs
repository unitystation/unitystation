using System.Collections;
using UnityEngine;

public class FuelTankHealthBehaviour : MonoBehaviour
{
	//	private PushPull pushPull;

	//	private void Awake()
	//	{
	//		pushPull = GetComponent<PushPull>();
	//	}

	//FIXME: this class no longer derives from LivingHealthBehaviour as it is not
	// a living thing. A new damage system is required for non living objects

	// 	protected override void OnDeathActions()
	// 	{
	// //		pushPull.BreakPull();
	// 		float delay = 0f;
	// 		switch (LastDamageType)
	// 		{
	// 			case DamageType.Brute:
	// 				delay = 0.1f;
	// 				break;
	// 			case DamageType.Burn:
	// 				delay = Random.Range(0.2f, 2f);
	// 				break; //surprise
	// 		}

	// 		string killer = "God";
	// 		if (LastDamagedBy != null)
	// 		{
	// 			killer = LastDamagedBy.name;
	// 		}
	// 		StartCoroutine(explodeWithDelay(delay, killer));

	// 		//            Logger.Log("FuelTank ded!");
	// 	}

	private IEnumerator explodeWithDelay(float delay, string damagedBy)
	{
		yield return new WaitForSeconds(delay);
		GetComponentInParent<ExplodeWhenShot>().ExplodeOnDamage(damagedBy);
		yield return null;
	}
}