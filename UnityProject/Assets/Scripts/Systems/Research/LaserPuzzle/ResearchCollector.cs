using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjects.Gun;
using Shared.Systems.ObjectConnection;
using UnityEngine;
using Weapons.Projectiles;
using Weapons.Projectiles.Behaviours;

namespace Objects.Research
{
	public class ResearchCollector : MonoBehaviour, IOnHitDetect, IMultitoolSlaveable, ICheckedInteractable<HandApply>
	{
		public LayerMaskData laserData;

		private Rotatable Rotatable;

		private RegisterTile registerTile;

		public ResearchLaserProjector AssociatedResearchLaserProjector;

		public void Awake()
		{
			Rotatable = this.GetComponent<Rotatable>();
			registerTile = this.GetComponent<RegisterTile>();
		}

		private float ConvertToWorldRotation(float Local)
		{
			if (Local >= 360)
			{
				Local -= 360;
			}
			else if (Local < 0)
			{
				Local += 360;
			}

			var ModifiedAngle = Local;

			if (registerTile.Matrix.MatrixMove != null)
			{
				ModifiedAngle = registerTile.Matrix.MatrixMove.CurrentState.FacingDirection.AsEnum().Rotate360By(ModifiedAngle);
			}

			// If the final angle is greater than or equal to 360 or less than 0, wrap it around.
			if (ModifiedAngle >= 360)
			{
				ModifiedAngle -= 360;
			}
			else if (ModifiedAngle < 0)
			{
				ModifiedAngle += 360;
			}
			return ModifiedAngle;
		}

		public void OnHitDetect(OnHitDetectData data)
		{
			//Only reflect lasers
			if (data.BulletObject.TryGetComponent<Bullet>(out var bullet) == false ||
				bullet.MaskData != laserData) return;


			if (data.BulletObject.TryGetComponent<ContainsResearchData>(out var Data) == false) return;
			if (Data.ResearchData.Technology == null) return;
			var Vector = Rotatable.CurrentDirection.ToLocalVector2Int();
			float rotation = Mathf.Atan2(Vector.y, Vector.x) * Mathf.Rad2Deg;
			rotation += 180;

			if (rotation >= 360)
			{
				rotation -= 360;
			}
			else if (rotation < 0)
			{
				rotation += 360;
			}

			var Worldrotation = ConvertToWorldRotation(rotation);

			var OffsetAngle = Vector2.Angle(data.BulletShootDirection, VectorExtensions.DegreeToVector2(Worldrotation));

			var LaserResearchPower = 5;

			var ResearchPower = Mathf.Round(Mathf.Pow((Mathf.Pow((OffsetAngle / 20f), 4) + 1), -1) * LaserResearchPower);

			Data.ResearchData.ResearchPower = ResearchPower;
			if (AssociatedResearchLaserProjector == null)
			{
				Chat.AddActionMsgToChat(this.gameObject, "The H.I.E.C.A begins to beep. Ensure a connection to a valid R&D laser projector.");
				return;
			}

			AssociatedResearchLaserProjector.RegisterCollectorData(Data.ResearchData);
		}

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.ResearchLaser;
		IMultitoolMasterable IMultitoolSlaveable.Master => AssociatedResearchLaserProjector.OrNull()?.GetComponent<IMultitoolMasterable>();
		bool IMultitoolSlaveable.RequireLink => true;


		/// <summary>
		/// Try to set the master of the device in-game, via e.g. a multitool. Provides the performer
		/// responsible for the link request.
		/// </summary>
		/// <remarks>Master should never be null and it will always be of the relevant connection type.</remarks>
		/// <param name="performer">The performer of the interaction</param>
		/// <param name="master">Requested master to link with</param>
		/// <returns></returns>
		public bool TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			var projector = master.gameObject.GetComponent<ResearchLaserProjector>();
			if (projector != null)
			{
				AssociatedResearchLaserProjector = master.gameObject.GetComponent<ResearchLaserProjector>();
				return true;
			}

			return false;
		}

		/// <summary>Set the master of the device from an editor environment.</summary>
		/// <remarks>The master can be null to indicate an unlinked state.</remarks>
		/// <param name="master">Null for unlinked state</param>
		public void SetMasterEditor(IMultitoolMasterable master)
		{
			AssociatedResearchLaserProjector = master.gameObject.GetComponent<ResearchLaserProjector>();
		}
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject != null) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			Rotatable.RotateBy(1);
		}

	}
}
