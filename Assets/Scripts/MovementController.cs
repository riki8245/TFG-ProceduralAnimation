using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class MovementController : MonoBehaviour
{
    public GameObject canvas;
    public float _speed = 1f;
    public float mouseSensitivity = 100f;

    [SerializeField] private LayerMask pointLayer, curveEditor; 

    //Private Variables
    public bool pause = false;
    private Rigidbody _rigibody;
    private float axisHorizontal;
    private float axisVertical;
    private float mouseX;
    private string editingCurve;
    private GameObject speedCurveUI;
    private GameObject heightCurveUI;
    private ProceduralAnimation proceduralAnimation;

    void Start()
    {
        proceduralAnimation = GetComponentInChildren<ProceduralAnimation>();
        _rigibody = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)){
            f_OpenCloseMenu();
        }

        if(pause) return;

        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        axisHorizontal = Input.GetAxis("Horizontal");
        axisVertical = Input.GetAxis("Vertical");
    }

    private void f_OpenCloseMenu(){
        pause = !pause;
        canvas.SetActive(pause);
        if(pause){
            speedCurveUI = GameObject.Find("SpeedCurve");
            heightCurveUI = GameObject.Find("HeightCurve");
            Cursor.lockState = CursorLockMode.None;
        }
        else{
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void FixedUpdate() {
        if(pause) return;
        f_Movement();
    }

    public void f_selectWalkingCycle(string cycle){
        proceduralAnimation.f_ChoosePreset(cycle);

        speedCurveUI.GetComponent<Image>().color = Color.white;
        heightCurveUI.GetComponent<Image>().color = Color.white;
    }

    public void f_EditMode(string curve){
        editingCurve = curve;
        if(editingCurve.Equals("speed")){
            heightCurveUI.GetComponent<Image>().color = Color.grey;
            speedCurveUI.GetComponent<Image>().color = Color.white;
        }
        else{
            speedCurveUI.GetComponent<Image>().color = Color.grey;
            heightCurveUI.GetComponent<Image>().color = Color.white;
        }
    }

    private void f_PutPoint(){

    }

    private void f_Movement(){
        if(_rigibody.velocity.magnitude < _speed){
            if(axisVertical != 0){
                Vector3 forwardMovement = transform.forward * axisVertical * Time.fixedDeltaTime * 1500f;
                _rigibody.AddForce(forwardMovement);
            } 
            if(axisHorizontal != 0){
                Vector3 horizontalMovement = transform.right * axisHorizontal * Time.fixedDeltaTime * 1500f;
                _rigibody.AddForce(horizontalMovement);
            } 
        }
    }
}
