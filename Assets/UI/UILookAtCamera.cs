using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILookAtCamera : MonoBehaviour
{
    private Camera _playerCam;
    public void Start()
    {
        _playerCam = Camera.main;
    }
    public void Update()
    {
        Vector3 lookDirection = _playerCam.transform.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = rotation;
    }
}
