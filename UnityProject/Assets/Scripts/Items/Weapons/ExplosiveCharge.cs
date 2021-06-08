using UnityEngine;
using Systems.Explosions;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using AddressableReferences;
using Objects;
using System;
using System.Linq;

namespace Items.Weapons
{
	[RequireComponent(typeof(RadioReceiver))]
    public class ExplosiveCharge : NetworkBehaviour
	{
		[NonSerialized] public int timer = 10;
		private bool detonated = false;
		[NonSerialized] public bool freq = false;
		[NonSerialized] public bool armed = false;
		[SerializeField] private ExplosionComponent explosionPrefab;
		private RegisterItem registerItem;
		private ObjectBehaviour objectBehaviour;
		private RadioReceiver radioReceiver;
		public int frequencyReceive{get {return radioReceiver.frequency; } set {radioReceiver.frequency = value;}}


		private void Start()
		{
			registerItem = GetComponent<RegisterItem>();
			objectBehaviour = GetComponent<ObjectBehaviour>();
			radioReceiver = GetComponent<RadioReceiver>();
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Add(ServerUpdateTimer, 1f);
			}
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerUpdateTimer);
			}
		}

		public void ServerUpdateTimer()
		{
			if (freq == false && armed == true && detonated == false)
			{
				if (timer > 0)
				{
					timer--;
				}
				else
				{
					Detonate();
				}
			}
		}

		public void UpdateFreq(int freq)
		{
			frequencyReceive = freq;
		}

		public void ServerFreqDet()
		{
			if (freq == true && armed == true && detonated == false)
			{
				Detonate();
			}
		}

		public void Detonate()
		{
			if (detonated)
			{
				return;
			}
			detonated = true;

			if (isServer)
			{
				var explosionMatrix = registerItem.Matrix;
				var worldPos = objectBehaviour.AssumedWorldPositionServer();

				ClosetControl closetControl = null;
				if ((objectBehaviour.parentContainer != null) && (objectBehaviour.parentContainer.TryGetComponent(out closetControl)))
				{
					closetControl.ServerHeldItems.Remove(objectBehaviour);
				}

				_ = Despawn.ServerSingle(gameObject);

				var explosionGO = Instantiate(explosionPrefab, explosionMatrix.transform);
				explosionGO.transform.position = worldPos;
				explosionGO.Explode(explosionMatrix);
			}
		}
    }
}