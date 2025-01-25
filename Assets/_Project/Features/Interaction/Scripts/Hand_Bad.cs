//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: The hands used by the player in the vr interaction system
//
//=============================================================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BacklineVR.Core;
using BacklineVR.Interaction.Bow;
using CurseVR.Director;

namespace BacklineVR.Interaction
{
    public class Hand_Bad : MonoBehaviour
    {
        // The flags used to determine how an object is attached to the hand.
        [Flags]
        public enum AttachmentFlags
        {
            SnapOnAttach = 1 << 0, // The object should snap to the position of the specified attachment point on the hand.
            DetachOthers = 1 << 1, // Other objects attached to this hand will be detached.
            DetachFromOtherHand = 1 << 2, // This object will be detached from the other hand.
            ParentToHand = 1 << 3, // The object will be parented to the hand.
            VelocityMovement = 1 << 4, // The object will attempt to move to match the position and rotation of the hand.
            TurnOnKinematic = 1 << 5, // The object will not respond to external physics.
            TurnOffGravity = 1 << 6, // The object will not respond to external physics.
            AllowSidegrade = 1 << 7, // The object is able to switch from a pinch grab to a grip grab. Decreases likelyhood of a good throw but also decreases likelyhood of accidental drop
        };

        public const AttachmentFlags defaultAttachmentFlags = AttachmentFlags.ParentToHand |
                                                              AttachmentFlags.DetachOthers |
                                                              AttachmentFlags.DetachFromOtherHand |
                                                              AttachmentFlags.TurnOnKinematic |
                                                              AttachmentFlags.SnapOnAttach;

        public Hand_Bad OtherHand;
        public HandSide HandSide;

        private InputProvider _inputProvider;

        public Transform hoverSphereTransform;
        public float hoverSphereRadius = 0.05f;
        public LayerMask hoverLayerMask = -1;
        public float hoverUpdateInterval = 0.1f;


        [Tooltip("A transform on the hand to center attached objects on")]
        public Transform objectAttachmentPoint;

        public bool showDebugText = false;
        public bool spewDebugText = false;
        public bool showDebugInteractables = false;

        public struct AttachedObject
        {
            public GameObject attachedObject;
            public Interactable_Bad interactable;
            public Rigidbody attachedRigidbody;
            public CollisionDetectionMode collisionDetectionMode;
            public bool attachedRigidbodyWasKinematic;
            public bool attachedRigidbodyUsedGravity;
            public GameObject originalParent;
            public bool isParentedToHand;
            public GrabTypes grabbedWithType;
            public AttachmentFlags attachmentFlags;
            public Vector3 initialPositionalOffset;
            public Quaternion initialRotationalOffset;
            public Transform attachedOffsetTransform;
            public Transform handAttachmentPointTransform;
            public Vector3 easeSourcePosition;
            public Quaternion easeSourceRotation;
            public float attachTime;

            public bool HasAttachFlag(AttachmentFlags flag)
            {
                return (attachmentFlags & flag) == flag;
            }
        }

        private List<AttachedObject> attachedObjects = new List<AttachedObject>();

        public ReadOnlyCollection<AttachedObject> AttachedObjects
        {
            get { return attachedObjects.AsReadOnly(); }
        }

        public bool hoverLocked { get; private set; }

        private Interactable_Bad _hoveringInteractable;

        private TextMesh debugText;
        private int prevOverlappingColliders = 0;

        private const int ColliderArraySize = 32;
        private Collider[] overlappingColliders = new Collider[ColliderArraySize];

        private Player playerInstance;

        //-------------------------------------------------
        protected virtual void Awake()
        {
            if (hoverSphereTransform == null)
                hoverSphereTransform = this.transform;

            if (objectAttachmentPoint == null)
                objectAttachmentPoint = this.transform;
        }




        //-------------------------------------------------
        private void Start()
        {
            _inputProvider = GlobalDirector.Get<InputProvider>();

            // save off player instance
            playerInstance = Player.Instance;
            if (!playerInstance)
            {
                Debug.LogError("<b>[SteamVR Interaction]</b> No player instance found in Hand Start()", this);
            }

            if (this.gameObject.layer == 0)
                Debug.LogWarning("<b>[SteamVR Interaction]</b> Hand is on default layer. This puts unnecessary strain on hover checks as it is always true for hand colliders (which are then ignored).", this);
            else
                hoverLayerMask &= ~(1 << this.gameObject.layer); //ignore self for hovering
        }

