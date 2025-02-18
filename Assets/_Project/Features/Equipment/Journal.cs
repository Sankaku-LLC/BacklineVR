using BacklineVR.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Equipment
{
    /// <summary>
    /// This is mounted on the front chest of the player and is the main interface for text
    /// While holding it is held by the hand that grabbed it
    /// </summary>
    [RequireComponent(typeof(Holdable))]
    public class Journal : MonoBehaviour
    {
        private Holdable _interactable;
        private void Awake()
        {
            _interactable = GetComponent<Holdable>();
            _interactable.OnGrab += OnGrab;
            _interactable.OnRelease += OnRelease;
        }
        public void OnGrab()
        {
            Debug.LogError("Grabbed journal!");
        }
        public void OnRelease()
        {
            Debug.LogError("Released journal!");
        }
    }

}