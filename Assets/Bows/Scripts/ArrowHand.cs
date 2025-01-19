//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: The object attached to the player's hand that spawns and fires the
//			arrow
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BacklineVR.Core;
using BacklineVR.Casting;
using CurseVR.SymbolSystem;

namespace BacklineVR.Interaction.Bow
{
    public enum GrabTypes
    {
        None,
        Trigger,
        Pinch,
        Grip,
        Scripted,
    }
    //-------------------------------------------------------------------------
    public class ArrowHand : MonoBehaviour
	{
		private Hand hand;
		private Longbow bow;

		private GameObject currentArrow;

        [SerializeField]
        private GameObject _forceArrow;
        [SerializeField]
        private GameObject _fireArrow;
        [SerializeField]
		private GameObject _divineArrow;
        [SerializeField]
        private GameObject _lifeArrow;

        public Transform arrowNockTransform;

		public float nockDistance = 0.1f;
		public float lerpCompleteDistance = 0.08f;
		public float rotationLerpThreshold = 0.15f;
		public float positionLerpThreshold = 0.15f;

		private bool allowArrowSpawn = true;
		private bool nocked;
        private GrabTypes nockedWithType = GrabTypes.None;

		private bool inNockRange = false;
		private bool arrowLerpComplete = false;

		public SoundPlayOneshot arrowSpawnSound;

		public int maxArrowCount = 10;
		private List<GameObject> arrowList;


		//-------------------------------------------------
		void Awake()
		{
			arrowList = new List<GameObject>();
        }
        private void Start()
        {
			VRInput.Caster.OnSymbolCast += OnSymbolCast;
        }
        private void OnSymbolCast(ClassificationResult result)
		{
			if (!allowArrowSpawn || currentArrow != null)
				return;
            arrowSpawnSound.Play();
            Debug.LogError("SUCCESFUL CAST! " + result.MatchName + " " + result.MatchPercent);
			currentArrow = InstantiateArrow(result.MatchName);
        }

        //-------------------------------------------------
        private void OnAttachedToHand( Hand attachedHand )
		{
			hand = attachedHand;
			FindBow();
		}


		//-------------------------------------------------
		private GameObject InstantiateArrow(string spellName)
		{
			GameObject targetPrefab;
            if (spellName == "Heal")
            {
				targetPrefab = _lifeArrow;
            }
            else if (spellName == "Light")
            {
                targetPrefab = _divineArrow;

            }
            else if (spellName == "Fire")
            {
				targetPrefab = _fireArrow;
            }
            else
            {
				targetPrefab = _forceArrow;
            }

            GameObject arrow = Instantiate( targetPrefab, arrowNockTransform.position, arrowNockTransform.rotation ) as GameObject;
			arrow.name = "Arrow of " + spellName;
			arrow.transform.parent = arrowNockTransform;
			Util.ResetTransform( arrow.transform );

			arrowList.Add( arrow );

			while ( arrowList.Count > maxArrowCount )
			{
				GameObject oldArrow = arrowList[0];
				arrowList.RemoveAt( 0 );
				if ( oldArrow )
				{
					Destroy( oldArrow );
				}
			}

			return arrow;
		}


