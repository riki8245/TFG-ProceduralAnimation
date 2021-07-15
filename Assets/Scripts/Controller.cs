using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//[RequireComponent(typeof(Rigidbody))]
public class Controller : MonoBehaviour
{
    //Public variables
    public GameObject canvas;
    public float _speed = 20f;
    public float mouseSensitivity = 150f;
    [HideInInspector] public bool pause = false;
    [HideInInspector] public int _nOfCoins;

   //Private Variables
    [Header("UI Editor Stuff")]
    [SerializeField] private GameObject speedEditor;
    [SerializeField] private GameObject heightEditor;
    [SerializeField] private GameObject speedPointsUIContainer;
    [SerializeField] private GameObject heightPointsUIContainer;
    [SerializeField] private Sprite pointSprite; 
    
    [Header("Limits speed curve")]
    [SerializeField] private Transform l_initialSpeed;
    [SerializeField] private Transform l_speed;
    [SerializeField] private Transform l_speedFrame;

    [Header("Limits height curve")]
    [SerializeField] private Transform l_initialHeight;
    [SerializeField] private Transform l_height;
    [SerializeField] private Transform l_heightFrame;
    [SerializeField] private Text trailText;
    [SerializeField] private Text nLegsText;
    [SerializeField] private Text nOfCoinsText;
    [SerializeField] private GameObject finishText;
 
    private string editingCurve;
    private GameObject view_speedCurveUI;
    private GameObject view_heightCurveUI;
    private AnimationCurve actualSpeedCurve;
    private AnimationCurve actualHeightCurve;
    private ProceduralAnimation proceduralAnimation;
    private Keyframe[] myHeightKeyframes, mySpeedKeyframes;
    private List<GameObject> heightPointsUI, speedPointsUI;
    private List<float[]>  key_heightPoints, key_speedPoints;
    private int editingIndex;
    private bool wasEdited;
    private bool firstClickInBoundaries;
    private string lastCurveEdited;
    private bool putNewPoint;
    private bool pointChangedBACK;
    private bool pointChangedFORWARD;
    private float lastEditMosuePoint;
    private Rigidbody _rigibody;
    private float axisHorizontal;
    private float axisVertical;
    private float mouseX;

    void Start()
    {
        _nOfCoins = 0;
        mySpeedKeyframes = new Keyframe[0];
        myHeightKeyframes = new Keyframe[0];
        editingIndex = -1;
        firstClickInBoundaries = false;
        lastCurveEdited = "";
        
        proceduralAnimation = GetComponentInChildren<ProceduralAnimation>();
        ShowCycle(proceduralAnimation.speedCurve, proceduralAnimation.heightCurve);
        //_rigibody = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) {
            f_OpenCloseMenu();
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if(pause && editingCurve != null) {
            if(Input.GetMouseButtonDown(0)) f_EditPoint(); // Select point if mouse click over one & deselect point if mouse click air
            if(Input.GetMouseButton(0)) f_movePoint(); // Move point clicked while holding mouse 
            if(Input.GetMouseButtonUp(0)) f_PutRayPoint(); // Create new point if there wasn't  selected point
        }
        else{
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            transform.Rotate(Vector3.up * mouseX);

            axisHorizontal = Input.GetAxis("Horizontal");
            axisVertical = Input.GetAxis("Vertical");
        }
    }

