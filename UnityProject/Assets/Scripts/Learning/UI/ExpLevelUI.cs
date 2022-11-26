using TMPro;
using UnityEngine;

namespace Learning
{
	public class ExpLevelUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text levelDesc;

		[SerializeField, TextArea(5, 30)]
		private string NewToSS;
		[SerializeField, TextArea(5, 30)]
		private string NewToUS;
		[SerializeField, TextArea(5, 30)]
		private string ExpPlayer;
		[SerializeField, TextArea(5, 30)]
		private string RobustPlayer;

		public void SetExpLevel(int level)
		{
			ProtipManager.Instance.SetExperienceLevel((ProtipManager.ExperienceLevel)level);
			gameObject.SetActive(false);
		}

		public void SetExpDesc(int level)
		{
			levelDesc.text = level switch
			{
				0 => NewToSS,
				1 => NewToUS,
				2 => ExpPlayer,
				3 => RobustPlayer,
				_ => levelDesc.text
			};
		}
	}
}