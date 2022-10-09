using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetParentSetterTarget : MonoBehaviour
{
	public IDTarget TargetType;

	public int intID;


	[Tooltip(" Use this when you have a Scrolling rect , And it has a tiny hit box, This means that you can use the viewport instead, Only works if there's one child ")]
	public bool SetParentBelow;

    public enum IDTarget
    {
	    DNASequence,
	    DNATarget
    }
}
