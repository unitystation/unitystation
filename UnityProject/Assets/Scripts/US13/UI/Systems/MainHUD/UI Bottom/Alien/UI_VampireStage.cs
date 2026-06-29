using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
using TMPro;
using UnityEngine.EventSystems;
using US13.Actions;
using US13.Clothing.Eyewear;
using US13.UI.Core.Net.Elements;
using US13.UI.Systems;
using US13.UI.Systems.Tooltips.HoverTooltips;
using Util;
using Random = System.Random;

namespace US13.UI.Core
{
	public class UI_VampireStage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IHoverTooltip
	{
		[SerializeField] private List<Color> stageColors;
		[SerializeField] private US13.Clothing.Eyewear.ProgressBar progressBar;

		[SerializeField] private List<Sprite> sprites;

		[SerializeField]
		private Image
			currentStageImage; //We don't actually need to net this as its only every called client side... what fun!

		[SerializeField] private Image nextStageImage;
		[SerializeField] private Image barBackgroundImage;
		[SerializeField] private Image backgroundImage;
		[SerializeField] private TextMeshProUGUI stageLabel;

		[SerializeField] private UI_HoverTooltip hoverTooltip;
		private string toolTip;


		public void UpdateHud(int currentStage, float currentCorruption, float minForStage, float desiredCorruption)
		{
			progressBar.SetGradient(FetchGradientForStage(currentStage));
			progressBar.UpdateValue((currentCorruption - minForStage) / (desiredCorruption - minForStage), true);
			currentStageImage.sprite = sprites[currentStage];
			nextStageImage.sprite = sprites[currentStage + 1];

			toolTip = $"{GetRomanNumerals(currentStage)}: {(int)Math.Round(currentCorruption)}/{(int)Math.Round(desiredCorruption)}u";
			stageLabel.text = toolTip;
		}

		public void SetVisible(bool visible)
		{
			progressBar.SetVisible(visible);

			stageLabel.SetActive(visible);
			barBackgroundImage.SetActive(visible);
			backgroundImage.SetActive(visible);
			nextStageImage.SetActive(visible);
			currentStageImage.SetActive(visible);
		}

		private Gradient FetchGradientForStage(int stageIndex)
		{
			Gradient gradient = new Gradient();
			var colors = new GradientColorKey[2];
			colors[0] = new GradientColorKey(stageColors[stageIndex], 0.0f);
			colors[1] = new GradientColorKey(stageColors[stageIndex + 1], 1.0f);

			var alphas = new GradientAlphaKey[2];
			alphas[0] = new GradientAlphaKey(1.0f, 0.0f);
			alphas[1] = new GradientAlphaKey(1.0f, 1.0f);

			gradient.SetKeys(colors, alphas);
			return gradient;
		}

		private string GetRomanNumerals(int num)
		{
			switch (num)
			{
				case 1:
					return "I";
				case 2:
					return "II";
				case 3:
					return "III";
				case 4:
					return "IV";
				case 5:
					return "V";
				default:
					return "0";
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			UIManager.SetHoverToolTip = gameObject;
			UIManager.SetToolTip = toolTip;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			UIManager.SetToolTip = "";
			UIManager.SetHoverToolTip = null;
		}

		public string HoverTip()
		{
			return $"Stage {toolTip}\nUse your vampire abilities to gain corruption and new powers!";
		}

		public string CustomTitle()
		{
			return "Unholy Blood";
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			return null;
		}
	}
}
