using UnityEngine;

public class ParallaxStars : MonoBehaviour
{
	private void Update()
	{
		if ((Camera.main.transform.position.x - transform.position.x) > 30f) {
			Vector2 pos = transform.localPosition;
			pos.x += 60f;
			transform.localPosition = pos;
		}

		if ((Camera.main.transform.position.x - transform.position.x) < -30f) {
			Vector2 pos = transform.localPosition;
			pos.x -= 60f;
			transform.localPosition = pos;
		}

		if ((Camera.main.transform.position.y - transform.position.y) > 30f) {
			Vector2 pos = transform.localPosition;
			pos.y += 60f;
			transform.localPosition = pos;
		}

		if ((Camera.main.transform.position.y - transform.position.y) < -30f) {
			Vector2 pos = transform.localPosition;
			pos.y -= 60f;
			transform.localPosition = pos;
		}
	}
}