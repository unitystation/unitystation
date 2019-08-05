using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Indicates an object that emits sound upon activation (bike horn/air horn...)
/// </summary>
public class Horn : NetworkBehaviour, IInteractable<HandActivate>, IInteractable<PositionalHandApply>
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

	private IEnumerator CoolDown()
	{
		allowUse = false;
		yield return WaitFor.Seconds(Cooldown);
		allowUse = true;
	}

	//fixme: apparently it's only executed on client
	//todo: check if clientside AssumedLocation calls are broken
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
					SoundManager.PlayNetworkedAtPos( Sound, gameObject.AssumedWorldPosServer(), Random.Range( 0.8f, 1.2f ), true, 20, 5 );
					targetHealth.ApplyDamage(clickData.Performer, CritDamage, AttackType.Energy, DamageType.Brute, BodyPartType.Head);
				}

			}
			else
			{
				ClassicHonk();
			}
		}

		StartCoroutine( CoolDown());
	}

	private void ClassicHonk()
	{
		SoundManager.PlayNetworkedAtPos( Sound, gameObject.AssumedWorldPosServer(), Random.Range( 0.8f, 1.2f ) );
	}

	/// <summary>
	///	honk on activate
	/// </summary>
	public bool Interact(HandActivate interaction)
	{
		if ( !DefaultWillInteract.HandActivate( interaction, NetworkSide.Client ) )
		{
			return false;
		}

		TryHonk();
		return true;
	}


	/// <summary>
	/// honk on world click
	/// </summary>
	public bool Interact( PositionalHandApply interaction )
	{
		if ( !DefaultWillInteract.PositionalHandApply( interaction, NetworkSide.Client ) )
		{
			return false;
		}

		TryHonk(interaction);
		return true;
	}
}