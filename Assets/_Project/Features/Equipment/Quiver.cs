using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BacklineVR.Core;
using BacklineVR.Interaction;

namespace BacklineVR.Items
{
    [RequireComponent(typeof(Holdable))]
    public class Quiver : MonoBehaviour
	{
        [SerializeField]
		private Longbow _bow;

		private GameObject currentArrow;

        [SerializeField]
        private GameObject _forceArrow;

		public float nockDistance = 0.1f;
		public float lerpCompleteDistance = 0.08f;
		public float rotationLerpThreshold = 0.15f;
		public float positionLerpThreshold = 0.15f;

		private bool nocked;

		private bool inNockRange = false;
		private bool arrowLerpComplete = false;

		public SoundPlayOneshot arrowSpawnSound;

		private bool _nockReady = true;
        private Transform _arrowNockTransform;

        private Holdable _interactable;
		//-------------------------------------------------
		void Awake()
		{
            _interactable = GetComponent<Holdable>();
            _interactable.OnGrab += OnGrab;
            _interactable.OnRelease += OnRelease;
            _interactable.OnHeldUpdate += OnUpdate;
        }
        private void Start()
        {
        }


        public void OnGrab()
        {
            if(currentArrow != null)
            {
                Debug.LogError("Arrow is present but grabbed was called again before release!");
                return;
            }
            _arrowNockTransform = _interactable.Owner.ArrowNockTransform;//Quiver is always used by dominant hand

            //TODO: This should disable grab casting on dominant hand
            currentArrow = Instantiate(_forceArrow, _arrowNockTransform.position, _arrowNockTransform.rotation);
            currentArrow.transform.parent = _arrowNockTransform;
            Util.ResetTransform(currentArrow.transform);
            arrowSpawnSound.Play();
            _nockReady = true;
        }

        public void OnRelease()
        {
            _nockReady = false;
            //On dropping the item
            if (!nocked)
            {
                DestroyArrow();
                _arrowNockTransform = null;
                return;
            }

            if (_bow.pulled) // If bow is pulled back far enough, fire arrow, otherwise reset arrow in arrowhand
            {
                FireArrow();
            }
            else
            {
                _arrowNockTransform.rotation = currentArrow.transform.rotation;
                nocked = false;
                _bow.ReleaseNock();
                DestroyArrow();
            }
            _arrowNockTransform = null;
            _bow.StartRotationLerp(); // Arrow is releasing from the bow, tell the bow to lerp back to controller rotation
        }

        public void OnUpdate()
        {
            if (currentArrow == null)
                return;

            float distanceToNockPosition = Vector3.Distance(_arrowNockTransform.parent.position, _bow.nockTransform.position);

            // If there's an arrow spawned in the hand and it's not nocked yet
            if (!nocked)
            {
                // If we're close enough to nock position that we want to start arrow rotation lerp, do so
                if (distanceToNockPosition < rotationLerpThreshold)
                {
                    float lerp = Util.RemapNumber(distanceToNockPosition, rotationLerpThreshold, lerpCompleteDistance, 0, 1);

                    _arrowNockTransform.rotation = Quaternion.Lerp(_arrowNockTransform.parent.rotation, _bow.nockRestTransform.rotation, lerp);
                }
                else // Not close enough for rotation lerp, reset rotation
                {
                    _arrowNockTransform.localRotation = Quaternion.identity;
                }

                // If we're close enough to the nock position that we want to start arrow position lerp, do so
                if (distanceToNockPosition < positionLerpThreshold)
                {
                    float posLerp = Util.RemapNumber(distanceToNockPosition, positionLerpThreshold, lerpCompleteDistance, 0, 1);

                    posLerp = Mathf.Clamp(posLerp, 0f, 1f);

                    _arrowNockTransform.position = Vector3.Lerp(_arrowNockTransform.parent.position, _bow.nockRestTransform.position, posLerp);
                }
                else // Not close enough for position lerp, reset position
                {
                    _arrowNockTransform.position = _arrowNockTransform.parent.position;
                }


                // Give a haptic tick when lerp is visually complete
                if (distanceToNockPosition < lerpCompleteDistance)
                {
                    if (!arrowLerpComplete)
                    {
                        arrowLerpComplete = true;
                        Player.Instance.TriggerHaptics(true, 500);//Launchables are all in dominant hand
                    }
                }
                else
                {
                    if (arrowLerpComplete)
                    {
                        arrowLerpComplete = false;
                    }
                }

                // Allow nocking the arrow when controller is close enough
                if (distanceToNockPosition < nockDistance)
                {
                    if (!inNockRange)
                    {
                        inNockRange = true;
                        _bow.ArrowInPosition();
                    }
                }
                else
                {
                    if (inNockRange)
                    {
                        inNockRange = false;
                    }
                }

                // If arrow is close enough to the nock position and we're pressing the grip, and we're not nocked yet, Nock
                if (inNockRange && _nockReady && !nocked)
                {
                    nocked = true;
                    _bow.StartNock(this);
                    currentArrow.transform.parent = _bow.nockTransform;
                    Util.ResetTransform(currentArrow.transform);
                    Util.ResetTransform(_arrowNockTransform);
                }
            }
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

			arrow.arrowHeadRB.AddForce( currentArrow.transform.forward * _bow.GetArrowVelocity(), ForceMode.VelocityChange );
			arrow.arrowHeadRB.AddTorque( currentArrow.transform.forward * 10 );

			nocked = false;

			currentArrow.GetComponent<Arrow>().ArrowReleased( _bow.GetArrowVelocity() );
			_bow.ArrowReleased();

			StartCoroutine( ArrowReleaseHaptics() );

			currentArrow = null;
		}
        private void DestroyArrow()
        {
            Debug.LogError("Destroying arrow");
            Destroy(currentArrow);
            currentArrow = null;
        }

		//-------------------------------------------------
		private IEnumerator ArrowReleaseHaptics()
		{
			yield return new WaitForSeconds( 0.05f );
			Player.Instance.TriggerHaptics(false, 1500);

			yield return new WaitForSeconds( 0.05f );
            Player.Instance.TriggerHaptics(false, 800);

			yield return new WaitForSeconds( 0.05f );
            Player.Instance.TriggerHaptics(false, 500);

			yield return new WaitForSeconds( 0.05f );
            Player.Instance.TriggerHaptics(false, 300);
		}

    }
}
