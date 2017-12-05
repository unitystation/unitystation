using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class More_Buttons : MonoBehaviour
{

    public void Patr_btn()
    {
        Application.OpenURL("http://patreon.com/unitystation");
        SoundManager.Play("Click01");
    }

    public void Git_btn()
    {
        Application.OpenURL("http://github.com/unitystation/unitystation");
        SoundManager.Play("Click01");
    }

    public void Reddit_btn()
    {
        Application.OpenURL("http://reddit.com/r/unitystation");
        SoundManager.Play("Click01");
    }

    public void Discord_btn()
    {
        Application.OpenURL("https://discord.gg/tFcTpBp");
        SoundManager.Play("Click01");
    }

    public void Issues_btn()
    {
        Application.OpenURL("http://github.com/unitystation/unitystation/issues");
        SoundManager.Play("Click01");
    }
}
