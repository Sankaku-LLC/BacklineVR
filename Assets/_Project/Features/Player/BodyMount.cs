using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Core {
    /// <summary>
    /// If both hands are valid and facing the same direction but the head is not, assume body isn't rotating, just head
    /// If a hand is valid and facing the same direction as the head, begin lerping at speed dependent on degree of alignment
    /// </summary>
    public class BodyMount : MonoBehaviour
    {
        private const float HAND_VALIDATION_THRESHOLD = .8f;
        [SerializeField]
        private Transform _headset;
        [SerializeField]
        private Transform _handL;
        [SerializeField]
        private Transform _handR;

        [SerializeField]
        private Vector3 _offset;

        private Vector3 _previousForward;

        // Update is called once per frame
        private void Update()
        {
            var handLValid = _handL.up.y > HAND_VALIDATION_THRESHOLD;
            var handRValid = _handR.up.y > HAND_VALIDATION_THRESHOLD;

            if (!handLValid && !handRValid)//If neither of the hands are pointing down don't even bother adjusting mount
                return;

            var rotation = Quaternion.Euler(0, _headset.eulerAngles.y, 0);
            transform.position = _headset.position + rotation * _offset;
            transform.rotation = rotation;
            var headForward = _headset.forward;
            var handL = _handL.forward;
            var handR = _handR.forward;

            var handsAligned = Vector3.Dot(handL, handR) > HAND_VALIDATION_THRESHOLD;

            if (handRValid)
            {
                var handLAlignment = 0f;
            }
            var handRAlignment = 0f;

        }
    }
}
