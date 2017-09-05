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

    //50 for limbs, 200 for the head(?)
    public int MaxDamage = 50;
    private int _damage = 0;
    private DamageSeverity _severity;
    public DamageSeverity Severity
    {
        get { return _severity; }
    }
    
    public virtual void ReceiveDamage(DamageType damageType, int damage)
    {
        UpdateDamage(damage);
        Debug.LogFormat("{0} received {1} {2} damage. Total {3}/{4}, condition is {5}",
                         Type, damage, damageType, _damage, MaxDamage, Severity);
    }

    private void UpdateDamage(int damage)
    {
        _damage += damage;
        if ( _damage > MaxDamage )
        {
            _damage = MaxDamage;
        }
        UpdateSeverity();
    }

    private void UpdateIcons()
    {
//        if (server)return;
        UI.UIManager.PlayerHealth.SetBodyTypeOverlay(this);
    }

    private void UpdateSeverity()
    {
        float severity = (float)_damage / MaxDamage;
        if ( severity >= 0.2 && severity < 0.4 )
        {
            _severity = DamageSeverity.Moderate;
        }
        else if ( severity >= 0.4 && severity < 0.7 )
        {
            _severity = DamageSeverity.Bad;
        }
        else if ( severity >= 0.7 )
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