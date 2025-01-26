//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: The hands used by the player in the vr interaction system
//
//=============================================================================

using UnityEngine;
using System;
using System.Collections;
using BacklineVR.Core;

namespace BacklineVR.Interaction
{
    public enum HandSide { Left, Right };
    public class Hand : MonoBehaviour
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

        public const AttachmentFlags DEFAULT_ATTACHMENT_FLAGS = AttachmentFlags.ParentToHand |
                                                              AttachmentFlags.DetachFromOtherHand |
                                                              AttachmentFlags.TurnOnKinematic |
                                                              AttachmentFlags.SnapOnAttach;

        public Hand OtherHand;
        public HandSide HandSide;

        public Transform hoverSphereTransform;
        public float hoverSphereRadius = 0.05f;
        public LayerMask hoverLayerMask = -1;
        public float hoverUpdateInterval = 0.1f;


        [SerializeField, Tooltip("A transform on the hand to center attached objects on")]
        private Transform _objectAttachmentPoint;

        public bool showDebugText = false;
        public bool spewDebugText = false;
        public bool showDebugInteractables = false;

        public struct AttachedObjectData
        {
            public GameObject attachedObject;
            public Interactable interactable;
            public Rigidbody attachedRigidbody;
            public CollisionDetectionMode collisionDetectionMode;
            public bool attachedRigidbodyWasKinematic;
            public bool attachedRigidbodyUsedGravity;
            public GameObject originalParent;
            public bool isParentedToHand;
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

        private AttachedObjectData _objectData;

        public bool hoverLocked { get; private set; }

        private Interactable _hoveringInteractable;

        private TextMesh debugText;
        private int _prevOverlappingColliders = 0;

        private const int COLLIDER_ARRAY_SIZE = 32;
        private readonly Collider[] _overlappingColliders = new Collider[COLLIDER_ARRAY_SIZE];

        [SerializeField]
        private Transform _arrowNockTransform;

        private Player playerInstance;

        //-------------------------------------------------
        protected virtual void Awake()
        {
            if (hoverSphereTransform == null)
                hoverSphereTransform = this.transform;

            if (_objectAttachmentPoint == null)
                _objectAttachmentPoint = this.transform;
        }




        //-------------------------------------------------
        private void Start()
        {
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
        public Interactable hoveringInteractable
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
                        _hoveringInteractable.OnStopHover?.Invoke();
                    }

                    _hoveringInteractable = value;

                    if (_hoveringInteractable != null)
                    {
                        if (spewDebugText)
                            HandDebugLog("HoverBegin " + _hoveringInteractable.gameObject);
                        _hoveringInteractable.OnStartHover?.Invoke();
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
                return _objectData.attachedObject;
            }
        }

        public void GrabHovered()
        {
            AttachObject(hoveringInteractable);
        }

