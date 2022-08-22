using HealthV2;
using Messages.Server.SoundMessages;
using UnityEngine;


namespace Player
{
	/// <summary>
    /// Allows an object to be hugged by a player.
    /// </summary>
    public class Huggable : MonoBehaviour, ICheckedInteractable<HandApply>, ICooldown
    {
    	private HandApply interaction;
    	private string performerName;
    	private string targetName;

    	public float DefaultTime { get; } = 15f;

    	/// <summary>
    	/// The chance of being gibbed after pulling a tail multiple times.
    	/// </summary>
    	private readonly float tailPullJudgementChance = 0.4f;
    	/// <summary>
    	/// Who was the last one to pull a tail?
    	/// </summary>
    	private LivingHealthMasterBase lastPuller;

    	[SerializeField] private ItemTrait tailTrait;

    	public bool WillInteract(HandApply interaction, NetworkSide side)
    	{
    		if (!DefaultWillInteract.Default(interaction, side)) return false;
    		if (interaction.Intent != Intent.Help) return false;
    		if (interaction.HandObject != null) return false;
    		if (interaction.TargetObject == interaction.Performer) return false;

    		if (interaction.TargetObject.TryGetComponent(out PlayerHealthV2 targetPlayerHealth))
    		{
    			if (targetPlayerHealth.ConsciousState != ConsciousState.CONSCIOUS) return false;
    		}

    		var performerRegisterPlayer = interaction.Performer.GetComponent<RegisterPlayer>();
    		if (performerRegisterPlayer.IsLayingDown) return false;

    		return true;
    	}

    	public void ServerPerformInteraction(HandApply interaction)
    	{
    		this.interaction = interaction;
    		performerName = interaction.Performer.ExpensiveName();
    		targetName = interaction.TargetObject.ExpensiveName();

    		if (interaction.TargetObject.TryGetComponent(out RegisterPlayer targetRegisterPlayer) && targetRegisterPlayer.IsLayingDown)
    		{
    			HelpUp();
    		}
    		else if (!TryFieryHug())
    		{
    			Hug();
    		}

    		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.8f, 1.2f));
    		SoundManager.PlayNetworkedAtPos(
    				CommonSounds.Instance.ThudSwoosh, interaction.TargetObject.AssumedWorldPosServer(), audioSourceParameters, sourceObj: interaction.TargetObject);
    	}

    	// TODO Consider moving this into its own component, or merging Huggable, this and CPRable into
    	// one "Helpable" component.
    	private void HelpUp()
    	{
    		var targetRegisterPlayer = interaction.TargetObject.GetComponent<RegisterPlayer>();
    		targetRegisterPlayer.ServerHelpUp();

    		Chat.AddActionMsgToChat(interaction.Performer,
    				$"You try to help {targetName} up.", $"{performerName} tries to help {targetName} up.");
    		Chat.AddExamineMsgFromServer(interaction.TargetObject, $"{performerName} tries help you up!");
    	}

    	private bool TryFieryHug()
    	{
    		var performerLHB = interaction.Performer.GetComponent<LivingHealthMasterBase>();
    		var targetLHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();

    		if (performerLHB != null && targetLHB != null && (performerLHB.FireStacks > 0 || targetLHB.FireStacks > 0))
    		{
    			performerLHB.ApplyDamageAll(interaction.TargetObject, 1, AttackType.Fire, DamageType.Burn);
    			targetLHB.ApplyDamageAll(interaction.Performer, 1, AttackType.Fire, DamageType.Burn);

    			Chat.AddCombatMsgToChat(
    					interaction.Performer, $"You hug {targetName} with fire!", $"{performerName} hugs {targetName} with fire!");
    			Chat.AddExamineMsgFromServer(interaction.TargetObject, $"{performerName} hugs you with fire!");

    			return true;
    		}

    		return false;
    	}

    	private void Judgement(LivingHealthMasterBase puller)
    	{
    		if (Cooldowns.TryStart(
    			    interaction.PerformerPlayerScript, this, NetworkSide.Server) == false)
    		{
    			lastPuller = null;
    		}
    		if (lastPuller == null || lastPuller != puller)
    		{
    			lastPuller = puller;
    			return;
    		}
    		if (DMMath.Prob(tailPullJudgementChance))
    		{
    			Chat.AddExamineMsg(puller.gameObject, $"<color=red><size=+24>You have been judged for your lust..</size></color>");
    			puller.OnGib();
    		}
    	}

    	private bool PullTail()
    	{
    		var performerLHB = interaction.Performer.GetComponent<LivingHealthMasterBase>();
    		var targetLHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
    		if (targetLHB == null) return false;
    		foreach (var possibleTail in targetLHB.BodyPartList)
    		{
    			if (possibleTail.gameObject.Item().HasTrait(tailTrait) == false) continue;
    			Chat.AddActionMsgToChat(interaction.Performer, $"<color=#be2596>You pull on {targetName}'s tail.</color>",
    				$"<color=#be2596>{performerName} pulls on {targetName}'s tail!</color>");
    			Chat.AddExamineMsgFromServer(interaction.TargetObject, $"<color=#be2596>{performerName} hugs you.</color>");
    			Judgement(performerLHB);
    			return true;
    		}
    		return false;
    	}

    	private void Hug()
    	{
    		if(interaction.TargetBodyPart == BodyPartType.Groin && PullTail()) return;
    		Chat.AddActionMsgToChat(interaction.Performer, $"You hug {targetName}.", $"{performerName} hugs {targetName}.");
    		Chat.AddExamineMsgFromServer(interaction.TargetObject, $"{performerName} hugs you.");
    	}
    }
}
