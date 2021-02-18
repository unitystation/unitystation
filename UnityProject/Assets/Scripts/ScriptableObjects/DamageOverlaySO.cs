using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "DamageOverlays", menuName = "ScriptableObjects/DamageOverlays", order = 0)]
	public class DamageOverlaySO : ScriptableObject
	{
		[SerializeField]
		[Tooltip("Put them in damage % DECREASING order, eg 99% (nearly removed), 50%, 25%")]
		private List<DamageOverlayData> damageOverlays = new List<DamageOverlayData>();

		public List<DamageOverlayData> DamageOverlays => damageOverlays;
	}

	[Serializable]
	public class DamageOverlayData
	{
		public OverlayTile overlayTile;

		//Above what damage % should this overlay happen
		[Range(0, 1f)]
		public float damagePercentage;
	}
}
