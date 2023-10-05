using System;
using System.Text;
using HealthV2;
using JetBrains.Annotations;
using Logs;
using Mirror;
using UnityEngine;

namespace Items
{
	public class SuitSensor : NetworkBehaviour, IRightClickable
	{
		public enum SensorMode
		{
			OFF,
			LOCATION,
			VITALS,
			FULL,
		}

		[field: SerializeField, SyncVar] public SensorMode Mode { get; private set; } = SensorMode.VITALS;
		[SerializeField] private Pickupable pickupable;

		private string bruteColor;
		private string burnColor;
		private string toxinColor;
		private string oxylossColor;

		private void Awake()
		{
			pickupable ??= GetComponent<Pickupable>();
			bruteColor = ColorUtility.ToHtmlStringRGB(Color.red);
			burnColor = ColorUtility.ToHtmlStringRGB(Color.yellow);
			toxinColor = ColorUtility.ToHtmlStringRGB(Color.green);
			oxylossColor = ColorUtility.ToHtmlStringRGB(new Color(0.50f, 0.50f, 1));
		}

		private bool IsEquipped()
		{
			if (pickupable.ItemSlot == null) return false;
			return pickupable.ItemSlot.RootPlayer() != null && pickupable.ItemSlot.NamedSlot == NamedSlot.uniform;
		}

		public float OverallHealth(LivingHealthMasterBase health)
		{
			return Mathf.Floor(100 * health.OverallHealth / health.MaxHealth);
		}

		private string HealthStatus(LivingHealthMasterBase health)
		{
			StringBuilder healthReport = new StringBuilder();
			float[] fullDamage = new float[7];
			foreach (var bodypart in health.BodyPartList)
			{
				if ( bodypart.DamageContributesToOverallHealth == false ) continue;
				if ( bodypart.TotalDamage == 0 ) continue;

				for (int i = 0; i < bodypart.Damages.Length; i++)
				{
					fullDamage[i] += bodypart.Damages[i];
				}
			}
			healthReport.Append("<mspace=0.6em>");
			healthReport.AppendLine(
				$"<color=#{bruteColor}><b>{"Brute", -8}</color><color=#{burnColor}>{"Burn", -8}</color>" +
				$"<color=#{toxinColor}>{"Toxin", -8}</color><color=#{oxylossColor}>Oxy</color></b>\n" +
				$"<color=#{bruteColor}>{Mathf.Round(fullDamage[(int)DamageType.Brute]), 16}</color>" +
				$"<color=#{burnColor}>{Mathf.Round(fullDamage[(int)DamageType.Burn]), 4}</color>" +
				$"<color=#{toxinColor}>{Mathf.Round(fullDamage[(int)DamageType.Tox]), 4}</color>" +
				$"<color=#{oxylossColor}>{Mathf.Round(fullDamage[(int)DamageType.Oxy]), 4}</color>"
			);
			healthReport.Append("</mspace>");
			return healthReport.ToString();
		}

		[CanBeNull]
		public string GetInfo()
		{
			if (IsEquipped() == false) return null;
			StringBuilder sensorReport = new StringBuilder();
			RegisterPlayer player = pickupable.ItemSlot.RootPlayer();
			LivingHealthMasterBase health = player.PlayerScript.playerHealth;
			sensorReport.Append($"{player.PlayerScript.playerName}");
			if (Mode == SensorMode.FULL) sensorReport.Append($" - {OverallHealth(health)}%");
			switch (Mode)
			{
				case SensorMode.OFF:
					sensorReport.AppendLine("N/A");
					break;
				case SensorMode.LOCATION:
					sensorReport.AppendLine($"({player.gameObject.AssumedWorldPosServer()})");
					break;
				case SensorMode.VITALS:
					sensorReport.AppendLine($"{HealthStatus(health)}");
					break;
				case SensorMode.FULL:
					sensorReport.AppendLine($"{HealthStatus(health)} ({player.gameObject.AssumedWorldPosServer()})");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return sensorReport.ToString();
		}

		private void SwitchMode()
		{
			switch (Mode)
			{
				case SensorMode.OFF:
					Loggy.Log("You're not supposed to be here, doctor freeman.");
					break;
				case SensorMode.LOCATION:
					Mode = SensorMode.VITALS;
					break;
				case SensorMode.VITALS:
					Mode = SensorMode.FULL;
					break;
				case SensorMode.FULL:
					Mode = SensorMode.VITALS;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			Chat.AddExamineMsg(PlayerManager.LocalPlayerObject, $"Changed sensors to {Mode}");
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			if (Vector3.Distance(PlayerManager.LocalPlayerObject.AssumedWorldPosServer(),
				    gameObject.AssumedWorldPosServer()) > 2.5f)
			{
				//To avoid players remotely accessing these commands.
				return null;
			}

			RightClickableResult result = new RightClickableResult();
			if (Mode is not SensorMode.OFF)
			{
				result.AddElement("Turn off sensor", () => Mode = SensorMode.OFF);
				result.AddElement("Change vitals tracking mode", SwitchMode);
			}
			else
			{
				result.AddElement("Turn on sensor", () => Mode = SensorMode.VITALS);
			}

			if (Input.GetKeyDown(KeyCode.LeftControl))
			{
				result.AddElement("[Debug]", () => Chat.AddExamineMsg(PlayerManager.LocalPlayerObject, $"{GetInfo()}"), Color.red);
			}
			return result;
		}
	}
}