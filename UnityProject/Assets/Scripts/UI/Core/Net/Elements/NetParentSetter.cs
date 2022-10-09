using System.Collections;
using System.Collections.Generic;
using UI.Core.Net.Elements;
using UnityEngine;

public class NetParentSetter : NetUIIntElement
{
	public NetParentSetterTarget.IDTarget IDTarget;
	public Transform TransformToSet;

	public override int Value {
		get => CurrentLocation;
		set
		{
			CurrentLocation = value;
			SetParentInternal(value);
		}
	}

	private List<NetParentSetterTarget> OrderedTargetParents = new List<NetParentSetterTarget>();
	private Dictionary<int, NetParentSetterTarget> DictionaryParents = new Dictionary<int, NetParentSetterTarget>();


	public int CurrentLocation;

	private void SetParentInternal(int ListLocation)
	{
		if (gameObject.activeSelf == false) return;
		if (ListLocation >= OrderedTargetParents.Count)
		{
			Logger.LogError($"{ListLocation} Was out of bounds on {this.name} ");
			return;
		}

		var Target = DictionaryParents[ListLocation];

		if (Target.SetParentBelow)
		{
			TransformToSet.SetParent(Target.transform.GetChild(0), false);
			TransformToSet.localPosition = Vector3.zero;
			return;
		}


		TransformToSet.SetParent(Target.transform, false);
		TransformToSet.localPosition = Vector3.zero;

		//TODO Order on lists
	}

	public void Start()
	{
		if (TransformToSet == null)
		{
			TransformToSet = this.transform;
		}

		var Parent =  GetComponentInParent<NetTab>();

		var targets = Parent.GetComponentsInChildren<NetParentSetterTarget>();

		OrderedTargetParents.Clear();
		foreach (var target in targets)
		{
			if (target.TargetType == IDTarget)
			{
				if (DictionaryParents.ContainsKey(target.intID))
				{
					Logger.LogError($"duplicate ID of {target.intID}  found on {target.name} please give unique ID");
				}

				DictionaryParents[target.intID] = target;
				OrderedTargetParents.Add(target);
			}
		}
	}


	public void SetParentViaID(int ID)
	{
		if (DictionaryParents.ContainsKey(ID))
		{
			SetValueClient(ID);
		}
		else
		{
			Logger.LogError($"ID {ID} not present on {this.name}");
		}

	}

	public bool IsValidParentViaNetParentSetterTarget(NetParentSetterTarget NetParentSetterTarget)
	{
		if (OrderedTargetParents.Contains(NetParentSetterTarget))
		{

			return true;
		}
		else
		{
			return false;
		}
	}


	public void SetParentViaNetParentSetterTarget(NetParentSetterTarget NetParentSetterTarget)
	{
		if (OrderedTargetParents.Contains(NetParentSetterTarget))
		{
			SetValueClient(NetParentSetterTarget.intID);
		}
		else
		{
			Logger.LogError($"NetParentSetterTarget {NetParentSetterTarget.name} not connected to {this.name}");
		}
	}



	public int DEBUG_ID = 0;

	[NaughtyAttributes.Button()]
	public void DEBUG_SET_parent()
	{
		SetParentViaID(DEBUG_ID);
	}

}
