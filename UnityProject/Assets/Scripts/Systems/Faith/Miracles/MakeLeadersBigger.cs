﻿using Scripts.Core.Transform;
using UnityEngine;

namespace Systems.Faith.Miracles
{
	public class MakeLeadersBigger : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Enlarge Leaders";
		[SerializeField] private string faithMiracleDesc = "Makes faith leaders bigger physically.";
		[SerializeField] private SpriteDataSO miracleIcon;

		string IFaithMiracle.FaithMiracleName
		{
			get => faithMiracleName;
			set => faithMiracleName = value;
		}

		string IFaithMiracle.FaithMiracleDesc
		{
			get => faithMiracleDesc;
			set => faithMiracleDesc = value;
		}

		SpriteDataSO IFaithMiracle.MiracleIcon
		{
			get => miracleIcon;
			set => miracleIcon = value;
		}

		public int MiracleCost { get; set; } = 100;
		public void DoMiracle()
		{
			foreach (var leader in FaithManager.Instance.FaithLeaders)
			{
				if (leader.TryGetComponent<ScaleSync>(out var scale) == false) continue;
				int scaleNew = Random.Range(2, 4);
				scale.SetScale(new Vector3(scaleNew,scaleNew,scaleNew));
				Chat.AddLocalMsgToChat($"{leader.visibleName} inflates and becomes bigger", leader.GameObject);
			}
		}
	}
}