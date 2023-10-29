using System.Collections.Generic;
using Items.Food;
using Logs;
using Scripts.Core.Transform;
using Systems.Explosions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Systems.Faith.Miracles
{
	public class MakeFoodBigger : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Make food bigger";
		[SerializeField] private string faithMiracleDesc = "Bleses (or curses) nearby food to become bigger, allowing for a feast.";
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

		public int MiracleCost { get; set; } = 500;

		public void DoMiracle()
		{
			foreach (var foodGobbler in FaithManager.Instance.FaithLeaders)
			{
				Chat.AddLocalMsgToChat($"{foodGobbler.visibleName}'s eyes become white as they start chanting some words loudly..", foodGobbler.gameObject);
				Chat.AddChatMsgToChatServer(foodGobbler.PlayerInfo, "..Eathem.. Wish-ha-pig..", ChatChannel.Local, Loudness.LOUD);
				GameManager.Instance.StartCoroutine(foodGobbler.GameObject.FindAllComponentsNearestToTarget<Edible>(6, CheckComponents));
			}
		}

		private void CheckComponents(List<Edible> items)
		{
			Loggy.Log($"cawka {items.Count}");
			foreach (var collider in items)
			{
				collider.SetMaxBites(Random.Range(15, 35));
				var randomScale = (int)Random.Range(2, 5);
				if (collider.TryGetComponent<ScaleSync>(out var scale) == false) continue;
				scale.SetScale(new Vector3(randomScale,randomScale, randomScale));
				SparkUtil.TrySpark(collider.gameObject);
			}
		}
	}
}