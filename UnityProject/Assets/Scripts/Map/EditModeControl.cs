﻿using UnityEngine;

[ExecuteInEditMode]
public class EditModeControl : MonoBehaviour
{
    public float depth;
    public float snapValue = 1f;

    public bool useInGame;

    private void Start()
    {
        Snap(); // snap on instantiate
    }

    public Vector3 Snap()
    {
        return transform.position = Snap(transform.position);
    }

    public Vector3 Snap(Vector3 rawPos)
    {
        var snapInverse = 1 / snapValue;

        // if snapValue = .5, x = 1.45 -> snapInverse = 2 -> x*2 => 2.90 -> round 2.90 => 3 -> 3/2 => 1.5
        // so 1.45 to nearest .5 is 1.5
        var x = Mathf.Round(rawPos.x * snapInverse) / snapInverse;
        var y = Mathf.Round(rawPos.y * snapInverse) / snapInverse;
        var z = depth; // depth from camera

        return new Vector3(x, y, z);
    }
}