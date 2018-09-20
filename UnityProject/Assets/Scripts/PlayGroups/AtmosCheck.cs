using System.Collections;
using Atmospherics;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace PlayGroups
{
	public static class AtmosConstants
	{
		public const float HAZARD_HIGH_PRESSURE = 550;
		public const float WARNING_HIGH_PRESSURE = 325;
		public const float WARNING_LOW_PRESSURE = 50;
		public const float HAZARD_LOW_PRESSURE = 20;

		public const float MINIMUM_OXYGEN_PRESSURE = 16;

	}


	public class AtmosCheck : NetworkBehaviour
	{
		private HealthBehaviour health;

		public int pressureDamage = 5;

		public int breathTime = 1;

		private void Awake()
		{
			health = GetComponent<HealthBehaviour>();
		}

		public override void OnStartServer()
		{
			StartCoroutine(Breathe());
		}

		private void Update()
		{
			if (isServer && !health.IsDead)
			{
				CheckPressure();
			}
		}

		[Server]
		private void CheckPressure()
		{
			MetaDataLayer metaDataLayer = MatrixManager.AtPoint(Vector3Int.RoundToInt(transform.position)).MetaDataLayer;

			GasMix atmos = metaDataLayer.Get(transform.localPosition.RoundToInt(), false).Atmos;

			if (atmos.Pressure < AtmosConstants.HAZARD_LOW_PRESSURE || atmos.Pressure > AtmosConstants.HAZARD_HIGH_PRESSURE)
			{
				health.ApplyDamage(null, pressureDamage * Time.deltaTime, DamageType.BRUTE);
			}
		}

		private IEnumerator Breathe()
		{
			while (!health.IsDead)
			{
				MetaDataLayer metaDataLayer = MatrixManager.AtPoint(Vector3Int.RoundToInt(transform.position)).MetaDataLayer;

				GasMix atmos = metaDataLayer.Get(transform.localPosition.RoundToInt(), false).Atmos;
				float partialPressure = atmos.GetPressure(Gas.Oxygen);

				if (partialPressure < AtmosConstants.MINIMUM_OXYGEN_PRESSURE)
				{
					float ratio = 1 - partialPressure / AtmosConstants.MINIMUM_OXYGEN_PRESSURE;

					health.ApplyDamage(null, Mathf.Min(5 * ratio, 3), DamageType.OXY);
				}
				else
				{
					// TODO implement breathing (remove oxygen, add co2, etc.)
				}

				yield return new WaitForSeconds(breathTime);
			}
		}
	}
}