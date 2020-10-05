using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Player
{
	public class ExaminablePlayer : MonoBehaviour, IExaminable
	{
		private const string LILAC_COLOR = "#b495bf";

		private PlayerScript script;

		private PlayerHealth Health => script.playerHealth;
		private Equipment Equipment => script.Equipment;
		private string VisibleName => script.visibleName;

		private void Awake()
		{
			script = GetComponent<PlayerScript>();
		}

		/// <summary>
		/// This is just a simple initial implementation of IExaminable to health;
		/// can potentially be extended to return more details and let the server
		/// figure out what to pass to the client, based on many parameters such as
		/// role, medical skill (if they get implemented), equipped medical scanners,
		/// etc. In principle takes care of building the string from start to finish,
		/// so logic generating examine text can be completely separate from examine
		/// request or netmessage processing.
		/// </summary>
		public string Examine(Vector3 worldPos = default)
		{
			return $"This is <b>{VisibleName}</b>.\n" +
					$"{Equipment.Examine()}" +
					$"<color={LILAC_COLOR}>{Health.GetExamineText()}</color>";
		}
	}
}
