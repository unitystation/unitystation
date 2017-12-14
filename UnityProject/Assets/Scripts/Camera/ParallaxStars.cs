using UnityEngine;

public class ParallaxStars : MonoBehaviour
{
    private Transform[,] backgrounds;

    private Vector3 currentPosition = Vector2.zero;
    private int offsetX, offsetY;
    public float speed = 1f;

    private void Start()
    {
        backgrounds = new Transform[3, 3];
        foreach (Transform child in transform)
        {
            var localPos = child.localPosition;
            var x = (int) (localPos.x == 0 ? 0 : Mathf.Sign(localPos.x)) + 1;
            var y = (int) (localPos.y == 0 ? 0 : Mathf.Sign(localPos.y)) + 1;
            backgrounds[x, y] = child;
        }
        currentPosition = transform.localPosition;
    }

    public void MoveInDirection(Vector2 dir)
    {
        transform.position -= new Vector3(dir.x, dir.y) * speed * Time.deltaTime;

        if (backgrounds != null)
        {
            var diff = transform.localPosition - currentPosition;

            currentPosition.x = calculate(currentPosition.x, diff.x, true, ref offsetX);
            currentPosition.y = calculate(currentPosition.y, diff.y, false, ref offsetY);
        }
    }

    private float calculate(float oldValue, float diffValue, bool atX, ref int offset)
    {
        if (Mathf.Abs(diffValue) > 5)
        {
            var index = (1 + (int) Mathf.Sign(diffValue) + offset) % 3;

            for (var i = 0; i < 3; i++)
            {
                var position = atX ? backgrounds[index, i] : backgrounds[i, index];
                position.position -= Mathf.Sign(diffValue) * (atX ? Vector3.right : Vector3.up) * 3 * 10;
            }

            offset = (3 + offset - (int) Mathf.Sign(diffValue)) % 3;
            var localPos = transform.localPosition;
            return (atX ? localPos.x : localPos.y) + Mathf.Sign(diffValue) * 5;
        }
        return oldValue;
    }
}