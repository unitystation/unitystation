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
    public class SyndiBomb : NetworkBehaviour
	{
		[NonSerialized] public int timer = 90;
		private bool detonated = false;
		[NonSerialized] public bool freq = false;
		[NonSerialized] public bool armed = false;
		[SerializeField] private ExplosionComponent explosionPrefab;
		private RegisterObject registerObject;
		private ObjectBehaviour objectBehaviour;
		private RadioReceiver radioReceiver;
		public int frequencyReceive{get {return radioReceiver.frequency; } set {radioReceiver.frequency = value;}}


		private void Start()
		{
			objectBehaviour = GetComponent<ObjectBehaviour>();
			registerObject = GetComponent<RegisterObject>();
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
			if (armed == true && detonated == false)
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
			if (armed == false && detonated == false)
			{
				armed = true;
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
				var explosionMatrix = registerObject.Matrix;
				var worldPos = objectBehaviour.AssumedWorldPositionServer();

				_ = Despawn.ServerSingle(gameObject);

				var explosionGO = Instantiate(explosionPrefab, explosionMatrix.transform);
				explosionGO.transform.position = worldPos;
				explosionGO.Explode(explosionMatrix);
			}
		}
    }
}