using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Learning
{
	public class ProtipListEntry : MonoBehaviour
	{
		[SerializeField] private Image tipIcon;
		[SerializeField] private TMP_Text tipText;
		[SerializeField] private TMP_Text tipTitle;


		public void Setup(ProtipSO data)
		{
			tipIcon.sprite = data.TipData.TipIcon;
			tipText.text = data.TipData.Tip;
			tipTitle.text = data.TipTile;
		}
	}
}