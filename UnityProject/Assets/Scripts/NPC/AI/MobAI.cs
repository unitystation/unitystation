using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MobFollow))]
[RequireComponent(typeof(MobExplore))]
[RequireComponent(typeof(MobFlee))]
public class MobAI : MonoBehaviour
{
	protected MobFollow mobFollow;
	protected MobExplore mobExplore;
	protected MobFlee mobFlee;
	protected LivingHealthBehaviour health;
	protected bool isServer;

	protected virtual void Awake()
	{
		mobFollow = GetComponent<MobFollow>();
		mobExplore = GetComponent<MobExplore>();
		mobFlee = GetComponent<MobFlee>();
		health = GetComponent<LivingHealthBehaviour>();
	}

	public virtual void OnEnable()
	{
		//only needed for starting via a map scene through the editor:
		if (CustomNetworkManager.Instance == null) return;

		if (CustomNetworkManager.Instance._isServer)
		{
			UpdateManager.Instance.Add(UpdateMe);
			isServer = true;
		}
	}

	public void OnDisable()
	{
		if (isServer)
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	protected virtual void UpdateMe()
	{

	}
}