        //-------------------------------------------------
        // The Interactable object this Hand is currently hovering over
        //-------------------------------------------------
        public Interactable_Bad hoveringInteractable
        {
            get { return _hoveringInteractable; }
            set
            {
                if (_hoveringInteractable != value)
                {
                    if (_hoveringInteractable != null)
                    {
                        if (spewDebugText)
                            HandDebugLog("HoverEnd " + _hoveringInteractable.gameObject);
                        _hoveringInteractable.SendMessage("OnHandHoverEnd", this, SendMessageOptions.DontRequireReceiver);

                        //Note: The _hoveringInteractable can change after sending the OnHandHoverEnd message so we need to check it again before broadcasting this message
                        if (_hoveringInteractable != null)
                        {
                            this.BroadcastMessage("OnParentHandHoverEnd", _hoveringInteractable, SendMessageOptions.DontRequireReceiver); // let objects attached to the hand know that a hover has ended
                        }
                    }

                    _hoveringInteractable = value;

                    if (_hoveringInteractable != null)
                    {
                        if (spewDebugText)
                            HandDebugLog("HoverBegin " + _hoveringInteractable.gameObject);
                        _hoveringInteractable.SendMessage("OnHandHoverBegin", this, SendMessageOptions.DontRequireReceiver);

                        //Note: The _hoveringInteractable can change after sending the OnHandHoverBegin message so we need to check it again before broadcasting this message
                        if (_hoveringInteractable != null)
                        {
                            this.BroadcastMessage("OnParentHandHoverBegin", _hoveringInteractable, SendMessageOptions.DontRequireReceiver); // let objects attached to the hand know that a hover has begun
                        }
                    }
                }
            }
        }


        //-------------------------------------------------
        // Active GameObject attached to this Hand
        //-------------------------------------------------
        public GameObject currentAttachedObject
        {
            get
            {
                CleanUpAttachedObjectStack();

                if (attachedObjects.Count > 0)
                {
                    return attachedObjects[attachedObjects.Count - 1].attachedObject;
                }

                return null;
            }
        }

        public AttachedObject? currentAttachedObjectInfo
        {
            get
            {
                CleanUpAttachedObjectStack();

                if (attachedObjects.Count > 0)
                {
                    return attachedObjects[attachedObjects.Count - 1];
                }

                return null;
            }
        }


