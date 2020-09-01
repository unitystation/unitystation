using UnityEngine;


public abstract class SubsystemBehaviour : MonoBehaviour
	{

		protected MetaDataLayer metaDataLayer;
		protected MetaTileMap metaTileMap;
		protected SubsystemManager subsystemManager;

		public virtual SystemType SubsystemType => SystemType.None;

		public virtual int Priority => 0;

		public virtual void Awake()
		{
			metaDataLayer = GetComponentInChildren<MetaDataLayer>();
			metaTileMap = GetComponentInChildren<MetaTileMap>();
			subsystemManager = GetComponent<SubsystemManager>();
			subsystemManager.Register(this);
		}

		public abstract void Initialize();

		public abstract void UpdateAt(Vector3Int localPosition);
	}
