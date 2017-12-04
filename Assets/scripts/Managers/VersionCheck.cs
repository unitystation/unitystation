using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VersionCheck : MonoBehaviour
{

    private static VersionCheck versionCheck;
    const string VERSION_NUMBER = "0.1.3";
    const string urlCheck = "http://doobly.izz.moe/unitystation/checkversion.php";

    public Text versionText;
    public Text yourVerText;
    public Text newVerText;

    public GameObject loginWindow;
    public GameObject updateWindow;
    public GameObject errorWindow;

    public static VersionCheck Instance
    {
        get
        {
            if (!versionCheck)
            {
                versionCheck = FindObjectOfType<VersionCheck>();
            }
            return versionCheck;
        }
    }

    void Start()
    {
        versionText.text = VERSION_NUMBER;
        //		StartCoroutine(CheckVersion());
    }

    IEnumerator CheckVersion()
    {
        string url = urlCheck + "?ver=" + VERSION_NUMBER;
        WWW get_curVersion = new WWW(url);
        yield return get_curVersion;

        if (get_curVersion.text == "1")
        {
            //			Debug.Log("Is up to date");
            loginWindow.SetActive(true);
        }
        else if (get_curVersion.text == "")
        {
            errorWindow.SetActive(true);
        }
        else
        {
            //			Debug.Log("Update required to: Version " + get_curVersion.text);
            updateWindow.SetActive(true);
            yourVerText.text = VERSION_NUMBER;
            newVerText.text = get_curVersion.text;
        }
    }

    public void DownloadButton()
    {
        SoundManager.Play("Click01", 1, 1, 0);

        Application.OpenURL("http://doobly.izz.moe/unitystation/");
        Application.Quit();
    }

    public void CheckAgain()
    {
        SoundManager.Play("Click01", 1, 1, 0);
        errorWindow.SetActive(false);
        StartCoroutine(CheckVersion());
    }
}
