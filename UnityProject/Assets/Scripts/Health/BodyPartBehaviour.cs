using PlayGroup;
using UI;
using UnityEngine;

public class BodyPartBehaviour : MonoBehaviour
{
	private int _damage;
	public Sprite GrayDamageMonitorIcon;

	public Sprite GreenDamageMonitorIcon;

	//50 for limbs, 200 for the head and torso(?)
	public int MaxDamage = 50;

	public Sprite OrangeDamageMonitorIcon;
	public Sprite RedDamageMonitorIcon;
	public BodyPartType Type;
	public Sprite YellowDamageMonitorIcon;

	public DamageSeverity Severity { get; private set; }

	public virtual void ReceiveDamage(DamageType damageType, int damage)
	{
		UpdateDamage(damage);
		//        Debug.LogFormat("{0} received {1} {2} damage. Total {3}/{4}, limb condition is {5}",
		//                         Type, damage, damageType, _damage, MaxDamage, Severity);
	}

	private void UpdateDamage(int damage)
	{
		_damage += damage;
		if (_damage > MaxDamage)
		{
			_damage = MaxDamage;
		}
		UpdateSeverity();
	}

	private void UpdateIcons()
	{
		if (!IsLocalPlayer())
		{
			return;
		}
		UIManager.PlayerHealthUI.SetBodyTypeOverlay(this);
	}

	protected bool IsLocalPlayer()
	{
		//kinda crappy way to determine local player,
		//but otherwise UpdateIcons would have to be moved to HumanHealthBehaviour
		return PlayerManager.LocalPlayerScript == gameObject.GetComponentInParent<PlayerScript>();
	}

	private void UpdateSeverity()
	{
		float severity = (float) _damage / MaxDamage;
		if (severity >= 0.2 && severity < 0.4)
		{
			Severity = DamageSeverity.Moderate;
		}
		else if (severity >= 0.4 && severity < 0.7)
		{
			Severity = DamageSeverity.Bad;
		}
		else if (severity >= 0.7)
		{
			Severity = DamageSeverity.Critical;
		}
		else
		{
			Severity = DamageSeverity.None;
		}
		UpdateIcons();
	}


	public virtual void RestoreDamage()
	{
		_damage = 0;
		UpdateSeverity();
	}
}