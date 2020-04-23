using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GUI_HackingNode : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI label;

	private HackingNode hackNode;
	public HackingNode HackNode => hackNode;


	public void SetHackingNode(HackingNode node)
	{
		hackNode = node;
		SetUpNodeData();
	}

	public void SetUpNodeData()
	{
		label.SetText(hackNode.InternalLabel);
	}
}
