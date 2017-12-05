using UnityEngine;
using System.Collections;
using UI;
using UnityEngine.UI;


public class temp_ClownBtn : MonoBehaviour
{
    public Transform clowns;
    public Camera currentCam;
    public Text toolTip;

    public void KloonButton()
    {

        SoundManager.Play("Click01");
        float ranNum = Random.Range(1f, 3f);
        float ranNum2 = Random.Range(1f, 3f);
        Vector2 newVect = currentCam.ScreenToWorldPoint(new Vector2(Screen.width / ranNum, Screen.height / ranNum2));

        Instantiate(clowns, newVect, Quaternion.identity, null);

        toolTip.text = "a scene straight out of Stephen Kings mind";
    }
}