        //-------------------------------------------------
        // Attach a GameObject to this GameObject
        //
        // objectToAttach - The GameObject to attach
        // flags - The flags to use for attaching the object
        // attachmentPoint - Name of the GameObject in the hierarchy of this Hand which should act as the attachment point for this GameObject
        //-------------------------------------------------
        public void AttachObject(GameObject objectToAttach, GrabTypes grabbedWithType, AttachmentFlags flags = defaultAttachmentFlags, Transform attachmentOffset = null)
        {
            AttachedObject attachedObject = new AttachedObject();
            attachedObject.attachmentFlags = flags;
            attachedObject.attachedOffsetTransform = attachmentOffset;
            attachedObject.attachTime = Time.time;

            if (flags == 0)
            {
                flags = defaultAttachmentFlags;
            }

            //Make sure top object on stack is non-null
            CleanUpAttachedObjectStack();

            //Detach the object if it is already attached so that it can get re-attached at the top of the stack
            if (ObjectIsAttached(objectToAttach))
                DetachObject(objectToAttach);

            //Detach from the other hand if requested
            if (attachedObject.HasAttachFlag(AttachmentFlags.DetachFromOtherHand))
            {
                if (OtherHand != null)
                    OtherHand.DetachObject(objectToAttach);
            }

            if (attachedObject.HasAttachFlag(AttachmentFlags.DetachOthers))
            {
                //Detach all the objects from the stack
                while (attachedObjects.Count > 0)
                {
                    DetachObject(attachedObjects[0].attachedObject);
                }
            }

            if (currentAttachedObject)
            {
                currentAttachedObject.SendMessage("OnHandFocusLost", this, SendMessageOptions.DontRequireReceiver);
            }

            attachedObject.attachedObject = objectToAttach;
            attachedObject.interactable = objectToAttach.GetComponent<Interactable_Bad>();
            attachedObject.handAttachmentPointTransform = this.transform;

            if (attachedObject.interactable != null)
            {
                if (attachedObject.interactable.attachEaseIn)
                {
                    attachedObject.easeSourcePosition = attachedObject.attachedObject.transform.position;
                    attachedObject.easeSourceRotation = attachedObject.attachedObject.transform.rotation;
                    attachedObject.interactable.snapAttachEaseInCompleted = false;
                }

                if (attachedObject.interactable.useHandObjectAttachmentPoint)
                    attachedObject.handAttachmentPointTransform = objectAttachmentPoint;

            }

            attachedObject.originalParent = objectToAttach.transform.parent != null ? objectToAttach.transform.parent.gameObject : null;

            attachedObject.attachedRigidbody = objectToAttach.GetComponent<Rigidbody>();
            if (attachedObject.attachedRigidbody != null)
            {
                if (attachedObject.interactable.attachedToHand != null) //already attached to another hand
                {
                    //if it was attached to another hand, get the flags from that hand

                    for (int attachedIndex = 0; attachedIndex < attachedObject.interactable.attachedToHand.attachedObjects.Count; attachedIndex++)
                    {
                        AttachedObject attachedObjectInList = attachedObject.interactable.attachedToHand.attachedObjects[attachedIndex];
                        if (attachedObjectInList.interactable == attachedObject.interactable)
                        {
                            attachedObject.attachedRigidbodyWasKinematic = attachedObjectInList.attachedRigidbodyWasKinematic;
                            attachedObject.attachedRigidbodyUsedGravity = attachedObjectInList.attachedRigidbodyUsedGravity;
                            attachedObject.originalParent = attachedObjectInList.originalParent;
                        }
                    }
                }
                else
                {
                    attachedObject.attachedRigidbodyWasKinematic = attachedObject.attachedRigidbody.isKinematic;
                    attachedObject.attachedRigidbodyUsedGravity = attachedObject.attachedRigidbody.useGravity;
                }
            }

            attachedObject.grabbedWithType = grabbedWithType;

            if (attachedObject.HasAttachFlag(AttachmentFlags.ParentToHand))
            {
                //Parent the object to the hand
                objectToAttach.transform.parent = this.transform;
                attachedObject.isParentedToHand = true;
            }
            else
            {
                attachedObject.isParentedToHand = false;
            }

            if (attachedObject.HasAttachFlag(AttachmentFlags.SnapOnAttach))
            {
                if (attachmentOffset != null)
                {
                    //offset the object from the hand by the positional and rotational difference between the offset transform and the attached object
                    Quaternion rotDiff = Quaternion.Inverse(attachmentOffset.transform.rotation) * objectToAttach.transform.rotation;
                    objectToAttach.transform.rotation = attachedObject.handAttachmentPointTransform.rotation * rotDiff;

                    Vector3 posDiff = objectToAttach.transform.position - attachmentOffset.transform.position;
                    objectToAttach.transform.position = attachedObject.handAttachmentPointTransform.position + posDiff;
                }
                else
                {
                    //snap the object to the center of the attach point
                    objectToAttach.transform.rotation = attachedObject.handAttachmentPointTransform.rotation;
                    objectToAttach.transform.position = attachedObject.handAttachmentPointTransform.position;
                }

                Transform followPoint = objectToAttach.transform;

                attachedObject.initialPositionalOffset = attachedObject.handAttachmentPointTransform.InverseTransformPoint(followPoint.position);
                attachedObject.initialRotationalOffset = Quaternion.Inverse(attachedObject.handAttachmentPointTransform.rotation) * followPoint.rotation;
            }
            else
            {
                if (attachmentOffset != null)
                {
                    //get the initial positional and rotational offsets between the hand and the offset transform
                    Quaternion rotDiff = Quaternion.Inverse(attachmentOffset.transform.rotation) * objectToAttach.transform.rotation;
                    Quaternion targetRotation = attachedObject.handAttachmentPointTransform.rotation * rotDiff;
                    Quaternion rotationPositionBy = targetRotation * Quaternion.Inverse(objectToAttach.transform.rotation);

                    Vector3 posDiff = (rotationPositionBy * objectToAttach.transform.position) - (rotationPositionBy * attachmentOffset.transform.position);

                    attachedObject.initialPositionalOffset = attachedObject.handAttachmentPointTransform.InverseTransformPoint(attachedObject.handAttachmentPointTransform.position + posDiff);
                    attachedObject.initialRotationalOffset = Quaternion.Inverse(attachedObject.handAttachmentPointTransform.rotation) * (attachedObject.handAttachmentPointTransform.rotation * rotDiff);
                }
                else
                {
                    attachedObject.initialPositionalOffset = attachedObject.handAttachmentPointTransform.InverseTransformPoint(objectToAttach.transform.position);
                    attachedObject.initialRotationalOffset = Quaternion.Inverse(attachedObject.handAttachmentPointTransform.rotation) * objectToAttach.transform.rotation;
                }
            }



            if (attachedObject.HasAttachFlag(AttachmentFlags.TurnOnKinematic))
            {
                if (attachedObject.attachedRigidbody != null)
                {
                    attachedObject.collisionDetectionMode = attachedObject.attachedRigidbody.collisionDetectionMode;
                    if (attachedObject.collisionDetectionMode == CollisionDetectionMode.Continuous)
                        attachedObject.attachedRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

                    attachedObject.attachedRigidbody.isKinematic = true;
                }
            }

            if (attachedObject.HasAttachFlag(AttachmentFlags.TurnOffGravity))
            {
                if (attachedObject.attachedRigidbody != null)
                {
                    attachedObject.attachedRigidbody.useGravity = false;
                }
            }

            if (attachedObject.interactable != null && attachedObject.interactable.attachEaseIn)
            {
                attachedObject.attachedObject.transform.position = attachedObject.easeSourcePosition;
                attachedObject.attachedObject.transform.rotation = attachedObject.easeSourceRotation;
            }

            attachedObjects.Add(attachedObject);

            UpdateHovering();

            if (spewDebugText)
                HandDebugLog("AttachObject " + objectToAttach);
            objectToAttach.SendMessage("OnAttachedToHand", this, SendMessageOptions.DontRequireReceiver);
        }

