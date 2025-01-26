//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: The bow
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BacklineVR.Core;

namespace BacklineVR.Interaction.Bow
{
	//-------------------------------------------------------------------------
	[RequireComponent(typeof(Interactable))]
	public class Longbow : MonoBehaviour
	{
		public Transform pivotTransform;
		public Transform handleTransform;

		public Transform nockTransform;
		public Transform nockRestTransform;

		public bool nocked;
		public bool pulled;

		private const float minPull = 0.05f;
		private const float maxPull = 0.5f;
		private float nockDistanceTravelled = 0f;
		private float hapticDistanceThreshold = 0.01f;
		private float lastTickDistance;
		private const float bowPullPulseStrengthLow = 100;
		private const float bowPullPulseStrengthHigh = 500;
		private Vector3 bowLeftVector;

		public float arrowMinVelocity = 3f;
		public float arrowMaxVelocity = 30f;
		private float arrowVelocity = 30f;

		private float minStrainTickTime = 0.1f;
		private float maxStrainTickTime = 0.5f;
		private float nextStrainTick = 0;

		private bool lerpBackToZeroRotation;
		private float lerpStartTime;
		private float lerpDuration = 0.15f;
		private Quaternion lerpStartRotation;

		private float nockLerpStartTime;

		private Quaternion nockLerpStartRotation;

		public float drawOffset = 0.06f;


		private Vector3 lateUpdatePos;
		private Quaternion lateUpdateRot;

		public SoundBowClick drawSound;
		private float drawTension;
		public SoundPlayOneshot arrowSlideSound;
		public SoundPlayOneshot releaseSound;
		public SoundPlayOneshot nockSound;

        //Linear mapping code
        private Animator _animator;
		private int _framesUnchanged;
		private float _currentValue;
		private float _targetValue;

		private bool _isGrabbed = false;
		private Transform _arrowNockTransform;

		private Interactable _interactable;
        private void Awake()
        {
            _animator = GetComponent<Animator>();
			_animator.speed = 0;

			_interactable = GetComponent<Interactable>();
			_interactable.OnGrab += OnGrab;
			_interactable.OnRelease += OnRelease;
			_interactable.OnActivate += OnActivate;
			_interactable.OnDeactivate += OnDeactivate;
			_interactable.OnHeldUpdate += OnUpdate;
        }

        private void Update()
        {
            if(_currentValue != _targetValue)
			{
				_currentValue = _targetValue;
				_animator.enabled = true;
				_animator.Play(0, 0, _currentValue);
				_framesUnchanged = 0;
			}
			else
			{
				_framesUnchanged++;
				if(_framesUnchanged > 2)
				{
					_animator.enabled = false;
				}
			}
        }

        //-------------------------------------------------
        public void OnGrab()
        {
			_isGrabbed = true;

            if (Player.Instance.IsLeftHanded)
                pivotTransform.localScale = new Vector3(1f, -1f, 1f);
            else
                pivotTransform.localScale = new Vector3(1f, 1f, 1f);

			_arrowNockTransform = Player.Instance.GetArrowNockTransform();
        }

        public void OnRelease()
        {
			_isGrabbed = false;
        }

        public void OnActivate()
        {
        }

