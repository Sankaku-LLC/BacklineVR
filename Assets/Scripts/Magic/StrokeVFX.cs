using System;
using System.Collections;
using System.Collections.Generic;
using CurseVR.SymbolSystem;
using UnityEngine;
using UnityEngine.VFX;
using static UnityEngine.ParticleSystem;

namespace CurseVR.Core.Casting
{
    /// <summary>
    /// Note: The workings of this are incredibly complex, just assume it works
    /// </summary>
    public class StrokeVFX : MonoBehaviour, IStrokeProvider
    {
        private VisualEffect _vfx;
        private ParticleSystem _ps;
        private float _spawnTime;
        private void Awake()
        {
            _vfx = GetComponent<VisualEffect>();
            _ps = GetComponent<ParticleSystem>();
            _spawnTime = Time.time;
        }
        private void Start()
        {
            _vfx.Play();
            _ps.Play();
        }
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
        public Stroke EndStroke()
        {
            _vfx.SetBool("Done", true);
            return GetStroke();
        }
        private Stroke GetStroke()
        {
            ParticleSystem ps = gameObject.GetComponent<ParticleSystem>();
            if (ps.particleCount < SymbolProcessor.MINPOINTS)
                return null;

            Particle[] data = new Particle[ps.particleCount];
            ps.GetParticles(data);

            var segmentData = new List<Vector>();
            Array.Sort(data, Comparer<Particle>.Create((x, y) => Math.Sign(x.remainingLifetime - y.remainingLifetime)));

            foreach (Particle p in data)
            {
                Vector3 pos = Camera.main.transform.InverseTransformPoint(p.position);
                segmentData.Add(new Vector(pos));
            }
            return new Stroke(segmentData);
        }
        public void Destroy(Transform casterTransform, float time)
        {
            if(Mathf.Approximately(time, 0))
            {
                ForceDestroy();
                return;
            }
            StartCoroutine(DestroySequence(casterTransform, time));
        }
        /// <summary>
        /// Casted glyphs contribute to sphere in WS
        /// </summary>
        /// <param name="parent"></param>
        public void Destroy(Vector3 pos, float time)
        {
            if (Mathf.Approximately(time, 0))
            {
                ForceDestroy();
                return;
            }
            StartCoroutine(DestroySequence(pos, time));
        }
        public void ForceDestroy()
        {
            StopAllCoroutines();
            Destroy(gameObject);
        }
        /// <summary>
        /// Use if WS
        /// </summary>
        private IEnumerator DestroySequence(Vector3 parent, float lifespan)
        {
            //yield return null;
            //float strokeLifeSpan = _endTime - _startTime;

            //Make it play at 5 times speed
            //_strokeLifeSpan /= 5f;
            //strokeLifeSpan /= 5f;

            //Add the time buffer to the time it takes for the written path to disappear
            _vfx.SetBool("Registered", true);
            _vfx.SetFloat("DeltaTime", lifespan);
            _vfx.SetVector3("TargetPosition", parent);

            //How long this specific stroke was alive
            //_vfx.SetFloat("PathBuffer", strokeLifeSpan);

            float end = Time.time + lifespan;
            //Wait till it's done
            while (Time.time < end) // _strokeLifeSpan + strokeLifeSpan)
            {
                yield return null;
            }
            Destroy(gameObject);
        }
        /// <summary>
        /// Use if player space
        /// </summary>
        private IEnumerator DestroySequence(Transform center, float lifespan)
        {
            var timeSinceSpawn = Time.time - _spawnTime;//Use this to update its current timer to be based on now

            yield return null;
            //Add the time buffer to the time it takes for the written path to disappear
            _vfx.SetBool("Registered", true);
            _vfx.SetFloat("DeltaTime", timeSinceSpawn);
            _vfx.SetFloat("PathBuffer", lifespan);

            float endTime = Time.time + lifespan;
            //Wait till it's done
            while (Time.time < endTime)
            {
                _vfx.SetVector3("TargetPosition", center.position);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}