        //-------------------------------------------------
        // Attach a GameObject to this GameObject
        //
        // objectToAttach - The GameObject to attach
        // flags - The flags to use for attaching the object
        // attachmentPoint - Name of the GameObject in the hierarchy of this Hand which should act as the attachment point for this GameObject
        //-------------------------------------------------
        public void AttachObject(Interactable interactable, AttachmentFlags flags = DEFAULT_ATTACHMENT_FLAGS, Transform attachmentOffset = null)
        {
            AttachedObjectData attachedObject = new AttachedObjectData();
            attachedObject.attachmentFlags = flags;
            attachedObject.attachedOffsetTransform = attachmentOffset;
            attachedObject.attachTime = Time.time;

            if (flags == 0)
            {
                flags = DEFAULT_ATTACHMENT_FLAGS;
            }

            //Detach from the other hand if requested
            if (attachedObject.HasAttachFlag(AttachmentFlags.DetachFromOtherHand))
            {
                if (OtherHand != null)
                    OtherHand.DetachObject();
            }

            attachedObject.attachedObject = interactable.gameObject;
            attachedObject.interactable = interactable.GetComponent<Interactable>();
            attachedObject.handAttachmentPointTransform = this.transform;

                if (attachedObject.interactable.attachEaseIn)
                {
                    attachedObject.easeSourcePosition = attachedObject.attachedObject.transform.position;
                    attachedObject.easeSourceRotation = attachedObject.attachedObject.transform.rotation;
                    attachedObject.interactable.snapAttachEaseInCompleted = false;
                }

                if (attachedObject.interactable.UseHandObjectAttachmentPoint)
                    attachedObject.handAttachmentPointTransform = _objectAttachmentPoint;

            attachedObject.originalParent = interactable.transform.parent != null ? interactable.transform.parent.gameObject : null;

            attachedObject.attachedRigidbody = interactable.GetComponent<Rigidbody>();
            if (attachedObject.attachedRigidbody != null)
            {
                if (attachedObject.interactable.Owner != null) //already attached to another hand
                {
                    //if it was attached to another hand, get the flags from that hand

                    AttachedObjectData attachedObjectInList = attachedObject.interactable.Owner._objectData;
                    attachedObject.attachedRigidbodyWasKinematic = attachedObjectInList.attachedRigidbodyWasKinematic;
                    attachedObject.attachedRigidbodyUsedGravity = attachedObjectInList.attachedRigidbodyUsedGravity;
                    attachedObject.originalParent = attachedObjectInList.originalParent;
                }
                else
                {
                    attachedObject.attachedRigidbodyWasKinematic = attachedObject.attachedRigidbody.isKinematic;
                    attachedObject.attachedRigidbodyUsedGravity = attachedObject.attachedRigidbody.useGravity;
                }
            }

            if (attachedObject.HasAttachFlag(AttachmentFlags.ParentToHand))
            {
                //Parent the object to the hand
                interactable.transform.parent = this.transform;
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
                    Quaternion rotDiff = Quaternion.Inverse(attachmentOffset.transform.rotation) * interactable.transform.rotation;
                    interactable.transform.rotation = attachedObject.handAttachmentPointTransform.rotation * rotDiff;

                    Vector3 posDiff = interactable.transform.position - attachmentOffset.transform.position;
                    interactable.transform.position = attachedObject.handAttachmentPointTransform.position + posDiff;
                }
                else
                {
                    //snap the object to the center of the attach point
                    interactable.transform.rotation = attachedObject.handAttachmentPointTransform.rotation;
                    interactable.transform.position = attachedObject.handAttachmentPointTransform.position;
                }

                Transform followPoint = interactable.transform;

                attachedObject.initialPositionalOffset = attachedObject.handAttachmentPointTransform.InverseTransformPoint(followPoint.position);
                attachedObject.initialRotationalOffset = Quaternion.Inverse(attachedObject.handAttachmentPointTransform.rotation) * followPoint.rotation;
            }
            else
            {
                if (attachmentOffset != null)
                {
                    //get the initial positional and rotational offsets between the hand and the offset transform
                    Quaternion rotDiff = Quaternion.Inverse(attachmentOffset.transform.rotation) * interactable.transform.rotation;
                    Quaternion targetRotation = attachedObject.handAttachmentPointTransform.rotation * rotDiff;
                    Quaternion rotationPositionBy = targetRotation * Quaternion.Inverse(interactable.transform.rotation);

                    Vector3 posDiff = (rotationPositionBy * interactable.transform.position) - (rotationPositionBy * attachmentOffset.transform.position);

                    attachedObject.initialPositionalOffset = attachedObject.handAttachmentPointTransform.InverseTransformPoint(attachedObject.handAttachmentPointTransform.position + posDiff);
                    attachedObject.initialRotationalOffset = Quaternion.Inverse(attachedObject.handAttachmentPointTransform.rotation) * (attachedObject.handAttachmentPointTransform.rotation * rotDiff);
                }
                else
                {
                    attachedObject.initialPositionalOffset = attachedObject.handAttachmentPointTransform.InverseTransformPoint(interactable.transform.position);
                    attachedObject.initialRotationalOffset = Quaternion.Inverse(attachedObject.handAttachmentPointTransform.rotation) * interactable.transform.rotation;
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

            if (attachedObject.interactable.attachEaseIn)
            {
                attachedObject.attachedObject.transform.position = attachedObject.easeSourcePosition;
                attachedObject.attachedObject.transform.rotation = attachedObject.easeSourceRotation;
            }

            _objectData = attachedObject;
            UpdateHovering();
            attachedObject.interactable.Attach(this);

            if (spewDebugText)
                HandDebugLog("AttachObject " + interactable);

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
        public void DetachObject(bool restoreOriginalParent = true)
        {
            if (spewDebugText)
                HandDebugLog("DetachObject " + _objectData);

            GameObject prevTopObject = currentAttachedObject;



            Transform parentTransform = null;
            if (_objectData.isParentedToHand)
            {
                if (restoreOriginalParent && (_objectData.originalParent != null))
                {
                    parentTransform = _objectData.originalParent.transform;
                }

                if (_objectData.attachedObject != null)
                {
                    _objectData.attachedObject.transform.parent = parentTransform;
                }
            }

            if (_objectData.HasAttachFlag(AttachmentFlags.TurnOnKinematic))
            {
                if (_objectData.attachedRigidbody != null)
                {
                    _objectData.attachedRigidbody.isKinematic = _objectData.attachedRigidbodyWasKinematic;
                    _objectData.attachedRigidbody.collisionDetectionMode = _objectData.collisionDetectionMode;
                }
            }

            if (_objectData.HasAttachFlag(AttachmentFlags.TurnOffGravity))
            {
                if (_objectData.attachedObject != null)
                {
                    if (_objectData.attachedRigidbody != null)
                        _objectData.attachedRigidbody.useGravity = _objectData.attachedRigidbodyUsedGravity;
                }
            }


            if (_objectData.attachedObject != null)
            {
                if (_objectData.interactable.IsDestroying == false)
                    _objectData.attachedObject.SetActive(true);

                _objectData.interactable.Detach(this);
            }

            GameObject newTopObject = currentAttachedObject;

            hoverLocked = false;


        }


        //-------------------------------------------------
        protected virtual void UpdateHovering()
        {
            if (hoverLocked)
                return;

            float closestDistance = float.MaxValue;
            Interactable closestInteractable = null;

            float scaledHoverRadius = hoverSphereRadius * Mathf.Abs(hoverSphereTransform.lossyScale.x);
            CheckHoveringForTransform(hoverSphereTransform.position, scaledHoverRadius, ref closestDistance, ref closestInteractable, Color.green);

            // Hover on this one
            hoveringInteractable = closestInteractable;
        }

        protected virtual bool CheckHoveringForTransform(Vector3 hoverPosition, float hoverRadius, ref float closestDistance, ref Interactable closestInteractable, Color debugColor)
        {
            bool foundCloser = false;

            // null out old vals
            for (int i = 0; i < _overlappingColliders.Length; ++i)
            {
                _overlappingColliders[i] = null;
            }

            int numColliding = Physics.OverlapSphereNonAlloc(hoverPosition, hoverRadius, _overlappingColliders, hoverLayerMask.value);

            if (numColliding >= COLLIDER_ARRAY_SIZE)
                Debug.LogWarning("<b>[SteamVR Interaction]</b> This hand is overlapping the max number of colliders: " + COLLIDER_ARRAY_SIZE + ". Some collisions may be missed. Increase ColliderArraySize on Hand.cs");

            // DebugVar
            int iActualColliderCount = 0;

            // Pick the closest hovering
            for (int colliderIndex = 0; colliderIndex < _overlappingColliders.Length; colliderIndex++)
            {
                Collider collider = _overlappingColliders[colliderIndex];

                if (collider == null)
                    continue;

                var contacting = collider.GetComponentInParent<Interactable>();

                // Yeah, it's null, skip
                if (contacting == null)
                    continue;

                // Can't hover over the object if it's attached
                if (_objectData.attachedObject == contacting.gameObject)
                    continue;

                // Best candidate so far...
                float distance = Vector3.Distance(contacting.transform.position, hoverPosition);
                bool lowerPriority = false;
                if (closestInteractable != null)
                { // compare to closest interactable to check priority
                    lowerPriority = contacting.HoverPriority < closestInteractable.HoverPriority;
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

            if (iActualColliderCount > 0 && iActualColliderCount != _prevOverlappingColliders)
            {
                _prevOverlappingColliders = iActualColliderCount;

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
                    _objectData.attachedObject,
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
        public Action thing;

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
            if(currentAttachedObject != null)
            {
                _objectData.interactable.OnHeldUpdate?.Invoke();
            }
            HandFollowUpdate();
        }

        protected virtual void HandFollowUpdate()
        {
            GameObject attachedObject = currentAttachedObject;
            if (attachedObject != null)
            {
                if (_objectData.interactable != null)
                {

                    if (_objectData.interactable.handFollowTransform)
                    {
                        Quaternion targetHandRotation;
                        Vector3 targetHandPosition;
                        Quaternion offset = Quaternion.Inverse(this.transform.rotation) * _objectData.handAttachmentPointTransform.rotation;
                        targetHandRotation = _objectData.interactable.transform.rotation * Quaternion.Inverse(offset);

                        Vector3 worldOffset = (this.transform.position - _objectData.handAttachmentPointTransform.position);
                        Quaternion rotationDiff = Quaternion.identity;
                        Vector3 localOffset = rotationDiff * worldOffset;
                        targetHandPosition = _objectData.interactable.transform.position + localOffset;
                    }
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (currentAttachedObject != null)
            {
                AttachedObjectData attachedInfo = _objectData;
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

        protected void UpdateAttachedVelocity(AttachedObjectData attachedObjectInfo)
        {
            Vector3 velocityTarget, angularTarget;
            bool success = GetUpdatedAttachedVelocities(attachedObjectInfo, out velocityTarget, out angularTarget);
            if (success)
            {
                float scale = _objectData.handAttachmentPointTransform.lossyScale.x;
                float maxAngularVelocityChange = MaxAngularVelocityChange * scale;
                float maxVelocityChange = MaxVelocityChange * scale;

                attachedObjectInfo.attachedRigidbody.velocity = Vector3.MoveTowards(attachedObjectInfo.attachedRigidbody.velocity, velocityTarget, maxVelocityChange);
                attachedObjectInfo.attachedRigidbody.angularVelocity = Vector3.MoveTowards(attachedObjectInfo.attachedRigidbody.angularVelocity, angularTarget, maxAngularVelocityChange);
            }
        }

        /// <summary>
        /// Snap an attached object to its target position and rotation. Good for error correction.
        /// </summary>
        public void ResetAttachedTransform(AttachedObjectData attachedObject)
        {
            attachedObject.attachedObject.transform.position = TargetItemPosition(attachedObject);
            attachedObject.attachedObject.transform.rotation = TargetItemRotation(attachedObject);
        }

        protected Vector3 TargetItemPosition(AttachedObjectData attachedObject)
        {
            return _objectData.handAttachmentPointTransform.TransformPoint(attachedObject.initialPositionalOffset);
        }

        protected Quaternion TargetItemRotation(AttachedObjectData attachedObject)
        {
            return _objectData.handAttachmentPointTransform.rotation * attachedObject.initialRotationalOffset;
        }

        protected bool GetUpdatedAttachedVelocities(AttachedObjectData attachedObjectInfo, out Vector3 velocityTarget, out Vector3 angularTarget)
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
        public void HoverLock(Interactable interactable)
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
        public void HoverUnlock(Interactable interactable)
        {
            if (spewDebugText)
                HandDebugLog("HoverUnlock " + interactable);

            if (hoveringInteractable == interactable)
            {
                hoverLocked = false;
            }
        }

        public Transform ArrowNockTransform
        {
            get
            {
                return _arrowNockTransform;
            }
        }
    }

}