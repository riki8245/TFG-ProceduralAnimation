using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class take_coin : MonoBehaviour
{
    [SerializeField] private Controller _controller;
    [SerializeField] private GameObject _parent;
    private void OnTriggerEnter(Collider other) {
        if(other != null && other.gameObject.CompareTag("Player")){
            _controller.f_GetCoin();
            GameObject.Destroy(_parent);
        }
    }
}
