using UnityEngine;
public class Leg
{
    public bool isMoving;
    //public Transform ik;
    //public Transform rayOrigin;

    public Vector3 targetPosition;
    public Vector3 rayHitPosition;
    public Vector3 rayHitNormal;
    public Vector3 lastPosition;
    public Vector3 lastPositionNormal;
    public SET set;


    // Transform ikPoint, Transform rayPoint, 
    public Leg(Vector3 lastPoint, SET setOfLegs){
        isMoving = false;
        //ik = ikPoint;
        //rayOrigin = rayPoint;
        lastPosition = lastPoint;
        set = setOfLegs;
    }
}

public enum SET {
    First, Second
}
