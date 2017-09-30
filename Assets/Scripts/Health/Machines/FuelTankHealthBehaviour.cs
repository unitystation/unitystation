
    using System.Collections;
    using UnityEngine;

public class FuelTankHealthBehaviour : HealthBehaviour
    {
        private PushPull pushPull;

        private void Awake()
        {
            pushPull = GetComponent<PushPull>();

        }

        protected override void OnDeathActions()
        {
            pushPull.BreakPull();
            var delay = 0f;
            switch ( LastDamageType )
            {
                case DamageType.BRUTE:
                    delay = 0.1f; break;
                case DamageType.BURN:
                    delay = Random.Range(0.2f,2f); break; //surprise
            }
            StartCoroutine(explodeWithDelay(delay, LastDamagedBy));

//            Debug.Log("FuelTank ded!");

        }

        private IEnumerator explodeWithDelay(float delay, string damagedBy)
        {
            yield return new WaitForSeconds(delay);
            GetComponentInParent<ExplodeWhenShot>().ExplodeOnDamage(damagedBy);
            yield return null;
        }
    }