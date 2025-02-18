using BacklineVR.Interaction;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace BacklineVR.Equipment
{
    /// <summary>
    /// The backpack can be grabbed from the back and has all the player's items accessible
    /// While holding it is held by the hand that grabbed it
    /// </summary>
    [RequireComponent(typeof(Holdable))]
    public class Backpack : MonoBehaviour
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
            Debug.LogError("Grabbed bag!");
        }
        public void OnRelease()
        {
            Debug.LogError("Released bag!");
        }
    }
}