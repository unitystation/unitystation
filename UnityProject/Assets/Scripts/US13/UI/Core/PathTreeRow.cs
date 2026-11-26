using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace US13.UI.Core
{
	/// <summary>
	/// Style the row's button, background and label on the template or a reusable prefab.
	/// </summary>
	public class PathTreeRow : MonoBehaviour
	{
		[SerializeField] private Button button;
		[SerializeField] private Text label;
		[SerializeField] private Color folderColor = Color.white;
		[SerializeField] private Color leafColor = Color.white;
		[SerializeField] private string collapsedPrefix = "+ ";
		[SerializeField] private string expandedPrefix = "- ";
		[SerializeField] private string leafPrefix = "  ";

		public void Bind(string text, bool folder, bool expanded, UnityAction clicked)
		{
			label.supportRichText = false;
			label.text = (folder ? (expanded ? expandedPrefix : collapsedPrefix) : leafPrefix) + text;
			label.color = folder ? folderColor : leafColor;
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(clicked);
		}
	}
}
