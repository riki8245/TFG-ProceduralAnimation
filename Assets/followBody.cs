using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class followBody : MonoBehaviour
{
    [SerializeField] private GameObject Leg_1;
    [SerializeField] private GameObject Leg_2;
    [SerializeField] private GameObject Leg_3_end;
    [SerializeField] private GameObject constraint;
    // Start is called before the first frame update
    void Awake()
    {
        TwoBoneIKConstraintData constraintData = constraint.GetComponent<TwoBoneIKConstraintData>();
        constraintData.root = Leg_1.transform;
        constraintData.mid = Leg_2.transform;
        constraintData.tip = Leg_3_end.transform;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
