using CurseVR.Director;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CallbackContext = UnityEngine.InputSystem.InputAction.CallbackContext;
namespace BacklineVR.Interaction
{
    public class InputProvider : MonoBehaviour, IGlobalComponent
    {

        private enum Device { HMD, LeftHand, RightHand }
        private enum ActionCode
        {
            Position,
            Rotation,
            Velocity,
            AngularVelocity,
            Trigger,
            Grip,
            Move,
            Haptic,
            PrimaryButton
        }
        [SerializeField]
        private InputActionAsset _inputActions;
        [SerializeField]
        private float _snapTurnAngle = 45;
        [SerializeField]
        private Transform _leftHand;
        [SerializeField]
        private Transform _rightHand;
        private Dictionary<string, List<Action<CallbackContext>>> _bindings;

        public Action<Vector3> OnTranslate;
        public Action<Vector3> OnRotate;
        //Casting
        public Action<HandSide, Vector3> OnUpdatePosition;
        public Action<HandSide, Quaternion> OnUpdateRotation;
        public Action<HandSide, Vector3> OnUpdateVelocity;
        public Action<HandSide, Vector3> OnUpdateAngularVelocity;
        public Action<HandSide, float> OnTriggerDown;
        public Action<HandSide> OnTriggerUp;
        public Action<HandSide, float> OnGripDown;
        public Action<HandSide> OnGripUp;
        public Action<HandSide> OnPrimaryButtonDown;
        public Action<Vector2> OnControlPad;

        private bool[] _isGripDown = new bool[2];
        private bool[] _isTriggerDown = new bool[2];
        public void OnInitialize()
        {
            _bindings = new Dictionary<string, List<Action<CallbackContext>>>();
        }

        public void OnStart()
        {
            SetupActions();
        }

