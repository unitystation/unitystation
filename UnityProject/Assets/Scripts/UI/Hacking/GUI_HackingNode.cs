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

	public GUI_Hacking parentHackingPanel;

	public void Start()
	{
		parentHackingPanel = GetComponentInParent<GUI_Hacking>();
	}

	public void SetHackingNode(HackingNode node)
	{
		hackNode = node;
		SetUpNodeData();
	}

	public void SetUpNodeData()
	{
		//label.SetText(hackNode.HiddenLabel);
	}

	public void OnClick()
	{
		if (parentHackingPanel.IsAddingWire)
		{
			if (hackNode.IsInput)
			{
				parentHackingPanel.FinishAddingWire(this);
			}
		}
		else
		{
			if (hackNode.IsOutput)
			{
				parentHackingPanel.BeginAddingWire(this);
			}
		}
	}
}
