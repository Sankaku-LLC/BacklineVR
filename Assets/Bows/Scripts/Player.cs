using BacklineVR.Interaction;
using BacklineVR.Interaction.Bow;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AttachmentFlags = BacklineVR.Interaction.Hand.AttachmentFlags;
namespace BacklineVR.Core
{
    public class Player : MonoBehaviour
    {
        public static Player Instance;
        public Transform Head;
        public Transform Origin;

        [SerializeField]
        private Interactable _longBow;

        [SerializeField]
        private Interactable _arrowHand;

        [SerializeField]
        private Hand _leftHand;

        [SerializeField]
        private Hand _rightHand;

        private AttachmentFlags _flags = AttachmentFlags.SnapOnAttach | AttachmentFlags.ParentToHand | AttachmentFlags.TurnOnKinematic | AttachmentFlags.TurnOffGravity | AttachmentFlags.AllowSidegrade;

        private void Awake()
        {
            Instance = this;
        }
        // Start is called before the first frame update
        void Start()
        {
            _leftHand.AttachObject(_longBow.gameObject, GrabTypes.Scripted);
            _rightHand.AttachObject(_arrowHand.gameObject, GrabTypes.Scripted);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}