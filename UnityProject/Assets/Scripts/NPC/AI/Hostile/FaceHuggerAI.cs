﻿using System;
using System.Collections.Generic;
using Clothing;
using UnityEngine;
using Doors;
using Systems.Mob;
using Random = UnityEngine.Random;
using AddressableReferences;
using HealthV2;
using Messages.Server.SoundMessages;
using UnityEngine.Serialization;


namespace Systems.MobAIs
{
	public class FaceHuggerAI : GenericHostileAI, ICheckedInteractable<HandApply>, IServerSpawn
	{
		[SerializeField]
		[Tooltip("If true, this hugger won't be counted for the cap Queens use for lying eggs.")]
		private bool ignoreInQueenCount = false;
		//private MobMeleeAction mobMeleeAction;
		private FaceHugAction faceHugAction;

		public override void OnEnable()
		{
			base.OnEnable();
			faceHugAction = gameObject.GetComponent<FaceHugAction>();
		}

		/// <summary>
		/// Looks around and tries to find players to target
		/// </summary>
		/// <returns>Gameobject of the first player it found</returns>
		protected override GameObject SearchForTarget()
		{
			var hits = coneOfSight.GetObjectsInSight(hitMask, LayerTypeSelection.Walls , directional.CurrentDirection.Vector, 10f, 20);
			if (hits.Count == 0)
			{
				return null;
			}

			foreach (var coll in hits)
			{
				if (coll.GameObject == null) continue;

				if (coll.GameObject.layer == playersLayer)
				{
					return coll.GameObject;
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

			Despawn.ServerSingle(gameObject);
		}

		public override void OnSpawnServer(SpawnInfo info)
		{
			if (ignoreInQueenCount == false)
			{
				XenoQueenAI.AddFacehuggerToCount();
			}
			base.OnSpawnServer(info);
			ResetBehaviours();
			BeginSearch();
		}
	}
}