		//-------------------------------------------------
		private void HandAttachedUpdate( Hand hand )
		{
			if ( bow == null )
			{
				FindBow();
			}

			if ( bow == null )
			{
				return;
			}

			if (currentArrow == null)
				return;

			float distanceToNockPosition = Vector3.Distance( transform.parent.position, bow.nockTransform.position );

			// If there's an arrow spawned in the hand and it's not nocked yet
			if ( !nocked )
			{
				// If we're close enough to nock position that we want to start arrow rotation lerp, do so
				if ( distanceToNockPosition < rotationLerpThreshold )
				{
					float lerp = Util.RemapNumber( distanceToNockPosition, rotationLerpThreshold, lerpCompleteDistance, 0, 1 );

					arrowNockTransform.rotation = Quaternion.Lerp( arrowNockTransform.parent.rotation, bow.nockRestTransform.rotation, lerp );
				}
				else // Not close enough for rotation lerp, reset rotation
				{
					arrowNockTransform.localRotation = Quaternion.identity;
				}

				// If we're close enough to the nock position that we want to start arrow position lerp, do so
				if ( distanceToNockPosition < positionLerpThreshold )
				{
					float posLerp = Util.RemapNumber( distanceToNockPosition, positionLerpThreshold, lerpCompleteDistance, 0, 1 );

					posLerp = Mathf.Clamp( posLerp, 0f, 1f );

					arrowNockTransform.position = Vector3.Lerp( arrowNockTransform.parent.position, bow.nockRestTransform.position, posLerp );
				}
				else // Not close enough for position lerp, reset position
				{
					arrowNockTransform.position = arrowNockTransform.parent.position;
				}


				// Give a haptic tick when lerp is visually complete
				if ( distanceToNockPosition < lerpCompleteDistance )
				{
					if ( !arrowLerpComplete )
					{
						arrowLerpComplete = true;
						hand.TriggerHapticPulse( 500 );
					}
				}
				else
				{
					if ( arrowLerpComplete )
					{
						arrowLerpComplete = false;
					}
				}

				// Allow nocking the arrow when controller is close enough
				if ( distanceToNockPosition < nockDistance )
				{
					if ( !inNockRange )
					{
						inNockRange = true;
						bow.ArrowInPosition();
					}
				}
				else
				{
					if ( inNockRange )
					{
						inNockRange = false;
					}
				}

                GrabTypes bestGrab = hand.GetBestGrabbingType(GrabTypes.Grip, true);

                // If arrow is close enough to the nock position and we're pressing the grip, and we're not nocked yet, Nock
                if ( ( distanceToNockPosition < nockDistance ) && bestGrab != GrabTypes.None && !nocked )
				{
					nocked = true;
                    nockedWithType = bestGrab;
					bow.StartNock( this );
					hand.HoverLock( GetComponent<Interactable>() );
					currentArrow.transform.parent = bow.nockTransform;
					Util.ResetTransform( currentArrow.transform );
					Util.ResetTransform( arrowNockTransform );
				}
			}


			// If arrow is nocked, and we release the trigger
			if ( nocked && hand.IsGrabbingWithType(nockedWithType) == false )
			{
				if ( bow.pulled ) // If bow is pulled back far enough, fire arrow, otherwise reset arrow in arrowhand
				{
					FireArrow();
				}
				else
				{
					arrowNockTransform.rotation = currentArrow.transform.rotation;
					currentArrow.transform.parent = arrowNockTransform;
					Util.ResetTransform( currentArrow.transform );
					nocked = false;
                    nockedWithType = GrabTypes.None;
					bow.ReleaseNock();
					hand.HoverUnlock( GetComponent<Interactable>() );
				}

				bow.StartRotationLerp(); // Arrow is releasing from the bow, tell the bow to lerp back to controller rotation
			}
		}


		//-------------------------------------------------
		private void OnDetachedFromHand( Hand hand )
		{
			Destroy( gameObject );
		}


		//-------------------------------------------------
		private void FireArrow()
		{
			currentArrow.transform.parent = null;

			Arrow arrow = currentArrow.GetComponent<Arrow>();
			arrow.shaftRB.isKinematic = false;
			arrow.shaftRB.useGravity = true;
			arrow.shaftRB.transform.GetComponent<BoxCollider>().enabled = true;

			arrow.arrowHeadRB.isKinematic = false;
			arrow.arrowHeadRB.useGravity = true;
			arrow.arrowHeadRB.transform.GetComponent<BoxCollider>().enabled = true;

			arrow.arrowHeadRB.AddForce( currentArrow.transform.forward * bow.GetArrowVelocity(), ForceMode.VelocityChange );
			arrow.arrowHeadRB.AddTorque( currentArrow.transform.forward * 10 );

			nocked = false;
            nockedWithType = GrabTypes.None;

			currentArrow.GetComponent<Arrow>().ArrowReleased( bow.GetArrowVelocity() );
			bow.ArrowReleased();

			allowArrowSpawn = false;
			StartCoroutine(DoArrowSpawnCooldown());
			StartCoroutine( ArrowReleaseHaptics() );

			currentArrow = null;
		}

		private IEnumerator DoArrowSpawnCooldown()
		{
			yield return new WaitForSeconds(.5f);
            allowArrowSpawn = true;
        }


		//-------------------------------------------------
		private IEnumerator ArrowReleaseHaptics()
		{
			yield return new WaitForSeconds( 0.05f );

			hand.OtherHand.TriggerHapticPulse( 1500 );
			yield return new WaitForSeconds( 0.05f );

			hand.OtherHand.TriggerHapticPulse( 800 );
			yield return new WaitForSeconds( 0.05f );

			hand.OtherHand.TriggerHapticPulse( 500 );
			yield return new WaitForSeconds( 0.05f );

			hand.OtherHand.TriggerHapticPulse( 300 );
		}


        //-------------------------------------------------
        public void OnHandFocusLost( Hand hand )
		{
			gameObject.SetActive( false );
		}


		//-------------------------------------------------
		public void OnHandFocusAcquired( Hand hand )
		{
			gameObject.SetActive( true );
		}


		//-------------------------------------------------
		private void FindBow()
		{
			bow = hand.OtherHand.GetComponentInChildren<Longbow>();
		}
	}
}
