using BacklineVR.Core;
using BacklineVR.Interaction;
using BacklineVR.Items;
using CurseVR.Director;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Equipment
{
    /// <summary>
    /// This class represents a slot on the body that the player can grab at, which shows UI for each option available (thumbstick selectable)
    /// Items grabbed from pockets auto return to their slots if not thrown/ launched
    /// If released in the pocket area they can choose where it goes. If pocket is full it will drop on the floor.
    /// Listen to OnHoverEnter, OnHoverExit for showing UI, allowing placement/ withdrawal slot selection (listen to grabbed item released)
    /// </summary>
    [RequireComponent(typeof(Holdable))]
    public class Holster : MonoBehaviour
    {
        [SerializeField]
        private ItemCategory _slotCategory;
        private Holdable _interactable;
        private ItemManager _itemManager;
        private void Awake()
        {
            _interactable = GetComponent<Holdable>();
            _interactable.OnGrab += OnGrab;
            _interactable.OnRelease += OnRelease;
            _interactable.OnStartHover += OnStartHover;
            _interactable.OnStopHover += OnStopHover;
        }
        private void Start()
        {
           _itemManager = GlobalDirector.Get<ItemManager>();

        }
        public void OnGrab()
        {
            Debug.LogError("Grabbed holster " + transform.name);
        }
        public void OnRelease()
        {
            Debug.LogError("Released holster " + transform.name);
        }
        public void OnStartHover()
        {
            //Show grab UI
        }
        public void OnStopHover()
        {
            //Hide grab UI
        }
    }
}