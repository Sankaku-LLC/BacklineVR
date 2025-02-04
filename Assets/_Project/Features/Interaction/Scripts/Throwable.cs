using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using BacklineVR.Core;

namespace BacklineVR.Interaction
{
    //-------------------------------------------------------------------------
    [RequireComponent(typeof(Holdable))]
    [RequireComponent(typeof(Rigidbody))]
    public class Throwable : MonoBehaviour
    {
        [Tooltip("How fast must this object be moving to attach due to a trigger hold instead of a trigger press? (-1 to disable)")]
        public float catchingSpeedThreshold = -1;

        public float scaleReleaseVelocity = 1.1f;

        [Tooltip("The release velocity magnitude representing the end of the scale release velocity curve. (-1 to disable)")]
        public float scaleReleaseVelocityThreshold = -1.0f;
        [Tooltip("Use this curve to ease into the scaled release velocity based on the magnitude of the measured release velocity. This allows greater differentiation between a drop, toss, and throw.")]
        public AnimationCurve scaleReleaseVelocityCurve = AnimationCurve.EaseInOut(0.0f, 0.1f, 1.0f, 1.0f);

        protected VelocityEstimator velocityEstimator;
        protected bool attached = false;
        protected float attachTime;

        protected RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;

        protected new Rigidbody rigidbody;

        [HideInInspector]
        public Holdable interactable;


        //-------------------------------------------------
        protected virtual void Awake()
        {
            velocityEstimator = GetComponent<VelocityEstimator>();
            interactable = GetComponent<Holdable>();

            interactable.OnGrab += OnGrab;
            interactable.OnRelease += OnRelease;


            rigidbody = GetComponent<Rigidbody>();
            rigidbody.maxAngularVelocity = 50.0f;
        }

        public bool CanCatch()
        {
            if (attached || catchingSpeedThreshold == -1)
                return false;
            var catchingThreshold = catchingSpeedThreshold * Player.Instance.Origin.lossyScale.x;
            return rigidbody.velocity.magnitude > catchingThreshold;
        }

        //-------------------------------------------------
        protected virtual void OnGrab()
        {
            interpolation = this.rigidbody.interpolation;

            attached = true;

            rigidbody.interpolation = RigidbodyInterpolation.None;

            if (velocityEstimator != null)
                velocityEstimator.BeginEstimatingVelocity();

            attachTime = Time.time;
        }


        //-------------------------------------------------
        protected virtual void OnRelease()
        {
            attached = false;

            rigidbody.interpolation = interpolation;

            GetReleaseVelocities(out var velocity, out var angularVelocity);

            rigidbody.velocity = velocity;
            rigidbody.angularVelocity = angularVelocity;
        }


        public virtual void GetReleaseVelocities(out Vector3 velocity, out Vector3 angularVelocity)
        {

            if (velocityEstimator != null)
            {
                velocityEstimator.FinishEstimatingVelocity();
                velocity = velocityEstimator.GetVelocityEstimate();
                angularVelocity = velocityEstimator.GetAngularVelocityEstimate();
            }
            else
            {
                Debug.LogWarning("No Velocity Estimator component on object but release style set to short estimation. Please add one or change the release style.");

                velocity = rigidbody.velocity;
                angularVelocity = rigidbody.angularVelocity;
            }

                float scaleFactor = 1.0f;
                if (scaleReleaseVelocityThreshold > 0)
                {
                    scaleFactor = Mathf.Clamp01(scaleReleaseVelocityCurve.Evaluate(velocity.magnitude / scaleReleaseVelocityThreshold));
                }

                velocity *= (scaleFactor * scaleReleaseVelocity);
        }
    }
}