        public bool ObjectIsAttached(GameObject go)
        {
            for (int attachedIndex = 0; attachedIndex < attachedObjects.Count; attachedIndex++)
            {
                if (attachedObjects[attachedIndex].attachedObject == go)
                    return true;
            }

            return false;
        }

        public void ForceHoverUnlock()
        {
            hoverLocked = false;
        }

        //-------------------------------------------------
        // Detach this GameObject from the attached object stack of this Hand
        //
        // objectToDetach - The GameObject to detach from this Hand
        //-------------------------------------------------
        public void DetachObject(GameObject objectToDetach, bool restoreOriginalParent = true)
        {
            int index = attachedObjects.FindIndex(l => l.attachedObject == objectToDetach);
            if (index != -1)
            {
                if (spewDebugText)
                    HandDebugLog("DetachObject " + objectToDetach);

                GameObject prevTopObject = currentAttachedObject;



                Transform parentTransform = null;
                if (attachedObjects[index].isParentedToHand)
                {
                    if (restoreOriginalParent && (attachedObjects[index].originalParent != null))
                    {
                        parentTransform = attachedObjects[index].originalParent.transform;
                    }

                    if (attachedObjects[index].attachedObject != null)
                    {
                        attachedObjects[index].attachedObject.transform.parent = parentTransform;
                    }
                }

                if (attachedObjects[index].HasAttachFlag(AttachmentFlags.TurnOnKinematic))
                {
                    if (attachedObjects[index].attachedRigidbody != null)
                    {
                        attachedObjects[index].attachedRigidbody.isKinematic = attachedObjects[index].attachedRigidbodyWasKinematic;
                        attachedObjects[index].attachedRigidbody.collisionDetectionMode = attachedObjects[index].collisionDetectionMode;
                    }
                }

                if (attachedObjects[index].HasAttachFlag(AttachmentFlags.TurnOffGravity))
                {
                    if (attachedObjects[index].attachedObject != null)
                    {
                        if (attachedObjects[index].attachedRigidbody != null)
                            attachedObjects[index].attachedRigidbody.useGravity = attachedObjects[index].attachedRigidbodyUsedGravity;
                    }
                }


                if (attachedObjects[index].attachedObject != null)
                {
                    if (attachedObjects[index].interactable == null || (attachedObjects[index].interactable != null && attachedObjects[index].interactable.isDestroying == false))
                        attachedObjects[index].attachedObject.SetActive(true);

                    attachedObjects[index].attachedObject.SendMessage("OnDetachedFromHand", this, SendMessageOptions.DontRequireReceiver);
                }

                attachedObjects.RemoveAt(index);

                CleanUpAttachedObjectStack();

                GameObject newTopObject = currentAttachedObject;

                hoverLocked = false;


                //Give focus to the top most object on the stack if it changed
                if (newTopObject != null && newTopObject != prevTopObject)
                {
                    newTopObject.SetActive(true);
                    newTopObject.SendMessage("OnHandFocusAcquired", this, SendMessageOptions.DontRequireReceiver);
                }
            }

            CleanUpAttachedObjectStack();


        }

