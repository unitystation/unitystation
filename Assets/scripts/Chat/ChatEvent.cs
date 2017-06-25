using System;
using UnityEngine;

public struct ChatEvent
{
    public double timestamp;
    public string message;

    public ChatEvent(double timestamp, string message)
    {
        this.timestamp = timestamp;
        this.message = message;
    }

    public ChatEvent(string message)
    {
        this.timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        this.message = message;
    }
}