/*

Deprecated shader. Will fallback to "Light 20 Points".

*/

Shader "Light2D/Light 8 Points" {
Properties {
	_MainTex ("Light texture", 2D) = "white" {}
	_ObstacleMul ("Obstacle Mul", Float) = 500
	_EmissionColorMul ("Emission color mul", Float) = 1
}

Fallback "Light2D/Light 20 Points"

}