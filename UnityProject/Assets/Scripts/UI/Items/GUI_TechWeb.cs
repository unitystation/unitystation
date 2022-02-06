using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Systems.Research;


namespace UI.Items
{
	public class GUI_TechWeb : NetTab
	{
		[SerializeField] private TechWebNodeItem nodePrefab;
		[SerializeField] private GameObject nodes;
		[SerializeField] private TMP_Text pointText;

		private List<TechWebNodeItem> techwebNodes;

		private IEnumerator Start()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			GenerateTechWebNodes();
		}


		private void GenerateTechWebNodes()
		{
			var tech = Techweb.Instance.Data.Technologies;
			Logger.Log(tech.Count.ToString());
			foreach (Technology technology in tech)
			{
				var newNode = Instantiate(nodePrefab, nodes.transform);
				var nodeScript = newNode.GetComponent<TechWebNodeItem>();
				nodeScript.Setup(technology);
				techwebNodes.Add(nodeScript);
			}
			pointText.text = Techweb.Instance.ResearchPoints.ToString();
			UpdateLines();
		}
		private void UpdateLines()
		{
			foreach (var node in techwebNodes)
			{
				if (node.techData.StartingNode == false) continue;
				foreach (var registeredTech in techwebNodes)
				{
					if (registeredTech.techData.RequiredTechnologies.Contains(node.techData.ID) == false) continue;
					node.DrawConnectionLines(registeredTech.transform.position);
				}
			}
		}

		public void HideUI()
		{
			gameObject.SetActive(false);
		}
	}
}

