using System;
using System.Collections;
using System.Collections.Generic;
using TileManagement;
using UnityEngine;

public abstract class ItemMatrixSystemInit : MonoBehaviour, IInitialiseSystem
{

	public virtual int Priority => 0;

	public virtual void Initialize() { }

	public MetaTileMap MetaTileMap;

	public MatrixSystemManager subsystemManager;

	public void Awake()
	{
		MetaTileMap = GetComponentInParent<MetaTileMap>();

		subsystemManager = GetComponentInParent<MatrixSystemManager>();
		subsystemManager.Register(this);
	}

	public virtual void OnDestroy()
	{
		MetaTileMap = null;
	}
}
