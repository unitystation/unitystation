using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class HUDHandler : MonoBehaviour
{

	public static Dictionary<Type, List<IHUD>> Categorys = new Dictionary<Type, List<IHUD>>();

	public static Dictionary<Type, bool> CategoryEnabled = new Dictionary<Type, bool>();

	public GameObject HudContainer;

	public List<IHUD> Huds = new List<IHUD>();


	public void AddNewHud(IHUD HUD)
	{
		if (Huds.Contains(HUD)) return;
		var InstantiatedGameObject = Instantiate(HUD.Prefab, HudContainer.transform);
		InstantiatedGameObject.transform.localPosition = Vector3.zero;
		Huds.Add(HUD);
		HUD.InstantiatedGameObject = InstantiatedGameObject;
		HUD.SetUp();

		var Type = HUD.GetType();
		if (Categorys.ContainsKey(Type) == false)
		{
			Categorys[Type] = new List<IHUD>();
		}
		Categorys[Type].Add(HUD);
	}

	public void RemoveHud(IHUD HUD)
	{
		if (Huds.Contains(HUD) == false) return;
		Huds.Remove(HUD);
		Destroy( HUD.InstantiatedGameObject);
		var type = HUD.GetType();
		if (Categorys.ContainsKey(type))
		{
			Categorys[type].Remove(HUD);
		}
	}


}