        //-------------------------------------------------
        private void CleanUpAttachedObjectStack()
        {
            attachedObjects.RemoveAll(l => l.attachedObject == null);
        }



        //-------------------------------------------------
        protected virtual void UpdateHovering()
        {
            if (hoverLocked)
                return;

            float closestDistance = float.MaxValue;
            Interactable_Bad closestInteractable = null;

            float scaledHoverRadius = hoverSphereRadius * Mathf.Abs(hoverSphereTransform.lossyScale.x);
            CheckHoveringForTransform(hoverSphereTransform.position, scaledHoverRadius, ref closestDistance, ref closestInteractable, Color.green);

            // Hover on this one
            hoveringInteractable = closestInteractable;
        }

        protected virtual bool CheckHoveringForTransform(Vector3 hoverPosition, float hoverRadius, ref float closestDistance, ref Interactable_Bad closestInteractable, Color debugColor)
        {
            bool foundCloser = false;

            // null out old vals
            for (int i = 0; i < overlappingColliders.Length; ++i)
            {
                overlappingColliders[i] = null;
            }

            int numColliding = Physics.OverlapSphereNonAlloc(hoverPosition, hoverRadius, overlappingColliders, hoverLayerMask.value);

            if (numColliding >= ColliderArraySize)
                Debug.LogWarning("<b>[SteamVR Interaction]</b> This hand is overlapping the max number of colliders: " + ColliderArraySize + ". Some collisions may be missed. Increase ColliderArraySize on Hand.cs");

            // DebugVar
            int iActualColliderCount = 0;

            // Pick the closest hovering
            for (int colliderIndex = 0; colliderIndex < overlappingColliders.Length; colliderIndex++)
            {
                Collider collider = overlappingColliders[colliderIndex];

                if (collider == null)
                    continue;

                Interactable_Bad contacting = collider.GetComponentInParent<Interactable_Bad>();

                // Yeah, it's null, skip
                if (contacting == null)
                    continue;

                // Can't hover over the object if it's attached
                bool hoveringOverAttached = false;
                for (int attachedIndex = 0; attachedIndex < attachedObjects.Count; attachedIndex++)
                {
                    if (attachedObjects[attachedIndex].attachedObject == contacting.gameObject)
                    {
                        hoveringOverAttached = true;
                        break;
                    }
                }

                if (hoveringOverAttached)
                    continue;

                // Best candidate so far...
                float distance = Vector3.Distance(contacting.transform.position, hoverPosition);
                bool lowerPriority = false;
                if (closestInteractable != null)
                { // compare to closest interactable to check priority
                    lowerPriority = contacting.hoverPriority < closestInteractable.hoverPriority;
                }
                bool isCloser = (distance < closestDistance);
                if (isCloser && !lowerPriority)
                {
                    closestDistance = distance;
                    closestInteractable = contacting;
                    foundCloser = true;
                }
                iActualColliderCount++;
            }

            if (showDebugInteractables && foundCloser)
            {
                Debug.DrawLine(hoverPosition, closestInteractable.transform.position, debugColor, .05f, false);
            }

            if (iActualColliderCount > 0 && iActualColliderCount != prevOverlappingColliders)
            {
                prevOverlappingColliders = iActualColliderCount;

                if (spewDebugText)
                    HandDebugLog("Found " + iActualColliderCount + " overlapping colliders.");
            }

            return foundCloser;
        }


        //-------------------------------------------------
        private void UpdateDebugText()
        {
            if (showDebugText)
            {
                if (debugText == null)
                {
                    debugText = new GameObject("_debug_text").AddComponent<TextMesh>();
                    debugText.fontSize = 120;
                    debugText.characterSize = 0.001f;
                    debugText.transform.parent = transform;

                    debugText.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
                }

                if (HandSide == HandSide.Right)
                {
                    debugText.transform.localPosition = new Vector3(-0.05f, 0.0f, 0.0f);
                    debugText.alignment = TextAlignment.Right;
                    debugText.anchor = TextAnchor.UpperRight;
                }
                else
                {
                    debugText.transform.localPosition = new Vector3(0.05f, 0.0f, 0.0f);
                    debugText.alignment = TextAlignment.Left;
                    debugText.anchor = TextAnchor.UpperLeft;
                }

                debugText.text = string.Format(
                    "Hovering: {0}\n" +
                    "Hover Lock: {1}\n" +
                    "Attached: {2}\n" +
                    "Total Attached: {3}\n" +
                    "Type: {4}\n",
                    (hoveringInteractable ? hoveringInteractable.gameObject.name : "null"),
                    hoverLocked,
                    (currentAttachedObject ? currentAttachedObject.name : "null"),
                    attachedObjects.Count,
                    HandSide.ToString());
            }
            else
            {
                if (debugText != null)
                {
                    Destroy(debugText.gameObject);
                }
            }
        }


