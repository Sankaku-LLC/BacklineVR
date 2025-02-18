using BacklineVR.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Magic
{
    /// <summary>
    /// Scorching ray will fly forwards upon spawning and enable the hit prefab upon collision
    /// </summary>
    public class ScorchingRay : Spell
    {
        [SerializeField]
        private GameObject _launchFX;
        [SerializeField]
        private GameObject _trailFX;
        [SerializeField]
        private GameObject _hitFX;
        [SerializeField]
        private float _launchVelocity;
        [SerializeField]
        private float _launchDelay;

        private bool _armed = false;
        private Vector3 _launchDirection;
        private Transform _projectileTransform;
        private CollisionListener _collisionListener;
        private void Awake()
        {
            _collisionListener = GetComponentInChildren<CollisionListener>(true);
            _collisionListener.OnTriggerStart += OnTriggerStart;
            _projectileTransform = _trailFX.transform;
        }
        private IEnumerator Start()
        {
            _launchDirection = transform.forward;
            _launchFX.SetActive(true);
            yield return new WaitForSeconds(_launchDelay);
            _trailFX.SetActive(true);
            _armed = true;
        }
        private void OnTriggerStart(Collider other)
        {
            if (!_armed)
                return;
            Debug.LogError("Armed and colliding with " + other.name);
            _trailFX.SetActive(false);
            _hitFX.transform.SetPositionAndRotation(_projectileTransform.position, _projectileTransform.rotation);
            _hitFX.SetActive(true);
            Destroy(gameObject, 5);
        }
        private void Update()
        {
            if (!_armed)
                return;
            _projectileTransform.position += _launchDirection * _launchVelocity * Time.deltaTime;
        }
    }
}