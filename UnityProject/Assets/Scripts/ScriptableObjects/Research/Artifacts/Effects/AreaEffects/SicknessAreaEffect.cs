using HealthV2;
using Health.Sickness;
using UnityEngine;

namespace Systems.Research
{
	/// <summary>
	/// Gives nearby players artifact sickness
	/// </summary>
	[CreateAssetMenu(fileName = "SicknessAreaEffect", menuName = "ScriptableObjects/Systems/Artifacts/SicknessAreaEffect")]
	public class  SicknessAreaEffect : AreaEffectBase
	{
		[Tooltip("The sickness to infect nearby players with, uses index from SicknessManager")]
		[SerializeField] private int sicknessindex;

		public override void OnEffect(PlayerScript player, BodyPart part = null)
		{
			Sickness sicknessToInfect = SicknessManager.Instance.Sicknesses[sicknessindex];

			if (player.playerHealth.mobSickness.HasSickness(sicknessToInfect)) return;

			SpawnResult spawnResult = Spawn.ServerPrefab(sicknessToInfect.gameObject);

			if (spawnResult.Successful == false || spawnResult.GameObject.TryGetComponent<Sickness>(out var newSick) == false) return;

			SpawnResult sicknessResult = Spawn.ServerPrefab(sicknessToInfect.gameObject, Vector3.zero, player.gameObject.transform);

			sicknessResult.GameObject.GetComponent<Sickness>().SetCure(newSick.CureForSickness);

			player.playerHealth.AddSickness(sicknessResult.GameObject.GetComponent<Sickness>());			
		}
	}
}
