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
		[Tooltip("The sickness to infect nearby players with, uses index from SicknessManager")]
		[SerializeField] private GameObject sicknessToInfect;

		public override void OnEffect(PlayerScript player, BodyPart part = null)
		{
			//TODO: Reimplement this to give the player space cancer
			//TODO: Give artifacts a custom sickness 'Artifact Sickness' they give instead of Space Cancer

			/*
			if(sicknessToInfect.TryGetComponent<Sickness>(out var sickness) == false) return;

			if (player.playerHealth.mobSickness.HasSickness(sickness)) return;

			SpawnResult spawnResult = Spawn.ServerPrefab(sicknessToInfect);

			if (spawnResult.Successful == false || spawnResult.GameObject.TryGetComponent<Sickness>(out var newSick) == false) return;

			SpawnResult sicknessResult = Spawn.ServerPrefab(sicknessToInfect, Vector3.zero, player.gameObject.transform);

			sicknessResult.GameObject.GetComponent<Sickness>().SetCure(newSick.CureForSickness);

			player.playerHealth.AddSickness(sicknessResult.GameObject.GetComponent<Sickness>());		*/
		}
	}
}
