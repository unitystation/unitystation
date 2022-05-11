using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UI.Core.NetUI;
using UI.Objects.Cargo;


namespace UI.Objects.Cargo
{
    public class GUI_CargoOfflinePage : GUI_CargoPage
    {
        public Text errorText;
        public float blinkTime = 0.3f;

        public override void OpenTab()
        {
            base.OpenTab();
            StartCoroutine(AnimateText());
        }

        private IEnumerator AnimateText()
        {
            while(gameObject.activeSelf)
            {
	            errorText.SetActive(false);
	            yield return WaitFor.Seconds(blinkTime);
	            errorText.SetActive(true);
            }
            errorText.SetActive(true);
        }
    }
}