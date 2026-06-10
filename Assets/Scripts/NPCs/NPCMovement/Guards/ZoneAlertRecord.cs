using System;
using UnityEngine;

[Serializable]
public class ZoneAlertRecord
{
    public string zoneID;
    public ZoneAlertLevel alertLevel;

    [Range(0f, 100f)]
    public float alertValue;

    public float timeSinceLastIncrease;
}