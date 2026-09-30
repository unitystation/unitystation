using System.Linq;
using SecureStuff;
using UnityEngine;
using US13.Actions.V2;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.HealthV2.Living;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Messages.Server.SoundMessages;
using US13.Objects.Pipes;
using US13.Objects.Pipes.Devices;
using US13.Player;
using US13.Tilemaps.Behaviours.Meta.Atmospherics.Data;
using US13.UI.Core.ProgressBar;
using Util;

public class MouseHeadActions : BodyPartFunctionality
{
	[field:SerializeField]
	public SerializableDictionary<ActionButtonData, SerializedAction> ActivatedAbilities { get; set; }
	[SerializeField] private AddressableAudioSource squeakSound = null;
	[SerializeField] private string EnterVentID = "vent_crawl";


	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{
		foreach (var action in ActivatedAbilities.Keys)
		{
			livingHealth.playerScript.PlayerButtonedActions?.RegisterNewAction(action, ActivatedAbilities[action].Invoke);
		}
	}

	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
	{
		foreach (var action in ActivatedAbilities.Keys)
		{
			livingHealth.playerScript?.PlayerButtonedActions?.UnregisterAction(action);
		}
		base.OnRemovedFromBody(livingHealth, source);
	}

	public void Squeak(Vector2 worldMousePosition)
	{
		AudioSourceParameters audioSourceParameters = default(AudioSourceParameters);
		audioSourceParameters.Pitch = Random.Range(.6f, 1.2f);
		SoundManager.PlayNetworkedAtPos(squeakSound, RelatedPart.HealthMaster.ObjectBehaviour.OfficialPosition, audioSourceParameters);

		Chat.AddActionMsgToChat(
			gameObject,
			$"{RelatedPart.HealthMaster.playerScript.visibleName} squeaks!",
			$"{RelatedPart.HealthMaster.playerScript.visibleName} squeaks!");
	}

	public void EnterVent(Vector2 worldMousePosition)
	{
			var matrix = RelatedPart.HealthMaster.playerScript.RegisterPlayer.Matrix;
			var ventsOnTile = matrix.Get<AirVent>(worldMousePosition.To3Int().ToLocal(matrix).CutToInt(), CustomNetworkManager.IsServer).ToList();
			if (ventsOnTile.Count != 0)
			{
				TryEnterVent(RelatedPart.HealthMaster.playerScript, ventsOnTile[0], ventsOnTile[0].pipeMix);
				return;
			}

			var scrubbersOnTile = matrix.Get<Scrubber>(worldMousePosition.To3Int().ToLocal(matrix).CutToInt(), CustomNetworkManager.IsServer).ToList();
			if (scrubbersOnTile.Count != 0)
			{
				TryEnterVent(RelatedPart.HealthMaster.playerScript, scrubbersOnTile[0], scrubbersOnTile[0].pipeMix);
				return;
			}

			RelatedPart.HealthMaster.playerScript.PlayerButtonedActions?.ServerEndCooldown(EnterVentID);
			Chat.AddWarningMsgFromServer(RelatedPart.HealthMaster.gameObject, "There is no pipe access on this tile!");
	}

	private void TryEnterVent(PlayerScript player, MonoPipe monoPipe, GasMix pipeMix)
	{
		Chat.AddActionMsgToChat(gameObject, $"You start to enter the {monoPipe.gameObject.ExpensiveName()}",
			$"{RelatedPart.HealthMaster.playerScript.visibleName} starts to enter the {monoPipe.gameObject.ExpensiveName()}!");

		var cfg = new StandardProgressActionConfig(StandardProgressActionType.Escape);
		StandardProgressAction.Create(
			cfg,
			() => monoPipe.FinishEnteringPipe(player, pipeMix)
		).ServerStartProgress(ActionTarget.Object(monoPipe.registerTile), 5, player.gameObject);
	}
}
