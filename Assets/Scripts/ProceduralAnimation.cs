using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;
using UnityEngine;

public class ProceduralAnimation : MonoBehaviour
{    
    static private Keyframe[] DEFAULT_SPEED_STEP = {new Keyframe(0f, 0f), new Keyframe(1f, 1f)};
    static private Keyframe[] DEFAULT_HEIGHT_STEP = {new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f)};

    static private Keyframe[] STOMP_SPEED_STEP = {new Keyframe(0f, 0f), new Keyframe(0.15f, 0.42f), new Keyframe(0.78f, 0.66f), new Keyframe(1f, 1f)};
    static private Keyframe[] STOMP_HEIGHT_STEP = {new Keyframe(0f, 0f), new Keyframe(0.3f, 1.8f), new Keyframe(0.7f, 1.8f), new Keyframe(1f, 0f)};

    static private Keyframe[] STEALTH_SPEED_STEP = {new Keyframe(0f, 0f), new Keyframe(0.32f, 0.14f), new Keyframe(0.672f, 0.89f), new Keyframe(1f, 1f)};
    static private Keyframe[] STEALTH_HEIGHT_STEP = {new Keyframe(0f, 0f), new Keyframe(0.19f, 1.4f), new Keyframe(1f, 0f)};
    

    //Public variables
    public GameObject legPrefab;
    public Controller movementController;
    public LayerMask groundLayer;
    public GameObject body, raysGameobject;
    public Transform[] spawnLegsPoints;
    public float stepDistance = 10f;
    public float stepMaxHeight = 4.5f;
    public float timeToTakeStep_ms = 320f;
    public float StepSpeed_ms = 1000f;
    public float stepFrameTime = 1/60f;

    [Header("Body rotation and position adjust ratio")]
    public float AdjustRatioPerTick = 1 / 10.0f;

    [Header("Step Curves")]
    public AnimationCurve speedCurve;
    public AnimationCurve heightCurve;
    [HideInInspector] public string movementVariant;
    [HideInInspector] public int numberOfLegs;
    [HideInInspector] public int nLegs;

    //Private Variables
    private Keyframe[] myCycle_speed_step, myCycle_height_step;
    private Transform [] ikLegs, rays;
    private Leg [] legs;
    private float bodyHeightBase = 10.9f;
    private Animator ch_controller;
    [HideInInspector] public TrailRenderer _trail;
    private float ikPassOver = 0.2f;   
    private int movingIndex;
    private int[] firstSetLegs, secondSetLegs;
    private int stepIndex = 0;

    // Start is called before the first frame update
    void Start()
    {   
        movementVariant = "variant1";
        f_SetLegs(8);
        ch_controller = transform.GetComponent<Animator>();
        StartCoroutine(bodyProceduraltransform());
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Y)) f_setNumberOfLegs(true);
        if (Input.GetKeyDown(KeyCode.H)) f_setNumberOfLegs(false);
        if (Input.GetKeyDown(KeyCode.L)) {
            f_InitializeLegs();
            movementVariant = "variant1";
        }
        if(body.gameObject.GetComponent<Controller>().pause) return;
        if (Input.GetKeyDown(KeyCode.T)) _trail.enabled = !_trail.enabled;

        if(!movementVariant.Equals("stop"))f_RestingPosition();
    }

    void FixedUpdate() {
        if(body.gameObject.GetComponent<Controller>().pause) return;
        switch(movementVariant){
            case "variant1": f_MovingLegsVariant1(); break;
            case "variant2": f_MovingLegsVariant2(); break;
            case "variant3": f_MovingLegsVariant3(); break;
            case "variant4": f_MovingLegsVariant4(); break;
            default: break;
        }
        
    }
    public void f_reInitializeLegs(){
        f_InitializeLegs();
        movementVariant = "variant1";
    }

    public int f_setNumberOfLegs(bool add){
        numberOfLegs = -1;
        if(add && nLegs < 8) {
            numberOfLegs = nLegs + 2;
        }
        else if(!add && nLegs > 2){
            numberOfLegs = nLegs - 2;
        }

        if(numberOfLegs == -1) return numberOfLegs;

        f_SetLegs(numberOfLegs);
        return numberOfLegs;
    }

    private void f_SetLegs(int numberOfLegs){
        string aux = movementVariant;
        movementVariant = "stop";
        
        RigBuilder[] old_legs_points = GetComponentsInChildren<RigBuilder>();
        GameObject[] old_legs = new GameObject[nLegs];

        for (int i = 0; i < old_legs_points.Length; i++)
        {
            GameObject.Destroy(old_legs_points[i].gameObject);
        }

        for (int i = 0; i < spawnLegsPoints.Length; i++)
        {
            print(spawnLegsPoints[i].gameObject.name);
        }

        int j = 0;
        GameObject auxLeg;
        for (int i = 0; i < numberOfLegs; i++)
        {
            if(i > numberOfLegs/2 - 1){
                auxLeg = Instantiate(legPrefab, spawnLegsPoints[j + 8/2]);
                j++;
                //auxLeg.transform.SetParent(spawnLegsPoints[j + 1 + 8/2], false);
            }
            else{
                auxLeg = Instantiate(legPrefab, spawnLegsPoints[i]);
                //auxLeg.transform.SetParent(spawnLegsPoints[i + 1], false);
            }
        }
        Invoke("f_reInitializeLegs", 0.2f);
    }

    private void f_InitializeLegs(){
        //Obtain Components via code
        Transform[] raysInTake = raysGameobject.GetComponentsInChildren<Transform>();
        TwoBoneIKConstraint[] iks = GetComponentsInChildren<TwoBoneIKConstraint>();
        nLegs = iks.Length;
        ikLegs = new Transform[nLegs];
        rays = new Transform[nLegs];
        int x = 0;
        print(nLegs);
        for (int i = 0; i < nLegs; i++)
        {
            ikLegs[i] = iks[i].transform;
            if(i > nLegs/2 - 1){
                rays[i] = raysInTake[x + 1 + 8/2];
                x++;
            }
            else{
                rays[i] = raysInTake[i + 1]; // The zero position belongs to the perent, so we must omit it
            }
            
        }
        ikLegs[0].gameObject.AddComponent<TrailRenderer>();
        _trail = ikLegs[0].gameObject.GetComponent<TrailRenderer>();
        _trail.enabled = false;

        //Initialize Data
        legs = new Leg[nLegs];
        movingIndex = -1;

        firstSetLegs = new int[nLegs/2];
        secondSetLegs = new int[nLegs/2];
        for (int i = 0; i < nLegs/2; i++)
        {
            firstSetLegs[i] = i%2 == 0 ? i : nLegs/2 + i;
            secondSetLegs[i] = i%2 == 0 ? nLegs/2 + i : i;
        }

        for (int i = 0; i < nLegs; i++)
        {
            //Put IKs place 
            RaycastHit hit;
            if (Physics.Raycast(rays[i].position, rays[i].TransformDirection(-Vector3.forward), out hit, Mathf.Infinity, groundLayer)){
                ikLegs[i].position = hit.point;
            }

            for (int j = 0; j < nLegs/2; j++)
            {
                if ( i == firstSetLegs[j]){
                    legs[i] = new Leg(ikLegs[i].position, SET.First);
                }
                else if ( i == secondSetLegs[j]) {
                   legs[i] = new Leg(ikLegs[i].position, SET.Second);
                } 
            }
        }
    }

    public void f_ChoosePreset(string preset) {
        Keyframe[] speedCurveKeyframes = new Keyframe[0];
        Keyframe[] heightCurveKeyframes = new Keyframe[0];
       switch (preset){
           case "Default":
                speedCurveKeyframes = DEFAULT_SPEED_STEP;
                heightCurveKeyframes = DEFAULT_HEIGHT_STEP;
                break;
           case "Stomp": 
                speedCurveKeyframes = STOMP_SPEED_STEP;
                heightCurveKeyframes = STOMP_HEIGHT_STEP;
                break;
           case "Stealth":
                speedCurveKeyframes = STEALTH_SPEED_STEP;
                heightCurveKeyframes = STEALTH_HEIGHT_STEP;
                break;
           case "Own":
                speedCurveKeyframes = myCycle_speed_step;
                heightCurveKeyframes = myCycle_height_step;
                break;
           default: 
                break;
       }

        speedCurve = speedCurveKeyframes.Length <= 0? speedCurve : new AnimationCurve(speedCurveKeyframes);
        heightCurve = heightCurveKeyframes.Length <= 0? heightCurve : new AnimationCurve(heightCurveKeyframes);
    }

    private void f_RestingPosition() {        
        for (int i = 0; i < nLegs; i++)
        {
            if(i != movingIndex && !legs[i].isMoving){
                ikLegs[i].transform.position = legs[i].lastPosition;
            } 
        }
    }

    IEnumerator Step(int index, Vector3 target){
        legs[index].isMoving = true;

        float elapsedTime = 0f;
        float animTime;

        Vector3 startPos = legs[index].lastPosition;
        Vector3 ikDirVec = legs[index].rayHitPosition - startPos;
        ikDirVec += ikDirVec.normalized * ikPassOver;

        Vector3 right = Vector3.Cross(body.transform.up, ikDirVec.normalized).normalized;
        legs[index].lastPositionNormal = Vector3.Cross(ikDirVec.normalized, right);


        while(elapsedTime < timeToTakeStep_ms)
        {
            animTime = speedCurve.Evaluate(elapsedTime / timeToTakeStep_ms);

            float ikAcceleration = Mathf.Max((legs[index].rayHitPosition - startPos).magnitude / ikDirVec.magnitude, 1f);

            legs[index].lastPosition = startPos + ikDirVec * ikAcceleration * animTime;
            legs[index].lastPosition += legs[index].lastPositionNormal * heightCurve.Evaluate(animTime) * stepMaxHeight;

            ikLegs[index].position = legs[index].lastPosition;
            // ikLegs[index].rotation = Quaternion.LookRotation(legs[index].lastPosition - ikLegs[index].position) * Quaternion.Euler(90, 0 ,0);

            elapsedTime += stepFrameTime * StepSpeed_ms;

            yield return new WaitForSeconds(stepFrameTime);
        }

        ikLegs[index].position = legs[index].rayHitPosition;
        legs[index].lastPosition = ikLegs[index].position;
        legs[index].isMoving = false;
    }

    private IEnumerator bodyProceduraltransform(){
        while (true){
            if(!movementVariant.Equals("stop")){ 

                Vector3 avgIkPositon = Vector3.zero;
                Vector3 bodyVecUp = Vector3.zero;

                for (int i = 0; i < nLegs; i++)
                {
                    avgIkPositon += ikLegs[i].position;
                    bodyVecUp += legs[i].lastPositionNormal + legs[i].rayHitNormal;
                }

                RaycastHit hit;
                if (Physics.Raycast(body.transform.position, body.transform.up * -1, out hit, Mathf.Infinity, groundLayer))
                {
                    bodyVecUp += hit.normal;
                }

                avgIkPositon = avgIkPositon / nLegs;
                bodyVecUp.Normalize();

                Vector3 bodyPos = avgIkPositon + bodyVecUp * bodyHeightBase;
                body.transform.position = Vector3.Lerp(body.transform.position, new Vector3(body.transform.position.x,bodyPos.y,body.transform.position.z), AdjustRatioPerTick);

                Vector3 bodyRight = Vector3.Cross(bodyVecUp, body.transform.forward);
                Vector3 bodyForward = Vector3.Cross(bodyRight, bodyVecUp);

                Quaternion bodyRotation = Quaternion.LookRotation(bodyForward, bodyVecUp);
                body.transform.rotation = Quaternion.Slerp(body.transform.rotation, bodyRotation, AdjustRatioPerTick);

            }
        
            yield return new WaitForFixedUpdate();
        }
    }

    public void f_setCurves(Keyframe[] speed, Keyframe[] height){
        myCycle_speed_step = speed;
        myCycle_height_step = height;
    }