    private void f_OpenCloseMenu() {
        pause = !pause;
        canvas.SetActive(pause);
        if(pause){
            //proceduralAnimation.f_pauseMovement();
            wasEdited = false;
            ShowCycle(proceduralAnimation.speedCurve, proceduralAnimation.heightCurve);

            view_speedCurveUI = GameObject.Find("SpeedCurve");
            view_heightCurveUI = GameObject.Find("HeightCurve");
            Cursor.lockState = CursorLockMode.None;
        }
        else{
            //proceduralAnimation.f_resumeState();
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void FixedUpdate() {
        if(pause) return;
        f_Movement();
    }

    private void f_Movement() {
        float fwdMovement = axisVertical * _speed * Time.fixedDeltaTime;
        transform.Translate(0, 0, fwdMovement);

        float hozMovement = axisHorizontal * _speed * Time.fixedDeltaTime;
        transform.Translate(hozMovement, 0, 0);
        

        // // RigibodyMovement
        // if(_rigibody.velocity.magnitude < _speed){
        //     if(axisVertical != 0){
        //         Vector3 forwardMovement = transform.forward * axisVertical * Time.fixedDeltaTime * 1500f;
        //         _rigibody.AddForce(forwardMovement);
        //     } 
        //     if(axisHorizontal != 0){
        //         Vector3 horizontalMovement = transform.right * axisHorizontal * Time.fixedDeltaTime * 1500f;
        //         _rigibody.AddForce(horizontalMovement);
        //     } 
        // }
    }
    

    //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*- BUTTONS IN UI -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

    public void f_ChangeMovementVariant(string variant){
        proceduralAnimation.movementVariant = variant;
    }

    public void f_AddSubstractLegs(bool add){
        int nLegs = proceduralAnimation.f_setNumberOfLegs(add);
        if(nLegs != -1){
            nLegsText.text = nLegs.ToString();
        }
    }
    public void f_ShowTrail(){
        proceduralAnimation._trail.enabled = !proceduralAnimation._trail.enabled;
        trailText.text = proceduralAnimation._trail.enabled ? "On" : "Off";
    }
    public void f_selectWalkingCycle(string cycle) {
        stopEditing();

        if(cycle.Equals("Own")) {
            saveCurves();
            proceduralAnimation.f_setCurves(mySpeedKeyframes, myHeightKeyframes);
        }
        proceduralAnimation.f_ChoosePreset(cycle);

        ShowCycle(proceduralAnimation.speedCurve, proceduralAnimation.heightCurve);
    }

    public void f_EditMode(string curve) {
        editingIndex = -1;
        ShowCycle(actualSpeedCurve, actualHeightCurve);
        if(editingCurve != null && editingCurve.Equals(curve)){ // Vuelves a Clickar
            stopEditing();
            return;
        }

        editingCurve = curve;
        if(editingCurve.Equals("speed")) {
            speedEditor.SetActive(true);
            heightEditor.SetActive(false);
            view_heightCurveUI.GetComponent<Image>().color = Color.grey;
            view_speedCurveUI.GetComponent<Image>().color = Color.white;
        }
        else if(editingCurve.Equals("height")) {
            heightEditor.SetActive(true);
            speedEditor.SetActive(false);
            view_speedCurveUI.GetComponent<Image>().color = Color.grey;
            view_heightCurveUI.GetComponent<Image>().color = Color.white;
        }
        
    }

    private void stopEditing() {
        editingIndex = -1;
        editingCurve = null;
        speedEditor.SetActive(false);
        heightEditor.SetActive(false);
        view_heightCurveUI.GetComponent<Image>().color = Color.white;
        view_speedCurveUI.GetComponent<Image>().color = Color.white;
    }

    public void deletePoint(string curveClicked) {
        if(lastCurveEdited.Equals("speed") && lastCurveEdited.Equals(editingCurve) && lastCurveEdited.Equals(curveClicked) && editingIndex != -1){
            actualSpeedCurve.RemoveKey(editingIndex);
            editingIndex = -1;
        }
        if(lastCurveEdited.Equals("height") && lastCurveEdited.Equals(editingCurve) && lastCurveEdited.Equals(curveClicked) && editingIndex != -1){
            actualHeightCurve.RemoveKey(editingIndex);
            editingIndex = -1;
        }
        ShowCycle(actualSpeedCurve, actualHeightCurve);
    }

    //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*- END BUTTONS IN UI -*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

    private void saveCurves() { // Si alguna curva fue editaba guadamos la edicion como la curva actual
        if(wasEdited){
            wasEdited = false;
            if(actualHeightCurve.length > 0){
                myHeightKeyframes = new Keyframe[actualHeightCurve.length];

                for (int i = 0; i < actualHeightCurve.length; i++)
                {
                    myHeightKeyframes[i] = new Keyframe(actualHeightCurve[i].time, actualHeightCurve[i].value);
                }
            }
            if(actualSpeedCurve.length > 0){
                mySpeedKeyframes = new Keyframe[actualSpeedCurve.length];

                for (int i = 0; i < actualSpeedCurve.length; i++)
                {
                    mySpeedKeyframes[i] = new Keyframe(actualSpeedCurve[i].time, actualSpeedCurve[i].value);
                }
            }
        }
    }

    private void f_EditPoint() { // Mouse Click DOWN
        // Añadir Mover puntos y borrar puntos individuales
        if(editingCurve.Equals("")) return;
        firstClickInBoundaries = false;
        
        if(editingCurve.Equals("speed") && 
        ((l_initialSpeed.position.y - Input.mousePosition.y > 0 || Input.mousePosition.y - l_speed.position.y > 0)
        || (l_initialSpeed.position.x - Input.mousePosition.x > 0 || Input.mousePosition.x - l_speedFrame.position.x > 0))) return;
        else if(editingCurve.Equals("height") && 
        ((l_initialHeight.position.y - Input.mousePosition.y > 0 || Input.mousePosition.y - l_height.position.y > 0) 
        || (l_initialHeight.position.x - Input.mousePosition.x > 0 || Input.mousePosition.x - l_heightFrame.position.x > 0))) return;

        firstClickInBoundaries = true;
        wasEdited = true;
        lastCurveEdited = editingCurve;
        putNewPoint = true;
        pointChangedBACK = false;
        pointChangedFORWARD = false;
        float v_position = -1;
        float h_position = -1;
        float v_distance = -1;
        float h_distance = -1;

        if(editingCurve.Equals("speed")){
            editingIndex = -1;
            //print("speed");
            v_distance = l_speed.position.y - l_initialSpeed.position.y;
            h_distance = l_speedFrame.position.x - l_initialSpeed.position.x;

            h_position = (Mathf.Abs(l_initialSpeed.position.x - Input.mousePosition.x) / h_distance)*h_distance + 5;
            v_position = (Mathf.Abs(l_initialSpeed.position.y - Input.mousePosition.y) / v_distance)*v_distance + 5;
            //print("horizontal pos: " + h_position + ", vertical pos: " + v_position);

            for (int i = 1; i < actualSpeedCurve.length - 1; i++){
                if(Vector2.Distance(new Vector2(actualSpeedCurve[i].time * h_distance, actualSpeedCurve[i].value * v_distance), new Vector2(h_position, v_position)) < 40){
                    editingIndex = i;
                    putNewPoint = false;
                    break;
                }
            }
        }
        else if(editingCurve.Equals("height")){
            //print("height");
            editingIndex = -1;

            v_distance = l_height.position.y - l_initialHeight.position.y;
            h_distance = l_heightFrame.position.x - l_initialHeight.position.x;

            h_position = (Mathf.Abs(l_initialHeight.position.x - Input.mousePosition.x) / h_distance) * h_distance;
            v_position = (Mathf.Abs(l_initialHeight.position.y - Input.mousePosition.y) / v_distance) * v_distance;

            for (int i = 1; i < actualHeightCurve.length - 1; i++){
                if(Vector2.Distance(new Vector2(actualHeightCurve[i].time * h_distance, actualHeightCurve[i].value * v_distance / 2), new Vector2(h_position, v_position)) < 40){
                    editingIndex = i;
                    putNewPoint = false;
                    break;
                }
            }
        }
        //print(editingIndex);
    }

    private void f_movePoint(){ //Mouse Click HOLD
        if(editingCurve.Equals("") || !firstClickInBoundaries || editingIndex == -1) return;

        float v_position = -1;
        float h_position = -1;
        float v_distance = -1;
        float h_distance = -1;
        Keyframe[] realtime_Keyframes;

        if(editingCurve.Equals("speed")){
            realtime_Keyframes = new Keyframe[actualSpeedCurve.length];

            for (int i = 0; i < realtime_Keyframes.Length; i++)
            {
                realtime_Keyframes[i] = new Keyframe(actualSpeedCurve[i].time, actualSpeedCurve[i].value);
            }

            v_distance = l_speed.position.y - l_initialSpeed.position.y;
            h_distance = l_speedFrame.position.x - l_initialSpeed.position.x;

            h_position = Mathf.Abs(l_initialSpeed.position.x - Input.mousePosition.x) / h_distance;
            v_position = Mathf.Abs(l_initialSpeed.position.y - Input.mousePosition.y) / v_distance;

            if(pointChangedBACK){
                pointChangedBACK = h_position * h_distance < lastEditMosuePoint - 4 ? false : true;
            }
            if(pointChangedFORWARD){
                pointChangedFORWARD = h_position * h_distance > lastEditMosuePoint + 4 ? false : true;
            }

            if((l_initialSpeed.position.y - Input.mousePosition.y > 0 || Input.mousePosition.y - l_speed.position.y > 0)
            || (l_initialSpeed.position.x - Input.mousePosition.x > 0 || Input.mousePosition.x - l_speedFrame.position.x > 0)) {
                editingIndex = -1;
                ShowCycle(actualSpeedCurve, actualHeightCurve);
                return;
            }
            if(pointChangedBACK || pointChangedFORWARD){
                ShowCycle(actualSpeedCurve, actualHeightCurve);
                return;
            }

            for (int i = 0; i < realtime_Keyframes.Length; i++)
            {
                if(editingIndex == i && (!pointChangedFORWARD && !pointChangedBACK)){

                    if(Input.GetAxis("Mouse X") < -0.000000001f && i - 1 != 0 && 
                    Mathf.Abs(realtime_Keyframes[i - 1].time * h_distance - h_position * h_distance) < 4f){
                        realtime_Keyframes[i] = realtime_Keyframes[i - 1];
  
                        editingIndex = i - 1;
                        lastEditMosuePoint = realtime_Keyframes[i].time * h_distance;
                        pointChangedBACK = true;
    
                    }
                    if(Input.GetAxis("Mouse X") > 0.000000001f && i + 1 != realtime_Keyframes.Length - 1 && 
                    Mathf.Abs(realtime_Keyframes[i + 1].time * h_distance - h_position * h_distance) < 4f){
                        realtime_Keyframes[i] = realtime_Keyframes[i + 1];

                        editingIndex = i + 1;
                        lastEditMosuePoint = realtime_Keyframes[i].time * h_distance;
                        pointChangedFORWARD = true;
                    }
                    
                    if (i == editingIndex) realtime_Keyframes[editingIndex] = new Keyframe(h_position, v_position);
                }
            }
            actualSpeedCurve = new AnimationCurve(realtime_Keyframes);
        }
        else if(editingCurve.Equals("height")){
            realtime_Keyframes = new Keyframe[actualHeightCurve.length];

            for (int i = 0; i < realtime_Keyframes.Length; i++)
            {
                realtime_Keyframes[i] = new Keyframe(actualHeightCurve[i].time, actualHeightCurve[i].value);
            }

            v_distance = l_height.position.y - l_initialHeight.position.y;
            h_distance = l_heightFrame.position.x - l_initialHeight.position.x;

            h_position = Mathf.Abs(l_initialHeight.position.x - Input.mousePosition.x) / h_distance;
            v_position = Mathf.Abs(l_initialHeight.position.y - Input.mousePosition.y) / v_distance;

            if(pointChangedBACK){
                pointChangedBACK = h_position * h_distance < lastEditMosuePoint - 4 ? false : true;
            }
            if(pointChangedFORWARD){
                pointChangedFORWARD = h_position * h_distance > lastEditMosuePoint + 4 ? false : true;
            }

            if((l_initialHeight.position.y - Input.mousePosition.y > 0 || Input.mousePosition.y - l_height.position.y > 0) 
            || (l_initialHeight.position.x - Input.mousePosition.x > 0 || Input.mousePosition.x - l_heightFrame.position.x > 0)) {
                editingIndex = -1;
                ShowCycle(actualSpeedCurve, actualHeightCurve);
                return;
            }

            if(pointChangedBACK || pointChangedFORWARD) {
                ShowCycle(actualSpeedCurve, actualHeightCurve);
                return;
            }

            
            for (int i = 0; i < realtime_Keyframes.Length; i++)
            {
                if(editingIndex == i && (!pointChangedFORWARD && !pointChangedBACK)){

                    if(Input.GetAxis("Mouse X") < -0.000000001f && i - 1 != 0 && 
                    Mathf.Abs(realtime_Keyframes[i - 1].time * h_distance - h_position * h_distance) < 4f){
                        realtime_Keyframes[i] = realtime_Keyframes[i - 1];
  
                        editingIndex = i - 1;
                        lastEditMosuePoint = realtime_Keyframes[i].time * h_distance;
                        pointChangedBACK = true;
    
                    }
                    if(Input.GetAxis("Mouse X") > 0.000000001f && i + 1 != realtime_Keyframes.Length - 1 && 
                    Mathf.Abs(realtime_Keyframes[i + 1].time * h_distance - h_position * h_distance) < 4f){
                        realtime_Keyframes[i] = realtime_Keyframes[i + 1];

                        editingIndex = i + 1;
                        lastEditMosuePoint = realtime_Keyframes[i].time * h_distance;
                        pointChangedFORWARD = true;
                    }
                    
                    if (i == editingIndex) realtime_Keyframes[editingIndex] = new Keyframe(h_position, v_position*2);

                }
            }
            actualHeightCurve = new AnimationCurve(realtime_Keyframes);
        }

        ShowCycle(actualSpeedCurve, actualHeightCurve);
    }

    private void f_PutRayPoint(){ // Mouse Click UP
        if (editingCurve.Equals("") || !firstClickInBoundaries || !putNewPoint) return;

        float v_position = -1;
        float h_position = -1;
        float v_distance = -1;
        float h_distance = -1;
        //GameObject pointAux;

        if(editingCurve.Equals("speed")){
            // If I'm out of limits return
            if((l_initialSpeed.position.y - Input.mousePosition.y > 0 || Input.mousePosition.y - l_speed.position.y > 0)
            || (l_initialSpeed.position.x - Input.mousePosition.x > 0 || Input.mousePosition.x - l_speedFrame.position.x > 0)) return;

            v_distance = l_speed.position.y - l_initialSpeed.position.y;
            h_distance = l_speedFrame.position.x - l_initialSpeed.position.x;

            h_position = Mathf.Abs(l_initialSpeed.position.x - Input.mousePosition.x) / h_distance;
            v_position = Mathf.Abs(l_initialSpeed.position.y - Input.mousePosition.y) / v_distance;
            
            actualSpeedCurve.AddKey(h_position, v_position);

        }
        else if(editingCurve.Equals("height")){
            // If I'm out of limits return
            if((l_initialHeight.position.y - Input.mousePosition.y > 0 || Input.mousePosition.y - l_height.position.y > 0) 
            || (l_initialHeight.position.x - Input.mousePosition.x > 0 || Input.mousePosition.x - l_heightFrame.position.x > 0)) return;

            v_distance = l_height.position.y - l_initialHeight.position.y;
            h_distance = l_heightFrame.position.x - l_initialHeight.position.x;

            h_position = Mathf.Abs(l_initialHeight.position.x - Input.mousePosition.x) / h_distance;
            v_position = Mathf.Abs(l_initialHeight.position.y - Input.mousePosition.y) / v_distance;

            actualHeightCurve.AddKey(h_position, v_position*2);
        }
        ShowCycle(actualSpeedCurve, actualHeightCurve);
    }

    private GameObject CreatePoint(Vector2 anchoredPosition, Transform graphContainer){
        GameObject pointObject = new GameObject("circle", typeof(Image));
        pointObject.transform.SetParent(graphContainer, false);
        pointObject.GetComponent<Image>().sprite = pointSprite;
        pointObject.GetComponent<Image>().color = Color.black;
        RectTransform rectTransform = pointObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(anchoredPosition.x + 5, anchoredPosition.y + 5);
        rectTransform.sizeDelta = new Vector2(20, 20);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);

        return pointObject;
    }

    private void CreatePointConnections(Vector2 pointA, Vector2 pointB, Transform graphContainer){
        GameObject lineObject = new GameObject("dotConnection", typeof(Image));
        lineObject.transform.SetParent(graphContainer, false);
        lineObject.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

        RectTransform rectTransform = lineObject.GetComponent<RectTransform>();
        Vector2 dir = (pointB - pointA).normalized;
        float distance = Vector2.Distance(pointA, pointB);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(distance, 3f);
        rectTransform.anchoredPosition = (pointA + pointB) * 0.5f;
        rectTransform.localEulerAngles = new Vector3 (0, 0, Mathf.Atan2(dir.y, dir.x)*Mathf.Rad2Deg);
    }

    private void ShowCycle(AnimationCurve selectedCycle_speed, AnimationCurve selectedCycle_height){
        actualSpeedCurve = selectedCycle_speed;
        actualHeightCurve = selectedCycle_height;
        ClearGraphs();

        GameObject lastPoint = null;
        float v_distance = l_speed.position.y - l_initialSpeed.position.y;
        float h_distance = l_speedFrame.position.x - l_initialSpeed.position.x;


        for (int i = 0; i < selectedCycle_speed.length; i++)
        {
            GameObject actualpoint = CreatePoint(new Vector2(selectedCycle_speed[i].time * h_distance, selectedCycle_speed[i].value * v_distance), speedPointsUIContainer.transform);
            if(lastCurveEdited.Equals("speed") && editingIndex == i){
                actualpoint.GetComponent<Image>().color = Color.red;
            }
            if(lastPoint != null){
                CreatePointConnections(lastPoint.GetComponent<RectTransform>().anchoredPosition, actualpoint.GetComponent<RectTransform>().anchoredPosition, speedPointsUIContainer.transform); 
            }
            lastPoint = actualpoint;
            // print("speed_h: " + selectedCycle_speed[i].time + ", speed_v: " + selectedCycle_speed[i].value);
        }

        lastPoint = null;
        v_distance = l_height.position.y - l_initialHeight.position.y;
        h_distance = l_heightFrame.position.x - l_initialHeight.position.x;

        for (int i = 0; i < selectedCycle_height.length; i++)
        {
            GameObject actualpoint = CreatePoint(new Vector2(selectedCycle_height[i].time * h_distance, selectedCycle_height[i].value * v_distance / 2), heightPointsUIContainer.transform);
            if(lastCurveEdited.Equals("height") && editingIndex == i){
                actualpoint.GetComponent<Image>().color = Color.red;
            }
             if(lastPoint != null){
                CreatePointConnections(lastPoint.GetComponent<RectTransform>().anchoredPosition, actualpoint.GetComponent<RectTransform>().anchoredPosition, heightPointsUIContainer.transform); 
            }
            lastPoint = actualpoint;
            // print("height_h: " + selectedCycle_height[i].time + ", height_v: " + selectedCycle_height[i].value);
        }
    }

    private void ClearGraphs(){
        Image[] destroySpeedPoints = speedPointsUIContainer.GetComponentsInChildren<Image>();
        Image[] destroyHeightPoints = heightPointsUIContainer.GetComponentsInChildren<Image>();
        
        for (int i = 0; i < destroySpeedPoints.Length; i++)
        {
            GameObject.Destroy(destroySpeedPoints[i].gameObject);
        }

        for (int i = 0; i < destroyHeightPoints.Length; i++)
        {
            GameObject.Destroy(destroyHeightPoints[i].gameObject);
        }
    }

    public void f_GetCoin(){
        _nOfCoins++;
        nOfCoinsText.text = _nOfCoins + "/5";
        if(_nOfCoins == 5){
            finishText.SetActive(true);
        }
    }

    public void f_CloseGame(){
        Application.Quit();
    }
}
