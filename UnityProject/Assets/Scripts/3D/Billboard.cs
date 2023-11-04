using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
	public Transform cam;

	private void Start()
	{
		cam = Camera.main.transform;
	}

	void LateUpdate()
	{
		if (cam == null) Destroy(this);
		if ((transform.position - cam.transform.position).magnitude < 25)
		{
			transform.LookAt(transform.position + cam.forward, cam.up);
			transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
		}
	}
}