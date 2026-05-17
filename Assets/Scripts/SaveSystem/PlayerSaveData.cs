using System;
using UnityEngine;

[Serializable]
public class PlayerSaveData
{
    public string sceneName;
    public string zoneID;

    public float positionX;
    public float positionY;
    public float positionZ;

    public float rotationX;
    public float rotationY;
    public float rotationZ;

    public void Capture(Transform playerTransform, string currentSceneName, string currentZoneID)
    {
        sceneName = currentSceneName;
        zoneID = currentZoneID;

        Vector3 position = playerTransform.position;
        Vector3 rotation = playerTransform.eulerAngles;

        positionX = position.x;
        positionY = position.y;
        positionZ = position.z;

        rotationX = rotation.x;
        rotationY = rotation.y;
        rotationZ = rotation.z;
    }

    public Vector3 GetPosition()
    {
        return new Vector3(positionX, positionY, positionZ);
    }

    public Quaternion GetRotation()
    {
        return Quaternion.Euler(rotationX, rotationY, rotationZ);
    }
}