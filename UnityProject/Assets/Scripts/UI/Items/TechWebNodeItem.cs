using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Research;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace UI.Items
{
	public class TechWebNodeItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public Technology techData;

		private UnityEvent onMouseHover;
		private UnityEvent onMouseLoseFocus;

		[SerializeField] private GameObject background;
		[SerializeField] private GameObject dataImage;
		[SerializeField] private TMP_Text nodeTitle;
		[SerializeField] private TMP_Text description;
		[SerializeField] private TMP_Text costText;

		private List<LineRenderer> lineRenderers = new List<LineRenderer>();

		private void OnEnable()
		{
			onMouseHover.AddListener(ShowInfo);
			onMouseLoseFocus.AddListener(HideInfo);
		}

		private void OnDisable()
		{
			onMouseHover.RemoveListener(ShowInfo);
			onMouseLoseFocus.RemoveListener(HideInfo);
		}

		public void Setup(Technology technology)
		{
			techData = technology;
			nodeTitle.text = technology.DisplayName;
			//description.text = technology.Description;
			costText.text = technology.ResearchCosts.ToString();
			
		}

		public void DrawConnectionLines(Vector3 pos)
		{
			LineRenderer line = new LineRenderer();
			line.startColor = Color.white;
			line.endColor = Color.green;
			Vector3[] connectionPoints = new Vector3[1];
			connectionPoints[0] = dataImage.transform.position;
			connectionPoints[1] = pos;
			line.SetPositions(connectionPoints);
			lineRenderers.Add(line);
		}

		private void ShowInfo()
		{
			background.LeanAlpha(1, 0.2f);
			foreach (var line in lineRenderers)
			{
				//I can't find a way to adjust the alpha so im just going to overide the entire material's color for now
				line.material.color = Color.white;
			}
		}
		private void HideInfo()
		{
			background.LeanAlpha(0, 0.2f);
			foreach (var line in lineRenderers)
			{
				line.material.color = Color.black;
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			onMouseHover.Invoke();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			onMouseLoseFocus.Invoke();
		}
	}
}
