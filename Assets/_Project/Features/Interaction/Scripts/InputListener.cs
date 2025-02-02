using BacklineVR.Core;
using BacklineVR.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Interaction
{
    public enum InputMode { Default = 0, MagicCasting = 1, UI = 2 }
    [RequireComponent(typeof(Player))]
    public abstract class InputListener : MonoBehaviour
    {
        private protected Player _player;
        public abstract InputMode GetInputMode();
        private void Awake()
        {
            _player = GetComponent<Player>();
            Initialize();
        }
        /// <summary>
        /// Use this to initialize references that will be used by someone in setup
        /// </summary>
        private protected virtual void Initialize()
        {
        }
        private void Start()
        {
            Setup();   
        }
        /// <summary>
        /// Use this to perform setup operations before starting
        /// </summary>
        private protected virtual void Setup()
        {
        }
        public abstract void Subscribe(InputProvider provider);
        public abstract void Unsubscribe(InputProvider provider);
        private protected virtual void OnGripDown(HandSide hand, float strength)
        {

        }
        private protected virtual void OnGripUp(HandSide hand)
        {

        }
        private protected virtual void OnTriggerDown(HandSide hand, float strength)
        {

        }
        private protected virtual void OnTriggerUp(HandSide hand)
        {

        }
        private protected virtual void OnTranslate(Vector3 translation)
        {

        }
        private protected virtual void OnRotate(Vector3 rotation)
        {

        }
        private protected virtual void OnUpdateHMDVelocity(Vector3 velocity)
        {

        }
        private protected virtual void OnUpdateHMDPosition(Vector3 position)
        {

        }
        private protected virtual void OnUpdateVelocity(HandSide hand, Vector3 velocity)
        {

        }
        private protected virtual void OnUpdateAngularVelocity(HandSide hand, Vector3 angularVelocity)
        {

        }
        private protected virtual void OnUpdatePosition(HandSide hand, Vector3 position)
        {

        }
        private protected virtual void OnUpdateRotation(HandSide hand, Quaternion rotation)
        {

        }
        private protected virtual void OnControlPad(Vector2 position)
        {

        }

    }
}