using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RotateEffect : LTEffect
{
    private int flips;
    private float animTime;
    private float rotationAngle;
    private bool isRandom = true;

    public override void CmdStartAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(Rotate(flips, animTime));
        base.CmdStartAnimation();
    }

    public void setupEffectvars(int f, float at, float rotAngle, bool random)
    {
        flips = f;
        animTime = at;
        rotationAngle = rotAngle;
		isRandom = random;
    }

    private IEnumerator Rotate(int numberOfrotates, float time)
    {
		float rotationResult = 0.5f;
		if (isRandom)
		{
			rotationResult = Random.value;
		}
		var trackedrotates = 0;
        while (numberOfrotates >= trackedrotates)
        {
            var rot = tween.target.rotation.eulerAngles;
            rot.z = pickRandomRotation(rot, rotationAngle, rotationResult);
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

	private float pickRandomRotation(Vector3 rotation, float target, float result)
	{
		if (result >= 0.5f)
		{
			return rotation.z += target;
		}
		else
		{
			return rotation.z -= target;
		}
	}
}
