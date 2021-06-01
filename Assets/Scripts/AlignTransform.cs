using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignTransform : MonoBehaviour
{
    public Transform [] ikLegs;
    public Transform [] rays;
    public LayerMask groundLayer;

    public float stepDistance;

    private bool legs_moving;
    private Vector3[] legsLastPosition;
    // Start is called before the first frame update
    void Start()
    {
        legs_moving = true;
        legsLastPosition = new Vector3[ikLegs.Length];
    }

    // Update is called once per frame
    void Update()
    {
        restingPosition();
    }

    void FixedUpdate() {
        movingLegs();
    }

    
    private void restingPosition() {
        if(legs_moving) return;
        for (int i = 0; i < ikLegs.Length; i++)
        {
            ikLegs[i].transform.position = legsLastPosition[i];
        }
    }

    private void movingLegs(){
        for (int i = 0; i < ikLegs.Length; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(rays[i].position, rays[i].TransformDirection(-Vector3.forward), out hit, Mathf.Infinity, groundLayer)){
                Debug.Log("Ground hitted for Raycast " + i);
            }
            float distance = Vector3.Distance(hit.point, ikLegs[i].transform.position);
            if(distance > stepDistance) legs_moving = true;
            if(legs_moving){
                ikLegs[i].transform.position = hit.point;
                legsLastPosition[i] = ikLegs[i].transform.position;
                legs_moving = false;
            }
        }
    }
    
}
