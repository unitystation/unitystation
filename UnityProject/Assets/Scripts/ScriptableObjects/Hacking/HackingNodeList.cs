﻿using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.Hacking
{
	[System.Serializable]
	public class HackingNodeInfo
	{
		[Tooltip("This is used by the code to identifiy this kind of node, and will be used by scripts to initialise the links. Make sure this is unique!")]
		public HackingIdentifier InternalIdentifier = HackingIdentifier.Unset;
		public bool IsInput = false;
		public bool IsOutput = false;
		public bool IsDeviceNode = false;

		[Tooltip("Displayed on the blueprints for this type of device.")]
		public string HiddenLabel = "";

		[Tooltip("What label the players see when they view the hacking UI.")]
		public string PublicLabel = "";
	}

	[CreateAssetMenu(fileName = "HackingNodeInfo", menuName = "ScriptableObjects/HackingNodeInfo", order = 1)]
	public class HackingNodeList : ScriptableObject
	{
		public List<HackingNodeInfo> nodeInfoList;
	}
}