using UnityEngine;
public class Leg
{
    public bool isMoving;
    public float distanceFromRest;
    public Vector3 targetPosition;
    public Vector3 rayHitPosition;
    public Vector3 rayHitNormal;
    public Vector3 lastPosition;
    public Vector3 lastPositionNormal;
    public SET set;
    public Leg(Vector3 lastPoint, SET setOfLegs){
        isMoving = false;
        distanceFromRest = 0;
        lastPosition = lastPoint;
        set = setOfLegs;
    }
}

public enum SET {
    First, Second
}
