using System.Collections.Generic;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Player;
using US13.Systems.Explosions;
using Util;
using Util.Independent.FluentRichText;

namespace US13.Systems.Faith.Miracles
{
	public class FreeBooze : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Free Booze!";
		[SerializeField] private string faithMiracleDesc = "The god(s) gift booze to the world.";
		[SerializeField] private SpriteDataSO miracleIcon;

		[SerializeField] private List<GameObject> boozeItemsToSpawn = new List<GameObject>();

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

		public int MiracleCost { get; set; } = 150;

		public void DoMiracle(FaithData associatedFaith, PlayerScript invoker = null)
		{
			string msg = new RichText().Color(RichTextColor.Yellow).Italic().Add("You hear bottles of glass fall..");
			Chat.AddGameWideSystemMsgToChat(msg);
			foreach (var dong in PlayerList.Instance.GetAlivePlayers())
			{
				Spawn.ServerPrefab(boozeItemsToSpawn.PickRandom(), dong.GameObject.AssumedWorldPosServer(), count: Random.Range(1,3), scatterRadius: 0.75f);
				SparkUtil.TrySpark(dong.GameObject.AssumedWorldPosServer());
			}
		}
	}
}