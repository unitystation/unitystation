using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerHealthUI : MonoBehaviour
    {
        /// placeholder based on old code
        public void SetBodyTypeOverlay(BodyPartBehaviour bodyPart)
        {
            foreach (var listener in UIManager.Instance.GetComponentsInChildren<DamageMonitorListener>())
            {
                if (listener.bodyPartType != bodyPart.Type)
                    continue;
                Sprite sprite;
                switch (bodyPart.Severity)
                {
                    case DamageSeverity.None:
                        sprite = bodyPart.GreenDamageMonitorIcon; break;
                    case DamageSeverity.Moderate:
                        sprite = bodyPart.YellowDamageMonitorIcon; break;
                    case DamageSeverity.Bad:
                        sprite = bodyPart.OrangeDamageMonitorIcon; break;
                    case DamageSeverity.Critical:
                        sprite = bodyPart.RedDamageMonitorIcon; break;
                    default:
                        sprite = bodyPart.GrayDamageMonitorIcon; break;
                }
                listener.GetComponent<Image>().sprite = sprite;
            }
        }
    }
}