using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class EditModeControl: MonoBehaviour {
    public float snapValue = 1;
    public float depth = 0;

    public bool useInGame;

    void Start() {
        Snap(); // snap on instantiate
    }

    public void Snap() {
        float snapInverse = 1 / snapValue;

        // if snapValue = .5, x = 1.45 -> snapInverse = 2 -> x*2 => 2.90 -> round 2.90 => 3 -> 3/2 => 1.5
        // so 1.45 to nearest .5 is 1.5
        float x = Mathf.Round(transform.position.x * snapInverse) / snapInverse;
        float y = Mathf.Round(transform.position.y * snapInverse) / snapInverse;
        float z = depth;  // depth from camera

        transform.position = new Vector3(x, y, z);
    }
}