/// <summary>
/// Move legs divided by sets
/// </summary>
    private void f_MovingLegsVariant1(){ // One set at a time
        movingIndex = -1;
        stepIndex = stepIndex == 0 ? nLegs/2 : 0;

        for (int i = 0; i < nLegs; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(rays[i].position, rays[i].TransformDirection(-Vector3.forward), out hit, Mathf.Infinity, groundLayer)){
                float distance = Vector3.Distance(hit.point, ikLegs[i].transform.position);
                legs[i].rayHitPosition = hit.point;
                legs[i].rayHitNormal = hit.normal;
                if(distance > stepDistance && i == stepIndex) {
                    legs[i].targetPosition = hit.point;
                    movingIndex = i;
                }
            }
        }

        if(movingIndex != -1 && !legs[movingIndex].isMoving){
            bool perfomStep = true;
            // for (int j = 0; j < nLegs/2; j++)
            // {
            //     if(legs[movingIndex].set == SET.First){
            //         if(legs[secondSetLegs[j]].isMoving){
            //             perfomStep = false;
            //             break;
            //         }
            //     }
            //     else{
            //         if(legs[firstSetLegs[j]].isMoving){
            //             perfomStep = false;
            //             break;
            //         }
            //     }
            // }
            int idxAnalogLeg = movingIndex - nLegs/2 < 0 ? nLegs - Mathf.Abs(movingIndex - nLegs/2) : movingIndex - nLegs/2;
            perfomStep = legs[idxAnalogLeg].isMoving ? false : true;
            if(perfomStep){
                legs[movingIndex].isMoving = true;
                StartCoroutine(Step(movingIndex, legs[movingIndex].targetPosition));
                if (legs[movingIndex].set == SET.First){
                    for (int j = 1; j < nLegs/2; j++)
                    {
                        StartCoroutine(Step(firstSetLegs[j], legs[firstSetLegs[j]].rayHitPosition));
                    }
                }
                else{
                    for (int j = 1; j < nLegs/2; j++)
                    {
                        StartCoroutine(Step(secondSetLegs[j], legs[secondSetLegs[j]].rayHitPosition));
                    }
                }
            } 
        }
    }

    /// <summary>
    /// Move all legs but only if the symmetrical leg is grounded
    /// </summary>
    private void f_MovingLegsVariant2(){ // First One set, the the other
        movingIndex = -1;
        for (int i = 0; i < nLegs/2; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(rays[firstSetLegs[i]].position, rays[firstSetLegs[i]].TransformDirection(-Vector3.forward), out hit, Mathf.Infinity, groundLayer)){
                // Debug.Log("Ground hitted for Raycast " + i);
                float distance = Vector3.Distance(hit.point, ikLegs[firstSetLegs[i]].transform.position);
                legs[firstSetLegs[i]].rayHitPosition = hit.point;
                if(distance > stepDistance) {
                    legs[firstSetLegs[i]].targetPosition = hit.point;
                    movingIndex = firstSetLegs[i];
                }
            }
        }
        for (int i = 0; i < nLegs/2; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(rays[secondSetLegs[i]].position, rays[secondSetLegs[i]].TransformDirection(-Vector3.forward), out hit, Mathf.Infinity, groundLayer)){
                // Debug.Log("Ground hitted for Raycast " + i);
                float distance = Vector3.Distance(hit.point, ikLegs[secondSetLegs[i]].transform.position);
                legs[secondSetLegs[i]].rayHitPosition = hit.point;
                if(distance > stepDistance) {
                    legs[secondSetLegs[i]].targetPosition = hit.point;
                    movingIndex = secondSetLegs[i];
                }
            }
        }
        if(movingIndex != -1 && !legs[movingIndex].isMoving){
            int idxAnalogLeg = movingIndex - nLegs/2 < 0 ? nLegs - Mathf.Abs(movingIndex - nLegs/2) : movingIndex - nLegs/2;
            if(!legs[idxAnalogLeg].isMoving) {
                StartCoroutine(Step(movingIndex, legs[movingIndex].targetPosition));
            }
        }
    }

    /// <summary>
    /// Move legs only if all other legs are grounded
    /// </summary>
    private void f_MovingLegsVariant3(){ // Index order
        movingIndex = -1;
        for (int i = 0; i < nLegs; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(rays[i].position, rays[i].TransformDirection(-Vector3.forward), out hit, Mathf.Infinity, groundLayer)){
                // Debug.Log("Ground hitted for Raycast " + i);
                float distance = Vector3.Distance(hit.point, ikLegs[i].transform.position);
                legs[i].rayHitPosition = hit.point;
                if(distance > stepDistance) {
                    legs[i].targetPosition = hit.point;
                    movingIndex = i;
                }
            }
        }
        if(movingIndex != -1 && !legs[movingIndex].isMoving){
            bool performStep = true;

            for (int j = 0; j < legs.Length; j++)
            {
                if(legs[j].isMoving) {
                    performStep = false;

                }
            }
            
            if(performStep) {
                StartCoroutine(Step(movingIndex, legs[movingIndex].targetPosition));
            }
        }
    }

    /// <summary>
    /// Move legs always if they are too far away of the resting point
    /// </summary>
    private void f_MovingLegsVariant4(){ // Index order
        movingIndex = -1;
        for (int i = 0; i < nLegs; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(rays[i].position, rays[i].TransformDirection(-Vector3.forward), out hit, Mathf.Infinity, groundLayer)){
                // Debug.Log("Ground hitted for Raycast " + i);
                float distance = Vector3.Distance(hit.point, ikLegs[i].transform.position);
                legs[i].rayHitPosition = hit.point;
                if(distance > stepDistance) {
                    legs[i].targetPosition = hit.point;
                    movingIndex = i;
                }
            }
        }
        if(movingIndex != -1 && !legs[movingIndex].isMoving){
            StartCoroutine(Step(movingIndex, legs[movingIndex].targetPosition));
        }
    }

    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < nLegs; ++i)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(ikLegs[i].position, 0.05f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint(legs[i].lastPosition), stepDistance);

        }
    }

/*
    IEnumerator Step(int index, Vector3 target){
        legs[index].isMoving = true;

        Vector3 startPos = legs[index].lastPosition;
        float elapsedTime = 0f;

        while(elapsedTime < timeToTakeStep_ms){
            ikLegs[index].position = Vector3.Lerp(startPos, target, elapsedTime / timeToTakeStep_ms);
            ikLegs[index].position += body.transform.up * Mathf.Sin(elapsedTime / timeToTakeStep_ms * Mathf.PI) * stepMaxHeight;
            elapsedTime += Time.fixedDeltaTime * StepSpeed_ms;
            yield return null;
        }
        ikLegs[index].position = target;
        legs[index].lastPosition = ikLegs[index].position;
        legs[index].isMoving = false;
        f_BodyHeight();
    }
 */
}
