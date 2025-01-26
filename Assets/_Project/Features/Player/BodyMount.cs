using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyMount : MonoBehaviour
{
    [SerializeField]
    private Transform _headset;

    [SerializeField]
    private Vector3 _offset;

    // Update is called once per frame
    private void Update()
    {
        var rotation = Quaternion.Euler(0, _headset.eulerAngles.y, 0);
        transform.position = _headset.position + rotation * _offset;
        transform.rotation = rotation;
    }
}
