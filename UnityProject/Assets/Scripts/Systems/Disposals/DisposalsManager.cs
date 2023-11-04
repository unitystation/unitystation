using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Logs;
using Objects;
using Objects.Atmospherics;
using Objects.Disposals;
using Shared.Managers;
using Systems.Atmospherics;
using UnityEngine;

namespace Systems.Disposals
{
	/// <summary>
	/// Creates, updates, and removes all disposal instances.
	/// </summary>
	public class DisposalsManager : SingletonManager<DisposalsManager>
	{
		[SerializeField]
		[Tooltip("Set the virtual container prefab to be used in disposal instances.")]
		public GameObject VirtualContainerPrefab;

		[SerializeField]
		[Tooltip("Crawling virtual container prefab")]
		private GameObject crawlingVirtualContainerPrefab = null;
		public GameObject CrawlingVirtualContainerPrefab => crawlingVirtualContainerPrefab;

		[SerializeField]
		[Tooltip("Set how many tiles every disposal instance can traverse in one second.")]
		private float TileTraversalsPerSecond = 20;
		[SerializeField]
		private AddressableAudioSource disposalEjectionHiss = default;

		public AddressableAudioSource DisposalEjectionHiss => disposalEjectionHiss;

		public readonly List<DisposalTraversal> disposalInstances = new List<DisposalTraversal>();

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			// TODO: this is terrible.

			/*
			 * Allow one disposal instance to traverse per update.
			 * This means that in the (unlikely) event of huge number of disposals, performance
			 * shouldn't be considerably affected - disposal instances would just be delayed
			 * once there are enough instances such that there is more than one instance that
			 * is not CurrentlyDelayed in an update cycle.
			 *
			 * Realistically, it is expected no more than, say, five disposal instances would exist at one time,
			 * so there should be no noticeable throttling.
			*/

			foreach (DisposalTraversal disposal in disposalInstances)
			{
				if (disposal.CurrentlyDelayed == false)
				{
					UpdateDisposal(disposal);
					break;
				}
			}
		}

		public static GameObject SpawnVirtualContainer(Vector3Int worldPosition)
		{
			SpawnResult virtualContainerSpawn = Spawn.ServerPrefab(Instance.VirtualContainerPrefab, worldPosition);
			if (virtualContainerSpawn.Successful == false)
			{
				Loggy.LogError(
						"Failed to spawn disposal virtual container! " +
						$"Is {nameof(DisposalsManager)} missing reference to {nameof(Instance.VirtualContainerPrefab)}?",
						Category.Machines);
				return default;
			}

			return virtualContainerSpawn.GameObject;
		}

		/// <summary>
		/// Create a new disposal instance, which will move its contents along the disposal pipe network.
		/// </summary>
		/// <param name="sourceContainer">The container holding the entities to be disposed of.</param>
		public void NewDisposal(GameObject sourceObject)
		{
			var selfControlledOnStart = false;
			// Spawn virtual container
			var disposalContainer = SpawnVirtualContainer(sourceObject.RegisterTile().WorldPositionServer);
			var virtualContainer = disposalContainer.GetComponent<DisposalVirtualContainer>();

			// Transfer contents
			if (sourceObject.TryGetComponent<ObjectContainer>(out var objectContainer))
			{
				objectContainer.TransferObjectsTo(disposalContainer.GetComponent<ObjectContainer>());
			}
			else
			{
				virtualContainer.ObjectContainer.StoreObject(sourceObject);
				selfControlledOnStart = true;
			}
			if (sourceObject.TryGetComponent<GasContainer>(out var gasContainer))
			{
				GasMix.TransferGas(disposalContainer.GetComponent<GasContainer>().GasMix, gasContainer.GasMix, gasContainer.GasMix.Moles);
			}
			else
			{
				var tile = sourceObject.RegisterTile();
				var gasMix = tile.Matrix.MetaDataLayer.Get(tile.LocalPositionServer)?.GasMix;
				if (gasMix != null)
				{
					GasMix.TransferGas(disposalContainer.GetComponent<GasContainer>().GasMix, gasMix, gasMix.Moles);
				}
			}

			// Start traversing
			var traversal = new DisposalTraversal(virtualContainer);
			virtualContainer.traversal = traversal;
			virtualContainer.SelfControlled = selfControlledOnStart;
			disposalInstances.Add(traversal);
		}

		/// <summary>
		/// Removes the disposal instance, stopping it from attempting to continue traversing the pipe network.
		/// </summary>
		/// <param name="disposal">The disposal instance to remove</param>
		public void FinishDisposal(DisposalTraversal disposal)
		{
			disposalInstances.Remove(disposal);
		}

		private void UpdateDisposal(DisposalTraversal disposal)
		{
			if (disposal.virtualContainer.SelfControlled)
			{
				return;
			}

			if (disposal.TraversalFinished)
			{
				FinishDisposal(disposal);
			}
			else if (disposal.ReadyToTraverse)
			{
				disposal.Traverse();
			}

			disposal.CurrentlyDelayed = true;
			StartCoroutine(DelayTraversal(disposal));
		}

		private IEnumerator DelayTraversal(DisposalTraversal disposal)
		{
			yield return WaitFor.Seconds(1 / TileTraversalsPerSecond);
			disposal.CurrentlyDelayed = false;
		}
	}
}
