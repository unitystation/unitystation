using Managers;
using TileManagement;
using UnityEngine;


public abstract class SubsystemBehaviour : MonoBehaviour
	{

		protected MetaDataLayer metaDataLayer;
		protected MetaTileMap metaTileMap;
		protected SubsystemManager subsystemManager;

		public virtual SystemType SubsystemType => SystemType.None;

		[field: SerializeField] public virtual int Priority { get; private set; } = 0;

		public virtual void Awake()
		{
			metaDataLayer = GetComponentInChildren<MetaDataLayer>();
			metaTileMap = GetComponentInChildren<MetaTileMap>();
			subsystemManager = GetComponent<SubsystemManager>();
			subsystemManager.Register(this);
			SubsystemBehaviourQueueInit.Queue(this);
		}

		public abstract void Initialize();

		public abstract void UpdateAt(Vector3Int localPosition);
	}
