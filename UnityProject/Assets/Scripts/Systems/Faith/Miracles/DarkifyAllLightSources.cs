using System.Collections;
using System.Collections.Generic;
using Core;
using Objects.Lighting;
using UnityEngine;
using Util.Independent.FluentRichText;

namespace Systems.Faith.Miracles
{
	public class DarkifyAllLightSources : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Darkify all Light Sources";
		[SerializeField] private string faithMiracleDesc = "Turns all light sources into a comfortable warm light.";
		[SerializeField] private SpriteDataSO miracleIcon;

		[SerializeField] private List<Color> lightColors = new List<Color>();

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

		public int MiracleCost { get; set; } = 180;
		public void DoMiracle()
		{
			GameManager.Instance.StartCoroutine(ChangeLights());
			string msg = new RichText().Italic().Color(RichTextColor.Red)
				.Add("An ominous hum is heard from nearby light sources..");
			Chat.AddGameWideSystemMsgToChat(msg);
		}

		private IEnumerator ChangeLights()
		{
			if (MatrixManager.MainStationMatrix?.Objects == null) yield break;

			var currentIndex = 0;
			var maximumIndexes = 20;
			foreach (var stationObject in ComponentsTracker<LightSource>.Instances)
			{
				if (currentIndex >= maximumIndexes)
				{
					currentIndex = 0;
					yield return WaitFor.EndOfFrame;
				}

				stationObject.CurrentOnColor = lightColors.PickRandom();
				currentIndex++;
			}
		}
	}
}