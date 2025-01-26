using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Interaction
{
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

        public void Attach(Hand hand)
        {
            Owner = hand;
        }

        public void Detach(Hand hand)
        {
            Owner = null;
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