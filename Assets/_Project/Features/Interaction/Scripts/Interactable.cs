using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Interaction
{
    // The flags used to determine how an object is attached to the hand.
    [Flags]
    public enum AttachmentFlags
    {
        SnapOnAttach = 1 << 0, // The object should snap to the position of the specified attachment point on the hand.
        DetachFromOtherHand = 1 << 2, // This object will be detached from the other hand.
        ParentToHand = 1 << 3, // The object will be parented to the hand.
        VelocityMovement = 1 << 4, // The object will attempt to move to match the position and rotation of the hand.
        TurnOnKinematic = 1 << 5, // The object will not respond to external physics.
        TurnOffGravity = 1 << 6, // The object will not respond to external physics.
    };
    [Flags]
    public enum AttachmentCriteria
    {
        None,
        DominantOnly,
        NonDominantOnly
    }
    public class Interactable : MonoBehaviour
    {
        public Action OnGrab;
        public Action OnRelease;
        public Action OnActivate;
        public Action OnDeactivate;
        public Action OnHeldUpdate;
        public Action OnStartHover;
        public Action OnStopHover;

        [Tooltip("Specify whether you want to snap to the hand's object attachment point, or just the raw hand")]
        public bool UseHandObjectAttachmentPoint = true;

        public bool attachEaseIn = false;
        [HideInInspector]
        public AnimationCurve snapAttachEaseInCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
        public float snapAttachEaseInTime = 0.15f;

        public bool snapAttachEaseInCompleted = false;


        [Tooltip("Should the rendered hand lock on to and follow the object")]
        public bool handFollowTransform = true;

        [Tooltip("Higher is better")]
        public int HoverPriority = 0;

        public Hand Owner { get; private set; }

        public bool IsDestroying { get; protected set; }

        public AttachmentFlags AttachmentFlags;
        public AttachmentCriteria AttachmentCriteria;


        public void Attach(Hand hand)
        {
            Owner = hand;
            OnGrab?.Invoke();
        }

        public void Detach(Hand hand)
        {
            Owner = null;
            OnRelease?.Invoke();
        }

        private void OnDestroy()
        {
            IsDestroying = true;

            if (Owner != null)
            {
                Owner.DetachObject(false);
            }
        }


        private void OnDisable()
        {
            IsDestroying = true;

            if (Owner != null)
            {
                Owner.ForceHoverUnlock();
            }
        }
    }
}