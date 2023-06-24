using Managers;
using TileManagement;
using UnityEngine;


public abstract class SubsystemBehaviour : MonoBehaviour
	{

		public bool Initialized { get; protected set; } = false;
		[field: SerializeField] public virtual int Priority { get; protected set; } = 0;
		[field: SerializeField] public virtual bool RegisteredToLegacySubsystemManager { get; protected set; } = true;
		protected MetaDataLayer metaDataLayer;
		protected MetaTileMap metaTileMap;
		protected SubsystemManager subsystemManager;

		public virtual SystemType SubsystemType => SystemType.None;

		public virtual void Awake()
		{
			metaDataLayer = GetComponentInChildren<MetaDataLayer>();
			metaTileMap = GetComponentInChildren<MetaTileMap>();
			//TODO: Figure out why removing or disabling the old subsystem manager causes all stations to break.
			//BUG: Electrical and atmospherics subsystems break whenever they're moved away from the legacy subsystem manager.
			//BUG: If you put a Chat message while the Initialize() method is running on electrical/atmos subsystems, the fucking subsystem breaks for no reason EVEN IF THE FUCKER IS ALREADY DONE WITH ITS OPERATIONS.
			//(Max): This has been tormenting me since the 4th of January of 2023.
			if (RegisteredToLegacySubsystemManager)
			{
				subsystemManager = GetComponent<SubsystemManager>();
				subsystemManager.Register(this);
			}
			SubsystemBehaviourQueueInit.Queue(this);
		}

		public abstract void Initialize();

		public abstract void UpdateAt(Vector3Int localPosition);
	}
