using HealthV2;
using UnityEngine;

namespace Systems.Research
{
	/// <summary>
	/// Gives nearby players artifact sickness
	/// </summary>
	[CreateAssetMenu(fileName = "SicknessAreaEffect", menuName = "ScriptableObjects/Systems/Artifacts/SicknessAreaEffect")]
	public class  SicknessAreaEffect : AreaEffectBase
	{
		public override void OnEffect(PlayerScript player, BodyPart part = null)
		{
			//TODO: Give artifacts a custom sickness 'Artifact Sickness' they give instead of Space Cancer
			player.playerHealth.reagentPoolSystem.BloodPool.Add(CommonSicknesses.Instance.SpaceFluReagent, 1f);
		}
	}
}
