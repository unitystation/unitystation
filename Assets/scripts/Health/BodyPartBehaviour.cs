using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;

public class BodyPartBehaviour : MonoBehaviour
{
    public BodyPartType Type;

    public Sprite GreenDamageMonitorIcon;
    public Sprite YellowDamageMonitorIcon;
    public Sprite OrangeDamageMonitorIcon;
    public Sprite RedDamageMonitorIcon;
    public Sprite GrayDamageMonitorIcon;

    //50 for limbs, 200 for the head and torso(?)
    public int MaxDamage = 50;
    private int _damage;
    private DamageSeverity _severity;
    public DamageSeverity Severity
    {
        get { return _severity; }
    }

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
        if (!IsLocalPlayer()) return;
        UI.UIManager.PlayerHealthUI.SetBodyTypeOverlay(this);
    }

    protected bool IsLocalPlayer()
    {
        //kinda crappy way to determine local player,
        //but otherwise UpdateIcons would have to be moved to HumanHealthBehaviour
        return PlayerManager.LocalPlayerScript == gameObject.GetComponentInParent<PlayerScript>();
    }

    private void UpdateSeverity()
    {
        float severity = (float)_damage / MaxDamage;
        if (severity >= 0.2 && severity < 0.4)
        {
            _severity = DamageSeverity.Moderate;
        }
        else if (severity >= 0.4 && severity < 0.7)
        {
            _severity = DamageSeverity.Bad;
        }
        else if (severity >= 0.7)
        {
            _severity = DamageSeverity.Critical;
        }
        else
        {
            _severity = DamageSeverity.None;
        }
        UpdateIcons();
    }


    public virtual void RestoreDamage()
    {
        _damage = 0;
        UpdateSeverity();
    }
}