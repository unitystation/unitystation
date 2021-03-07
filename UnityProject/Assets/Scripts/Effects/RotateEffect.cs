using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RotateEffect : LTEffect
{
    private int flips;
    private float animTime;
    private float rotationAngle;


    [Server]
    public override void CmdStartAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(Rotate(flips, animTime));
        base.CmdStartAnimation();
    }

    public void setupEffectvars(int f, float at, float rotAngle)
    {
		Debug.Log("Setting up variables");
        flips = f;
        animTime = at;
        rotationAngle = rotAngle;
    }

    private IEnumerator Rotate(int numberOfrotates, float time)
    {
		Debug.Log("Animation should be working now");
        var trackedrotates = 0;
        while (numberOfrotates >= trackedrotates)
        {
            var rot = tween.target.rotation.eulerAngles;
            rot.z += rotationAngle;
            trackedrotates++;
            rotateObject(rot, time);
            yield return new WaitForSeconds(time);
        }
        base.CmdStopAnimation();
    }

    private void rotateObject(Vector3 rot, float time)
    {
        tween.RotateGameObject(rot, time);
    }
}