        public void OnDeactivate()
        {
        }
		public void OnUpdate()
		{
            if (nocked)
            {
                Vector3 nockToarrowHand = (_arrowNockTransform.parent.position - nockRestTransform.position); // Vector from bow nock transform to arrowhand nock transform - used to align bow when drawing

                // Align bow
                // Time lerp value used for ramping into drawn bow orientation
                float lerp = Util.RemapNumberClamped(Time.time, nockLerpStartTime, (nockLerpStartTime + lerpDuration), 0f, 1f);

                float pullLerp = Util.RemapNumberClamped(nockToarrowHand.magnitude, minPull, maxPull, 0f, 1f); // Normalized current state of bow draw 0 - 1

                Vector3 arrowNockTransformToHeadset = ((Player.Instance.Head.position + (Vector3.down * 0.05f)) - _arrowNockTransform.parent.position).normalized;
                Vector3 arrowHandPosition = (_arrowNockTransform.parent.position + ((arrowNockTransformToHeadset * drawOffset) * pullLerp)); // Use this line to lerp arrowHand nock position

                Vector3 pivotToString = (arrowHandPosition - pivotTransform.position).normalized;
                Vector3 pivotToLowerHandle = (handleTransform.position - pivotTransform.position).normalized;
                bowLeftVector = -Vector3.Cross(pivotToLowerHandle, pivotToString);
                pivotTransform.rotation = Quaternion.Lerp(nockLerpStartRotation, Quaternion.LookRotation(pivotToString, bowLeftVector), lerp);

                // Move nock position
                if (Vector3.Dot(nockToarrowHand, -nockTransform.forward) > 0)
                {
                    float distanceToarrowHand = nockToarrowHand.magnitude * lerp;

                    nockTransform.localPosition = new Vector3(0f, 0f, Mathf.Clamp(-distanceToarrowHand, -maxPull, 0f));

                    nockDistanceTravelled = -nockTransform.localPosition.z;

                    arrowVelocity = Util.RemapNumber(nockDistanceTravelled, minPull, maxPull, arrowMinVelocity, arrowMaxVelocity);

                    drawTension = Util.RemapNumberClamped(nockDistanceTravelled, 0, maxPull, 0f, 1f);

                    _targetValue = drawTension; // Send drawTension value to LinearMapping script, which drives the bow draw animation

                    if (nockDistanceTravelled > minPull)
                    {
                        pulled = true;
                    }
                    else
                    {
                        pulled = false;
                    }

                    if ((nockDistanceTravelled > (lastTickDistance + hapticDistanceThreshold)) || nockDistanceTravelled < (lastTickDistance - hapticDistanceThreshold))
                    {
                        ushort hapticStrength = (ushort)Util.RemapNumber(nockDistanceTravelled, 0, maxPull, bowPullPulseStrengthLow, bowPullPulseStrengthHigh);
                        Player.Instance.TriggerHaptics(false, hapticStrength);
                        Player.Instance.TriggerHaptics(true, hapticStrength);

                        drawSound.PlayBowTensionClicks(drawTension);

                        lastTickDistance = nockDistanceTravelled;
                    }

                    if (nockDistanceTravelled >= maxPull)
                    {
                        if (Time.time > nextStrainTick)
                        {
                            Player.Instance.TriggerHaptics(false, 400);
                            Player.Instance.TriggerHaptics(true, 400);

                            drawSound.PlayBowTensionClicks(drawTension);

                            nextStrainTick = Time.time + Random.Range(minStrainTickTime, maxStrainTickTime);
                        }
                    }
                }
                else
                {
                    nockTransform.localPosition = new Vector3(0f, 0f, 0f);

                    _targetValue = 0f;
                }
            }
            else
            {
                if (lerpBackToZeroRotation)
                {
                    float lerp = Util.RemapNumber(Time.time, lerpStartTime, lerpStartTime + lerpDuration, 0, 1);

                    pivotTransform.localRotation = Quaternion.Lerp(lerpStartRotation, Quaternion.identity, lerp);

                    if (lerp >= 1)
                    {
                        lerpBackToZeroRotation = false;
                    }
                }
            }
        }

		//-------------------------------------------------
		public void ArrowReleased()
		{
			nocked = false;

			if ( releaseSound != null )
			{
				releaseSound.Play();
			}

			this.StartCoroutine( this.ResetDrawAnim() );
		}


		//-------------------------------------------------
		private IEnumerator ResetDrawAnim()
		{
			float startTime = Time.time;
			float startLerp = drawTension;

			while ( Time.time < ( startTime + 0.02f ) )
			{
				float lerp = Util.RemapNumberClamped( Time.time, startTime, startTime + 0.02f, startLerp, 0f );
				_targetValue = lerp;
				yield return null;
			}

			_targetValue = 0;

			yield break;
		}


		//-------------------------------------------------
		public float GetArrowVelocity()
		{
			return arrowVelocity;
		}


		//-------------------------------------------------
		public void StartRotationLerp()
		{
			lerpStartTime = Time.time;
			lerpBackToZeroRotation = true;
			lerpStartRotation = pivotTransform.localRotation;

			Util.ResetTransform( nockTransform );
		}


		//-------------------------------------------------
		public void StartNock( Quiver currentArrowHand )
		{
			nocked = true;
			nockLerpStartTime = Time.time;
			nockLerpStartRotation = pivotTransform.rotation;

			// Sound of arrow sliding on nock as it's being pulled back
			arrowSlideSound.Play();

			// Decide which hand we're drawing with and lerp to the correct side
		}


		//-------------------------------------------------
		public void ArrowInPosition()
		{
			if ( nockSound != null )
			{
				nockSound.Play();
			}
		}


		//-------------------------------------------------
		public void ReleaseNock()
		{
			// ArrowHand tells us to do this when we release the buttons when bow is nocked but not drawn far enough
			nocked = false;
			this.StartCoroutine( this.ResetDrawAnim() );
		}
    }
}
