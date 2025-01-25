using BacklineVR.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum HandSide { Left, Right }

public class Hand : MonoBehaviour
{
    [SerializeField]
    private Transform _arrowNockTransform;

    private void Awake()
    {
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        
    }
    public Transform ArrowNockTransform
    {
        get
        {
            return _arrowNockTransform;
        }
    }
}
