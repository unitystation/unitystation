using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VLSLightComputeScript : MonoBehaviour {

	public ComputeShader shader;
	private const string mainKernel = "CSMain";
	private const string resultMethod = "Result";

	void Start () {
		RunShader();
	}

	public void RunShader(){
		int kernelIndex = shader.FindKernel(mainKernel);

		RenderTexture tex = new RenderTexture(512, 512, 24);
		tex.enableRandomWrite = true;
		tex.Create();

		shader.SetTexture(kernelIndex, resultMethod, tex);
		shader.Dispatch(kernelIndex, 512 / 8, 512 / 8, 1);
	}
}
