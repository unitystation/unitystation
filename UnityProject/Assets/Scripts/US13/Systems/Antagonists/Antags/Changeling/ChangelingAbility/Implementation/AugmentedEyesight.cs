using Mirror;
using UnityEngine;
using US13.Core.Camera;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs;
using US13.Managers.NetworkManagement;
using US13.Player;

namespace US13.Systems.Antagonists.Antags.Changeling.ChangelingAbility.Implementation
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Systems/ChangelingAbilities/AugmentedEyesight")]
	public class AugmentedEyesight: ChangelingToggleAbility
	{
		[SerializeField] private Vector3 expandedNightVisionVisibility = new(25, 25, 42);
		public Vector3 ExpandedNightVisionVisibility => expandedNightVisionVisibility;

		[SerializeField] private float defaultvisibilityAnimationSpeed = 1.25f;
		public float DefaultvisibilityAnimationSpeed => defaultvisibilityAnimationSpeed;

		[SerializeField] private float revertvisibilityAnimationSpeed = 0.2f;
		public float RevertvisibilityAnimationSpeed => revertvisibilityAnimationSpeed;

		public override bool UseAbilityToggleClient(ChangelingMain changeling, bool toggle, bool fromServer = false)
		{
			if (Camera.main == null ||
				Camera.main.TryGetComponent<CameraEffectControlScript>(out var effects) == false) return true;

			effects.AdjustPlayerVisibility(
				toggle ? ExpandedNightVisionVisibility : effects.MinimalVisibilityScale,
				toggle ? DefaultvisibilityAnimationSpeed : RevertvisibilityAnimationSpeed);
			effects.NvgHasMaxedLensRadius(true);

			if (fromServer == false)
			{
				PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilitesToggle(Index, toggle);
			}
			return true;
		}

		[Server]
		public override bool UseAbilityToggleServer(ChangelingMain changeling, bool toggle)
		{
			if (CustomNetworkManager.IsServer == false) return false;
			foreach (var bodyPart in changeling.ChangelingMind.Body.playerHealth.BodyPartList)
			{
				foreach (BodyPartFunctionality organ in bodyPart.OrganList)
				{
					if (organ is Eye eye)
					{
						eye.SyncXrayState(eye.HasXray, toggle);
					}
				}
			}
			return true;
		}
	}
}