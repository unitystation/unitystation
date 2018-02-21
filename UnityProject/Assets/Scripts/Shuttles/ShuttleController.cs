using System.Collections.Generic;
using UnityEngine;

public struct MatrixOrientation
{
	public static readonly MatrixOrientation 
		Up = new MatrixOrientation(0),
		Right = new MatrixOrientation(90),
	 	Down = new MatrixOrientation(180),
		Left = new MatrixOrientation(270);
	private static readonly List<MatrixOrientation> sequence = new List<MatrixOrientation> {Up, Left, Down, Right};
	public readonly int degree;

	private MatrixOrientation(int degree)
	{
		this.degree = degree;
	}

	public MatrixOrientation Next()
	{
		int index = sequence.IndexOf(this);
		if (index + 1 >= sequence.Count || index == -1)
		{
			return sequence[0];
		}
		return sequence[index + 1];
	}

	public MatrixOrientation Previous()
	{
		int index = sequence.IndexOf(this);
		if (index <= 0)
		{
			return sequence[sequence.Count-1];
		}
		return sequence[index - 1];
	}

	public override string ToString()
	{
		return $"{degree}";
	}
}

public class ShuttleController : MonoBehaviour {

	//TEST MODE
	bool doFlyingThing;
	public Vector2 flyingDirection;
	public float speed;
	private readonly float rotSpeed = 6;
	public KeyCode startKey = KeyCode.G;
	public KeyCode leftKey = KeyCode.Keypad4;
	public KeyCode rightKey = KeyCode.Keypad6;
	
	private MatrixOrientation orientation = MatrixOrientation.Up;
	
	void Update(){
		if ( Input.GetKeyDown(startKey) ){
			doFlyingThing = !doFlyingThing;
		}
		if ( Input.GetKeyDown(KeyCode.KeypadPlus) ){
			speed++;
		}
		if ( Input.GetKeyDown(KeyCode.KeypadMinus) ){
			speed--;
		}


		if ( NeedsRotation() ){
			transform.rotation = 
				Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0,0,orientation.degree), Time.deltaTime*90);//transform.Rotate(Vector3.forward * rotSpeed * Time.deltaTime);
		} else if ( NeedsFixing() ){
			// Finishes the job of Lerp and straightens the ship with exact angle value
			transform.rotation = Quaternion.Euler(0, 0, orientation.degree);
		} else {
			//Only fly or change orientation if rotation is finished
			if ( Input.GetKeyDown(leftKey) ){
				Rotate(false);
			}
			if ( Input.GetKeyDown(rightKey) ){
				Rotate(true);
			}
			if ( doFlyingThing ){
				transform.Translate(flyingDirection * speed * Time.deltaTime);
			}
		}	
	}	
	
	private bool NeedsFixing()
	{
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		return transform.rotation.eulerAngles.z != orientation.degree;
	}

	private bool NeedsRotation()
	{
		return !Mathf.Approximately(transform.rotation.eulerAngles.z, orientation.degree);
	}

	private void Rotate(bool clockwise)
	{
		orientation = clockwise ? orientation.Next() : orientation.Previous();
		Debug.Log($"Orientation is now {orientation}");
	}
}
