using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Indicates an object that emits sound upon activation (bike horn/air horn...)
/// </summary>
public class Horn : Interactable<HandActivate, PositionalHandApply>
{
	public string Sound;
	public float Cooldown = 0.2f;

	//todo: emit HONK particles

	/// <summary>
	/// Chance of causing minor ear injury when honking next to a living being
	/// </summary>
	[Range(0f, 1f)]
	public float CritChance = 0.1f;

	[Range( 0f, 100f )]
	public float CritDamage = 5f;

	private bool allowUse = true;
	private float randomPitch => Random.Range( 0.7f, 1.2f );

	private IEnumerator CoolDown()
	{
		allowUse = false;
		yield return WaitFor.Seconds(Cooldown);
		allowUse = true;
	}

	private void TryHonk(PositionalHandApply clickData = null)
	{
		if ( !allowUse )
		{
			return;
		}

		// simple honk on activate
		if ( clickData == null )
		{
			ClassicHonk();
		}
		else
		{
			bool inCritRange = PlayerScript.IsInReach( clickData.TargetVector );
			var targetHealth = clickData.TargetObject.GetComponent<LivingHealthBehaviour>();
			if ( inCritRange && (targetHealth != null) )
			{
				// honking in the face
				bool isCrit = Random.Range( 0f, 1f ) <= CritChance;
				clickData.Performer.GetComponent<WeaponNetworkActions>().RpcMeleeAttackLerp( clickData.TargetVector, this.gameObject );

				PostToChatMessage.SendAttackMessage(clickData.Performer, clickData.TargetObject, isCrit ? 100 : 1, BodyPartType.None, this.gameObject);

				ClassicHonk();

				if ( isCrit )
				{
					StartCoroutine( CritHonk( clickData, targetHealth ) );
				}

			}
			else
			{
				ClassicHonk();
			}
		}

		StartCoroutine( CoolDown());
	}

	private IEnumerator CritHonk( PositionalHandApply clickData, LivingHealthBehaviour targetHealth )
	{
		yield return WaitFor.Seconds( 0.02f );
		SoundManager.PlayNetworkedAtPos( Sound, gameObject.AssumedWorldPosServer(), -1f, true, true, 20, 5 );
		targetHealth.ApplyDamage( clickData.Performer, CritDamage, AttackType.Energy, DamageType.Brute, BodyPartType.Head );
	}

	/// <summary>
	///	honk on activate
	/// </summary>
	protected override void ServerPerformInteraction( HandActivate interaction )
	{
		TryHonk();
	}

	/// <summary>
	/// honk on world click
	/// </summary>
	protected override void ServerPerformInteraction( PositionalHandApply interaction )
	{
		TryHonk(interaction);
	}

	private void ClassicHonk()
	{
		SoundManager.PlayNetworkedAtPos( Sound, gameObject.AssumedWorldPosServer(), randomPitch, true );
	}

	/// <summary>
	/// Allow honking when barely conscious
	/// </summary>
	protected override bool WillInteract( HandActivate interaction, NetworkSide side )
	{
		return Validations.CanInteract( interaction.Performer, side, true );
	}

	/// <summary>
	/// Allow honking when barely conscious and when clicking anything
	/// </summary>
	protected override bool WillInteractT2( PositionalHandApply interaction, NetworkSide side )
	{
		return Validations.CanApply(interaction.Performer, interaction.TargetObject, side, true, ReachRange.Unlimited, interaction.TargetVector);
	}
}