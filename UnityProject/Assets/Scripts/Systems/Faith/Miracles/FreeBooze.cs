using System.Collections.Generic;
using UnityEngine;
using Util.Independent.FluentRichText;
using Color = Util.Independent.FluentRichText.Color;

namespace Systems.Faith.Miracles
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

		public void DoMiracle()
		{
			string msg = new RichText().Color(Color.Yellow).Italic().Add("You hear bottles of glass fall..");
			Chat.AddGameWideSystemMsgToChat(msg);
			foreach (var dong in PlayerList.Instance.GetAlivePlayers())
			{
				Spawn.ServerPrefab(boozeItemsToSpawn.PickRandom(), dong.GameObject.AssumedWorldPosServer(), null, count: Random.Range(1,3), scatterRadius: 30f);
			}
		}
	}
}