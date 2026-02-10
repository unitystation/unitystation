using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderFrameTimer : MonoBehaviour
{
	private const float SampleDuration = 1;

	public float renderTime;
	public float renderTimeLong;

	private float mRenderStart;
	private int frames;
	private float sampleTime;
	private float sample;
	private float sampleTimeLong;
	private float sampleLong;
	private int framesLong;


	private void OnPreRender()
	{
		mRenderStart = Time.realtimeSinceStartup;
	}

	private void WaitForEndOfFrame()
	{

	}

	public IEnumerator Start()
	{
		while (true)
		{
			// Wait until all rendering + UI is done.
			yield return new WaitForEndOfFrame();
			OnEndOfFrame();
		}
	}

	private void OnEndOfFrame()
	{
		sampleTime += Time.deltaTime;
		sample += Time.realtimeSinceStartup - mRenderStart;
		frames++;

		if (frames >= 60)
		{
			// display two fractional digits (f2 format)
			renderTime = (sample) * 1000;

			sampleTime = 0;
			sample = 0;
			frames = 0;
		}


		sampleTimeLong += Time.deltaTime;
		sampleLong += Time.realtimeSinceStartup - mRenderStart;
		framesLong++;

		if (framesLong >= 600)
		{
			// display two fractional digits (f2 format)
			renderTimeLong = (sampleLong) * 1000;

			sampleTimeLong = 0;
			sampleLong = 0;
			framesLong = 0;
		}
	}
}
