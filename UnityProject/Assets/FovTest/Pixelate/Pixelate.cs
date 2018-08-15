using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
[RequireComponent(typeof (Camera))]
[AddComponentMenu("Image Effects/Pixelate")]
public class Pixelate:MonoBehaviour{
	public Shader shader;
	int _pixelSizeX=1;
	int _pixelSizeY=1;
	Material _material;
	[Range(1,20)]
	public int pixelSizeX=1;
	[Range(1,20)]
	public int pixelSizeY=1;
	public bool lockXY=true;
	void OnRenderImage(RenderTexture source, RenderTexture destination){
		if(_material==null) _material=new Material(shader);
		_material.SetInt("_PixelateX",pixelSizeX);
		_material.SetInt("_PixelateY",pixelSizeY);
		Graphics.Blit(source,destination,_material);
	}
	void OnDisable(){
		DestroyImmediate(_material);
	}

	void Update(){
		if(pixelSizeX!=_pixelSizeX){
			_pixelSizeX=pixelSizeX;
			if(lockXY) _pixelSizeY=pixelSizeY=_pixelSizeX;
		}
		if(pixelSizeY!=_pixelSizeY){
			_pixelSizeY=pixelSizeY;
			if(lockXY) _pixelSizeX=pixelSizeX=_pixelSizeY;
		}
	}

}