        public Type GetManagerType()
        {
            return typeof(InputProvider);
        }
        private void SetupActions()
        {
            SetupHandActions(HandSide.Left);
            SetupHandActions(HandSide.Right);
            Action<CallbackContext> leftPadPerformed = OnLeftPadUpdated;
            SetupBinding(Device.LeftHand, ActionCode.Move, OnLeftPadUpdated);
            Action<CallbackContext> rightPadPerformed = OnRightPadUpdated;
            SetupBinding(Device.RightHand, ActionCode.Move, OnRightPadUpdated);
        }
        private void SetupHandActions(HandSide hand)
        {
            var deviceHand = hand == HandSide.Left ? Device.LeftHand : Device.RightHand;

            Action<CallbackContext> gripPerformed = (CallbackContext context) => OnGripDownEvent(hand, context.ReadValue<float>());
            Action<CallbackContext> gripCanceled = (CallbackContext _) => OnGripUpEvent(hand);
            SetupBinding(deviceHand, ActionCode.Grip, gripPerformed, gripCanceled);

            Action<CallbackContext> triggerPerformed = (CallbackContext context) => OnTriggerDownEvent(hand, context.ReadValue<float>());
            Action<CallbackContext> triggerCanceled = (CallbackContext _) => OnTriggerUpEvent(hand);
            SetupBinding(deviceHand, ActionCode.Trigger, triggerPerformed, triggerCanceled);

            Action<CallbackContext> secondaryPerformed = (CallbackContext context) => OnPrimaryButtonDown?.Invoke(hand);
            SetupBinding(deviceHand, ActionCode.PrimaryButton, secondaryPerformed);

            Action<CallbackContext> positionPerformed = (CallbackContext context) => OnPositionUpdated(hand, context);
            SetupBinding(deviceHand, ActionCode.Position, positionPerformed);

            Action<CallbackContext> rotationPerformed = (CallbackContext context) => OnRotationUpdated(hand, context);
            SetupBinding(deviceHand, ActionCode.Rotation, rotationPerformed);

            Action<CallbackContext> velocityPerformed = (CallbackContext context) => OnVelocityUpdated(hand, context);
            SetupBinding(deviceHand, ActionCode.Velocity, velocityPerformed);

            Action<CallbackContext> angularVelocityPerformed = (CallbackContext context) => OnAngularVelocityUpdated(hand, context);
            SetupBinding(deviceHand, ActionCode.AngularVelocity, angularVelocityPerformed);

        }
        private protected void OnDestroy()
        {
            RemoveAllBindings();
        }
        public bool IsGripDown(HandSide handSide)
        {
            return _isGripDown[(int)handSide];
        }
        public bool IsTriggerDown(HandSide handSide)
        {
            return _isTriggerDown[(int)handSide];
        }
        private void OnGripDownEvent(HandSide handSide, float value)
        {
            if (!_isGripDown[(int)handSide])
            {
                _isGripDown[(int)handSide] = true;
                OnGripDown?.Invoke(handSide, value);
            }
        }
        private void OnGripUpEvent(HandSide handSide)
        {
            if (_isGripDown[(int)handSide])
            {
                _isGripDown[(int)handSide] = false;
                OnGripUp?.Invoke(handSide);
            }
        }
        private void OnTriggerDownEvent(HandSide handSide, float value)
        {
            if (!_isTriggerDown[(int)handSide])
            {
                _isTriggerDown[(int)handSide] = true;
                OnTriggerDown?.Invoke(handSide, value);
            }

        }
        private void OnTriggerUpEvent(HandSide handSide)
        {
            if (_isTriggerDown[(int)handSide])
            {
                _isTriggerDown[(int)handSide] = false;
                OnTriggerUp?.Invoke(handSide);
            }
        }
        private void OnRightPadUpdated(CallbackContext context)
        {
            var padValue = context.ReadValue<Vector2>();
            var cardinalized = GetCardinalDirection(padValue);
            var rotateY = _snapTurnAngle * cardinalized.x;
            OnRotate?.Invoke(new Vector3(0, rotateY, 0));
            OnControlPad?.Invoke(padValue);
        }
        private void OnLeftPadUpdated(CallbackContext context)
        {
            var translation = context.ReadValue<Vector2>();
            OnTranslate?.Invoke(new Vector3(translation.x, 0, translation.y));
        }
        private void OnPositionUpdated(HandSide hand, CallbackContext context)
        {
            var value = context.ReadValue<Vector3>();
            OnUpdatePosition?.Invoke(hand, value);
            if (hand == HandSide.Left)
                _leftHand.position = value;
            else
                _rightHand.position = value;
        }
        private void OnRotationUpdated(HandSide hand, CallbackContext context)
        {
            var value = context.ReadValue<Quaternion>();
            OnUpdateRotation?.Invoke(hand, context.ReadValue<Quaternion>());
            if (hand == HandSide.Left)
                _leftHand.rotation = value;
            else
                _rightHand.rotation = value;
        }
        private void OnVelocityUpdated(HandSide hand, CallbackContext context)
        {
            OnUpdateVelocity?.Invoke(hand, context.ReadValue<Vector3>());
        }
        private void OnAngularVelocityUpdated(HandSide hand, CallbackContext context)
        {
            OnUpdateAngularVelocity?.Invoke(hand, context.ReadValue<Vector3>());
        }
        private void SetupBinding(Device device, ActionCode code, Action<CallbackContext> onPerformed, Action<CallbackContext> onCanceled = null)
        {
            var actionCode = GetCode(device, code);
            var inputAction = _inputActions[actionCode];
            inputAction.Enable();
            var actionList = new List<Action<CallbackContext>>();
            inputAction.performed += onPerformed;
            actionList.Add(onPerformed);
            if (onCanceled != null)
            {
                inputAction.canceled += onCanceled;
                actionList.Add(onCanceled);
            }
            _bindings.Add(actionCode, actionList);
        }
        private void RemoveAllBindings()
        {
            foreach ((string key, List<Action<CallbackContext>> value) in _bindings)
            {
                var action = _inputActions[key];
                action.performed -= value[0];
                if (value.Count > 1)
                    action.canceled -= value[1];
            }
        }
        private string GetCode(Device device, ActionCode code)
        {
            return device + "/" + code;
        }


        public void RequestHapticPulse(HandSide hand, float amplitude, float duration, float frequency)
        {
            List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();

            if (hand == HandSide.Left)
                UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Left, devices);
            else if (hand == HandSide.Right)
                UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Right, devices);

            foreach (var device in devices)
            {
                UnityEngine.XR.HapticCapabilities capabilities;
                if (device.TryGetHapticCapabilities(out capabilities))
                {
                    if (capabilities.supportsImpulse)
                    {
                        StartCoroutine(DoHapticPulse(device, amplitude, duration, frequency));
                    }
                }
            }
        }
        private IEnumerator DoHapticPulse(UnityEngine.XR.InputDevice device, float amplitude, float duration, float frequency)
        {
            float period = 1f / frequency;
            //If it wouldn't end during the activation
            if (duration < period)
            {
                Debug.LogError("Duration of haptic pulse is less than period of haptic signal");
                yield break;
            }
            else
            {
                int iterations = (int)(duration / period);
                for (int i = 0; i < iterations; i++)
                {
                    device.SendHapticImpulse(0, amplitude, period);
                    yield return new WaitForSeconds(period);
                }
            }
        }
        public static Vector2 GetCardinalDirection(Vector2 input)
        {
            var angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            var absAngle = Mathf.Abs(angle);
            if (absAngle < 45f)
                return Vector2.right;
            if (absAngle > 135f)
                return Vector2.left;
            return angle >= 0f ? Vector2.up : Vector2.down;
        }
    }
}