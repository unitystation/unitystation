using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class GameMessage<T> : GameMessageBase
{
    public static readonly short MessageType;

    static GameMessage()
    {
        // Each message needs to have a unique MessageType, defined as a short int.
        // Rather than have people manually define them and risk somebody using the
        // same number for two different messages, this constructor will automatically
        // assign the message types for you. Normally, static fields in C# classes are
        // not copied to derived classes, however, if you add a generic type parameter,
        // each type will get its own copy of the static field. So we add a type parameter
        // to GameMessage<T> and simply pass in the derived class as the parameter.
        // It's a hack, but it effectively turns a forgetful programmer mistake from being 
        // a runtime error into being a compile-time error.
        MessageType = GameMessageBase.msgTypeCounter++;
    }

    public abstract IEnumerator Process();
}