        //-------------------------------------------------
        protected virtual void OnEnable()
        {
            // Stagger updates between hands
            float hoverUpdateBegin = ((OtherHand != null) && (OtherHand.GetInstanceID() < GetInstanceID())) ? (0.5f * hoverUpdateInterval) : (0.0f);
            InvokeRepeating("UpdateHovering", hoverUpdateBegin, hoverUpdateInterval);
            InvokeRepeating("UpdateDebugText", hoverUpdateBegin, hoverUpdateInterval);
        }


        //-------------------------------------------------
        protected virtual void OnDisable()
        {
            CancelInvoke();
        }


        //-------------------------------------------------
        protected virtual void Update()
        {
            GameObject attachedObject = currentAttachedObject;
            if (attachedObject != null)
            {
                attachedObject.SendMessage("HandAttachedUpdate", this, SendMessageOptions.DontRequireReceiver);
            }

            if (hoveringInteractable)
            {
                hoveringInteractable.SendMessage("HandHoverUpdate", this, SendMessageOptions.DontRequireReceiver);
            }

            HandFollowUpdate();
        }

        protected virtual void HandFollowUpdate()
        {
            GameObject attachedObject = currentAttachedObject;
            if (attachedObject != null)
            {
                if (currentAttachedObjectInfo.Value.interactable != null)
                {

                    if (currentAttachedObjectInfo.Value.interactable.handFollowTransform)
                    {
                        Quaternion targetHandRotation;
                        Vector3 targetHandPosition;
                        Quaternion offset = Quaternion.Inverse(this.transform.rotation) * currentAttachedObjectInfo.Value.handAttachmentPointTransform.rotation;
                        targetHandRotation = currentAttachedObjectInfo.Value.interactable.transform.rotation * Quaternion.Inverse(offset);

                        Vector3 worldOffset = (this.transform.position - currentAttachedObjectInfo.Value.handAttachmentPointTransform.position);
                        Quaternion rotationDiff = Quaternion.identity;
                        Vector3 localOffset = rotationDiff * worldOffset;
                        targetHandPosition = currentAttachedObjectInfo.Value.interactable.transform.position + localOffset;
                    }
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (currentAttachedObject != null)
            {
                AttachedObject attachedInfo = currentAttachedObjectInfo.Value;
                if (attachedInfo.attachedObject != null)
                {
                    if (attachedInfo.HasAttachFlag(AttachmentFlags.VelocityMovement))
                    {
                        if (attachedInfo.interactable.attachEaseIn == false || attachedInfo.interactable.snapAttachEaseInCompleted)
                            UpdateAttachedVelocity(attachedInfo);

                    }
                    else
                    {
                        if (attachedInfo.HasAttachFlag(AttachmentFlags.ParentToHand))
                        {
                            attachedInfo.attachedObject.transform.position = TargetItemPosition(attachedInfo);
                            attachedInfo.attachedObject.transform.rotation = TargetItemRotation(attachedInfo);
                        }
                    }


                    if (attachedInfo.interactable.attachEaseIn)
                    {
                        float t = Util.RemapNumberClamped(Time.time, attachedInfo.attachTime, attachedInfo.attachTime + attachedInfo.interactable.snapAttachEaseInTime, 0.0f, 1.0f);
                        if (t < 1.0f)
                        {
                            if (attachedInfo.HasAttachFlag(AttachmentFlags.VelocityMovement))
                            {
                                attachedInfo.attachedRigidbody.velocity = Vector3.zero;
                                attachedInfo.attachedRigidbody.angularVelocity = Vector3.zero;
                            }
                            t = attachedInfo.interactable.snapAttachEaseInCurve.Evaluate(t);
                            attachedInfo.attachedObject.transform.position = Vector3.Lerp(attachedInfo.easeSourcePosition, TargetItemPosition(attachedInfo), t);
                            attachedInfo.attachedObject.transform.rotation = Quaternion.Lerp(attachedInfo.easeSourceRotation, TargetItemRotation(attachedInfo), t);
                        }
                        else if (!attachedInfo.interactable.snapAttachEaseInCompleted)
                        {
                            attachedInfo.interactable.gameObject.SendMessage("OnThrowableAttachEaseInCompleted", this, SendMessageOptions.DontRequireReceiver);
                            attachedInfo.interactable.snapAttachEaseInCompleted = true;
                        }
                    }
                }
            }
        }

        protected const float MaxVelocityChange = 10f;
        protected const float VelocityMagic = 6000f;
        protected const float AngularVelocityMagic = 50f;
        protected const float MaxAngularVelocityChange = 20f;

        protected void UpdateAttachedVelocity(AttachedObject attachedObjectInfo)
        {
            Vector3 velocityTarget, angularTarget;
            bool success = GetUpdatedAttachedVelocities(attachedObjectInfo, out velocityTarget, out angularTarget);
            if (success)
            {
                float scale = currentAttachedObjectInfo.Value.handAttachmentPointTransform.lossyScale.x;
                float maxAngularVelocityChange = MaxAngularVelocityChange * scale;
                float maxVelocityChange = MaxVelocityChange * scale;

                attachedObjectInfo.attachedRigidbody.velocity = Vector3.MoveTowards(attachedObjectInfo.attachedRigidbody.velocity, velocityTarget, maxVelocityChange);
                attachedObjectInfo.attachedRigidbody.angularVelocity = Vector3.MoveTowards(attachedObjectInfo.attachedRigidbody.angularVelocity, angularTarget, maxAngularVelocityChange);
            }
        }

        /// <summary>
        /// Snap an attached object to its target position and rotation. Good for error correction.
        /// </summary>
        public void ResetAttachedTransform(AttachedObject attachedObject)
        {
            attachedObject.attachedObject.transform.position = TargetItemPosition(attachedObject);
            attachedObject.attachedObject.transform.rotation = TargetItemRotation(attachedObject);
        }

        protected Vector3 TargetItemPosition(AttachedObject attachedObject)
        {
            return currentAttachedObjectInfo.Value.handAttachmentPointTransform.TransformPoint(attachedObject.initialPositionalOffset);
        }

        protected Quaternion TargetItemRotation(AttachedObject attachedObject)
        {
            return currentAttachedObjectInfo.Value.handAttachmentPointTransform.rotation * attachedObject.initialRotationalOffset;
        }

        protected bool GetUpdatedAttachedVelocities(AttachedObject attachedObjectInfo, out Vector3 velocityTarget, out Vector3 angularTarget)
        {
            bool realNumbers = false;


            float velocityMagic = VelocityMagic;
            float angularVelocityMagic = AngularVelocityMagic;

            Vector3 targetItemPosition = TargetItemPosition(attachedObjectInfo);
            Vector3 positionDelta = (targetItemPosition - attachedObjectInfo.attachedRigidbody.position);
            velocityTarget = (positionDelta * velocityMagic * Time.deltaTime);

            if (float.IsNaN(velocityTarget.x) == false && float.IsInfinity(velocityTarget.x) == false)
            {
                realNumbers = true;
            }
            else
                velocityTarget = Vector3.zero;


            Quaternion targetItemRotation = TargetItemRotation(attachedObjectInfo);
            Quaternion rotationDelta = targetItemRotation * Quaternion.Inverse(attachedObjectInfo.attachedObject.transform.rotation);


            float angle;
            Vector3 axis;
            rotationDelta.ToAngleAxis(out angle, out axis);

            if (angle > 180)
                angle -= 360;

            if (angle != 0 && float.IsNaN(axis.x) == false && float.IsInfinity(axis.x) == false)
            {
                angularTarget = angle * axis * angularVelocityMagic * Time.deltaTime;
                realNumbers &= true;
            }
            else
                angularTarget = Vector3.zero;

            return realNumbers;
        }

        //-------------------------------------------------
        protected virtual void OnDrawGizmos()
        {
            if (hoverSphereTransform != null)
            {
                Gizmos.color = Color.green;
                float scaledHoverRadius = hoverSphereRadius * Mathf.Abs(hoverSphereTransform.lossyScale.x);
                Gizmos.DrawWireSphere(hoverSphereTransform.position, scaledHoverRadius / 2);
            }
        }


        //-------------------------------------------------
        private void HandDebugLog(string msg)
        {
            if (spewDebugText)
            {
                Debug.Log("<b>[SteamVR Interaction]</b> Hand (" + this.name + "): " + msg);
            }
        }


        //-------------------------------------------------
        // Continue to hover over this object indefinitely, whether or not the Hand moves out of its interaction trigger volume.
        //
        // interactable - The Interactable to hover over indefinitely.
        //-------------------------------------------------
        public void HoverLock(Interactable_Bad interactable)
        {
            if (spewDebugText)
                HandDebugLog("HoverLock " + interactable);
            hoverLocked = true;
            hoveringInteractable = interactable;
        }


        //-------------------------------------------------
        // Stop hovering over this object indefinitely.
        //
        // interactable - The hover-locked Interactable to stop hovering over indefinitely.
        //-------------------------------------------------
        public void HoverUnlock(Interactable_Bad interactable)
        {
            if (spewDebugText)
                HandDebugLog("HoverUnlock " + interactable);

            if (hoveringInteractable == interactable)
            {
                hoverLocked = false;
            }
        }

        public void TriggerHapticPulse(ushort microSecondsDuration)
        {
            float seconds = (float)microSecondsDuration / 1000000f;
            _inputProvider.RequestHapticPulse(HandSide, 1, seconds, 1.5f / seconds);
        }

        public GrabTypes GetGrabStarting(GrabTypes explicitType = GrabTypes.None)
        {
            if (explicitType != GrabTypes.None)
            {
                if (explicitType == GrabTypes.Pinch && _inputProvider.IsTriggerDown(HandSide))
                    return GrabTypes.Pinch;
                if (explicitType == GrabTypes.Grip && _inputProvider.IsGripDown(HandSide))
                    return GrabTypes.Grip;
            }
            else
            {
                if (_inputProvider.IsTriggerDown(HandSide))
                    return GrabTypes.Pinch;
                if (_inputProvider.IsGripDown(HandSide))
                    return GrabTypes.Grip;
            }

            return GrabTypes.None;
        }

        public GrabTypes GetGrabEnding(GrabTypes explicitType = GrabTypes.None)
        {
            if (explicitType != GrabTypes.None)
            {
                if (explicitType == GrabTypes.Pinch && !_inputProvider.IsTriggerDown(HandSide))
                    return GrabTypes.Pinch;
                if (explicitType == GrabTypes.Grip && !_inputProvider.IsGripDown(HandSide))
                    return GrabTypes.Grip;
            }
            else
            {
                if (!_inputProvider.IsTriggerDown(HandSide))
                    return GrabTypes.Pinch;
                if (!_inputProvider.IsGripDown(HandSide))
                    return GrabTypes.Grip;
            }

            return GrabTypes.None;
        }

        public bool IsGrabEnding(GameObject attachedObject)
        {
            for (int attachedObjectIndex = 0; attachedObjectIndex < attachedObjects.Count; attachedObjectIndex++)
            {
                if (attachedObjects[attachedObjectIndex].attachedObject == attachedObject)
                {
                    return IsGrabbingWithType(attachedObjects[attachedObjectIndex].grabbedWithType) == false;
                }
            }

            return false;
        }

        public bool IsGrabbingWithType(GrabTypes type)
        {
            switch (type)
            {
                case GrabTypes.Pinch:
                    return _inputProvider.IsTriggerDown(HandSide);

                case GrabTypes.Grip:
                    return _inputProvider.IsGripDown(HandSide);

                default:
                    return false;
            }
        }

        public GrabTypes GetBestGrabbingType()
        {
            return GetBestGrabbingType(GrabTypes.None);
        }

        public GrabTypes GetBestGrabbingType(GrabTypes preferred, bool forcePreference = false)
        {
            if (preferred == GrabTypes.Pinch)
            {
                if (_inputProvider.IsTriggerDown(HandSide))
                    return GrabTypes.Pinch;
                else if (forcePreference)
                    return GrabTypes.None;
            }
            if (preferred == GrabTypes.Grip)
            {
                if (_inputProvider.IsGripDown(HandSide))
                    return GrabTypes.Grip;
                else if (forcePreference)
                    return GrabTypes.None;
            }

            if (_inputProvider.IsTriggerDown(HandSide))
                return GrabTypes.Pinch;
            if (_inputProvider.IsGripDown(HandSide))
                return GrabTypes.Grip;

            return GrabTypes.None;
        }

    }

}