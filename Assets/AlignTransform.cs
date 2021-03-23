using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignTransform : MonoBehaviour
{
    public Transform Tip;
    public Transform Controller;
    // Start is called before the first frame update
    void Start()
    {
        Controller.transform.position = Tip.transform.position;
        Controller.transform.rotation = Tip.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
