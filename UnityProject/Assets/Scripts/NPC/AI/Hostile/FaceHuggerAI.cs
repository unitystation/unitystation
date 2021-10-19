using System;
using System.Collections.Generic;
using Clothing;
using UnityEngine;


namespace Systems.MobAIs
{
	public class FaceHuggerAI : GenericHostileAI, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		[Tooltip("If true, this hugger won't be counted for the cap Queens use for lying eggs.")]
		private bool ignoreInQueenCount = false;
		//private MobMeleeAction mobMeleeAction;
		private FaceHugAction faceHugAction;

		#region Lifecycle

		protected override void Awake()
		{
			faceHugAction = gameObject.GetComponent<FaceHugAction>();
			base.Awake();
		}

		protected override void OnSpawnMob()
		{
			base.OnSpawnMob();
			if (ignoreInQueenCount == false)
			{
				XenoQueenAI.AddFacehuggerToCount();
			}
			ResetBehaviours();
		}

		#endregion

		/// <summary>
		/// Looks around and tries to find players to target
		/// </summary>
		/// <returns>Gameobject of the first player it found</returns>
		protected override GameObject SearchForTarget()
		{
			var hits = coneOfSight.GetObjectsInSight(hitMask, LayerTypeSelection.Walls , directional.CurrentDirection.Vector, 10f);
			if (hits.Count == 0)
			{
				return null;
			}

			foreach (var coll in hits)
			{
				if (coll.layer == playersLayer)
				{
					return coll;
				}
			}

			return null;
		}

		/// <summary>
		/// What happens when the mob dies or is unconscious
		/// </summary>
		protected override void HandleDeathOrUnconscious()
		{
			base.HandleDeathOrUnconscious();

			if (ignoreInQueenCount == false)
			{
				XenoQueenAI.RemoveFacehuggerFromCount();
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side)
			       && (interaction.HandObject == null
			           || (interaction.Intent == Intent.Help || interaction.Intent == Intent.Grab));
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var handSlot = interaction.HandSlot;

			var result = Spawn.ServerPrefab(faceHugAction.MaskObject);
			var mask = result.GameObject;

			if (IsDead || IsUnconscious)
			{
				mask.GetComponent<FacehuggerImpregnation>().KillHugger();
			}

			Inventory.ServerAdd(mask, handSlot, ReplacementStrategy.DropOther);

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
