using UnityEngine;
using UnityEngine.UI;


namespace UI {

    public enum DamageZoneSelector {
        torso,
        head,
        eyes,
        mouth,
        r_arm,
        l_arm,
        r_leg,
        l_leg
    }

    public class ZoneSelector: MonoBehaviour {
        public Sprite[] selectorSprites;
        public Image selImg;

        public void SelectAction(int curSelect) {
            SoundManager.Play("Click01");
            selImg.sprite = selectorSprites[curSelect];
            UIManager.DamageZone = (DamageZoneSelector) curSelect;
        }
    }
}