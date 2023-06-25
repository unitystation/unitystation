using Managers;
using TileManagement;
using UnityEngine;


public abstract class MatrixSystemBehaviour : MonoBehaviour
	{

		//public bool Initialized { get; protected set; } = false;
		[field: SerializeField] public virtual int Priority { get; protected set; } = 0;
		[field: SerializeField] public virtual bool RegisteredToLegacySubsystemManager { get; protected set; } = true;
		protected MetaDataLayer metaDataLayer;
		protected MetaTileMap metaTileMap;
		protected MatrixSystemManager subsystemManager;

		public virtual SystemType SubsystemType => SystemType.None;

		public virtual void Awake()
		{
			metaDataLayer = GetComponentInChildren<MetaDataLayer>();
			metaTileMap = GetComponentInChildren<MetaTileMap>();
			subsystemManager = GetComponent<MatrixSystemManager>();
			subsystemManager.Register(this);
		}

		public abstract void Initialize();

		public abstract void UpdateAt(Vector3Int localPosition);
	}
