using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;
using UnityEngine;

public class ProceduralAnimation : MonoBehaviour
{
    //Public variables
    public MovementController movementController;
    public LayerMask groundLayer;
    public GameObject body, raysGameobject;
    public float stepDistance = 10f;
    public float stepMaxHeight = 4.5f;
    public float timeToTakeStep_ms = 320f;
    public float StepSpeed_ms = 1000f;
    public float stepFrameTime = 1/60f;

    public float[] curveData; 

    [Header("Body rotation and position adjust ratio")]
    public float AdjustRatioPerTick = 1 / 10.0f;

    [Header("Step Curves")]
    [SerializeField] private AnimationCurve speedCurve;
    [SerializeField] private AnimationCurve heightCurve;

    //Private Variables
    private Transform [] ikLegs, rays;
    private Leg [] legs;
    private float bodyHeightBase = 10.85f;
    private Animator ch_controller;
    private TrailRenderer _trail;
    private float ikPassOver = 0.2f;   
    private int movingIndex, nLegs;
    private int[] firstSetLegs, secondSetLegs;
    private int stepIndex = 0;

    static private Keyframe[] DEFAULT_SPEED_STEP = {new Keyframe(0f, 0f), new Keyframe(1f, 1f)};
    static private Keyframe[] DEFAULT_HEIGHT_STEP = {new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f)};

    static private Keyframe[] STOMP_SPEED_STEP = {new Keyframe(0f, 0f), new Keyframe(0.15f, 0.42f), new Keyframe(0.78f, 0.66f), new Keyframe(1f, 1f)};
    static private Keyframe[] STOMP_HEIGHT_STEP = {new Keyframe(0f, 0f), new Keyframe(0.3f, 1.8f), new Keyframe(0.7f, 1.8f), new Keyframe(1f, 0f)};

    static private Keyframe[] STEALTH_SPEED_STEP = {new Keyframe(0f, 0f), new Keyframe(0.32f, 0.14f), new Keyframe(0.672f, 0.89f), new Keyframe(1f, 1f)};
    static private Keyframe[] STEALTH_HEIGHT_STEP = {new Keyframe(0f, 0f), new Keyframe(0.19f, 1.4f), new Keyframe(1f, 0f)};


    // Start is called before the first frame update
    void Start()
    {   
        f_InitializeLegs();
        ch_controller = transform.GetComponent<Animator>();
        StartCoroutine(bodyProceduraltransform());
    }

    // Update is called once per frame
    void Update()
    {
        if(body.gameObject.GetComponent<MovementController>().pause) return;
        if (Input.GetKeyDown(KeyCode.T)) _trail.enabled = !_trail.enabled;

        if (Input.GetKeyDown(KeyCode.K)) {
            Keyframe[] keys = new Keyframe[curveData.Length];
            float frame = 0;
            float betweenFrames = 1f / (curveData.Length - 1);
            print(betweenFrames);
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = new Keyframe(frame, curveData[i]);
                frame += betweenFrames;
                //if (i == keys.Length -1) frame = 1;
                //else frame += betweenFrames;
            }
            heightCurve = new AnimationCurve(keys);
        }
        f_RestingPosition();
    }

    void FixedUpdate() {
        if(body.gameObject.GetComponent<MovementController>().pause) return;
       f_MovingLegsDual();
    }
    private void f_InitializeLegs(){
        Transform[] raysInTake = raysGameobject.GetComponentsInChildren<Transform>();
        TwoBoneIKConstraint[] iks = GetComponentsInChildren<TwoBoneIKConstraint>();
        nLegs = iks.Length;
        ikLegs = new Transform[nLegs];
        rays = new Transform[nLegs];
        for (int i = 0; i < nLegs; i++)
        {
            ikLegs[i] = iks[i].transform;
            rays[i] = raysInTake[i + 1];
        }
        ikLegs[0].gameObject.AddComponent<TrailRenderer>();
        _trail = ikLegs[0].gameObject.GetComponent<TrailRenderer>();
        _trail.enabled = false;

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
        Keyframe[] speedCurveAux = DEFAULT_SPEED_STEP;
        Keyframe[] heightCurveAux = DEFAULT_HEIGHT_STEP;
       switch (preset){
           case "Default": print("Default Clicked");
                speedCurveAux = DEFAULT_SPEED_STEP;
                heightCurveAux = DEFAULT_HEIGHT_STEP;
                break;
           case "Stomp": print("Stomp Clicked"); 
                speedCurveAux = STOMP_SPEED_STEP;
                heightCurveAux = STOMP_HEIGHT_STEP;
                break;
           case "Stealth": print("Stealth Clicked");
                speedCurveAux = STEALTH_SPEED_STEP;
                heightCurveAux = STEALTH_HEIGHT_STEP;
                break;
           case "Own": print("Own Clicked");
                speedCurveAux = DEFAULT_SPEED_STEP;
                heightCurveAux = DEFAULT_HEIGHT_STEP;
                break;
           default: 
                break;
       }

        speedCurve = new AnimationCurve(speedCurveAux);
        heightCurve = new AnimationCurve(heightCurveAux);
       
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

    private void f_MovingLegsDual(){ // One set at a time
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

    private IEnumerator bodyProceduraltransform(){
        while (true){
            Vector3 avgIkPositon = Vector3.zero;
            Vector3 bodyVecUp = Vector3.zero;

            for (int i = 0; i < nLegs; i++)
            {
                avgIkPositon += ikLegs[i].position;
                bodyVecUp += legs[i].lastPositionNormal + legs[i].rayHitNormal;
            }

            RaycastHit hit;
            if (Physics.Raycast(body.transform.position, body.transform.up * -1, out hit, Mathf.Infinity))
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

            yield return new WaitForFixedUpdate();
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

    private void f_MovingLegsHomogeneus(){ // First One set, the the other
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
    private void f_MovingLegs(){ // Index order
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
            // bool perfomStep = true;
            // print(legs[movingIndex].set);
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
            // if(perfomStep){
            //     StartCoroutine(Step(movingIndex, legs[movingIndex].targetPosition));
            // } 

            int idxAnalogLeg = movingIndex - nLegs/2 < 0 ? nLegs - Mathf.Abs(movingIndex - nLegs/2) : movingIndex - nLegs/2;
            if(!legs[idxAnalogLeg].isMoving) {
                StartCoroutine(Step(movingIndex, legs[movingIndex].targetPosition));
            }
        }
    }
 */
}
