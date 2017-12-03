using InputControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireAttackController : InputTrigger
{
    public override void Interact(GameObject originator, string hand)
    {
        //if no glubs
            //zap him
        //if object == null
            //return
        //if attackby wirecutters
            //delete self 
            //visible message: wires are snibbedy snabd
            //play audio: snip
            //int wireCount = 1;
            //if(DirectionEnd != 0)
                //the wire is two long
                //wireCount++
            //check floor for wires
            //if wire exists
                //add wireCount to it
                //assume that the stack object handles that perfectly
                //return
            //make a new wire stack object
            //set the count to wireCount
            //return
        //if attackby multitool
            //if power network
                //handle power diagnostics
                //ss13 prints some message saying how much W is in the cable
                //may want to do something more fancy? idk
            //print something to the user: "no current on this wire, sorry mang"
        //if attackby wire stack object
            //if DirectionEnd != 0
                //int newDirectionEnd = relative direction of the attacking mob
                //check the floor for wires
                //for (StructurePowerWire wire in List)
                    //if(wire.DirectionStart == DirectionStart && wire.DirectionEnd == newDirectionEnd)
                        //print to mob: "there's already a wire there"
                        //return
                //reduce wire stack object's count by 1
                //Color = wire stack object's color
                //setDirection(DirectionStart, newDirectionEnd)
                //return
    }
}
