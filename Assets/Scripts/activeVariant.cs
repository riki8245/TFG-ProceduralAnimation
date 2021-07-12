using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class activeVariant : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Image active1, active2, active3, active4; 

    public void changeVariant(int variant){
        active1.enabled = false;
        active2.enabled = false;
        active3.enabled = false;
        active4.enabled = false;
        switch (variant){
            case 1: active1.enabled = true; break;
            case 2: active2.enabled = true; break;
            case 3: active3.enabled = true; break;
            case 4: active4.enabled = true; break;
            default: break;
        }
    }
}
