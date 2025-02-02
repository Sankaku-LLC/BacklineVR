using System;
using System.Collections;
using System.Collections.Generic;
using BacklineVR.Core;
using BacklineVR.Interaction;
using CurseVR.Director;
using CurseVR.SymbolSystem;
using UnityEngine;
namespace BacklineVR.Casting
{
    /// <summary>
    /// This class acts as the input method for all VR input for stroke inputs. Reserved for input related activity
    /// Note that a single static access point is used, rather than two. This is for easy handling of dual casting
    /// ASSUMPTION: This will be bound before localplayer binds it's grab methods
    /// This class has absolute priority in grabbing
    /// </summary>
    public class GrabCaster : MonoBehaviour
    {
        [SerializeField]
        private Transform _casterTransform;
        [SerializeField]
        private GameObject _strokePrefab;//StrokePrefab is a gameobject with a particle system that spawns over space to create a trail
        private List<StrokeCapture> _strokeData;

        private bool _drawing = false;//Used to keep track if we're currently casting, so that we can terminate running strokes as needed
        private Vector3 _cursorPos;//Used to cache the current pos in case we spawn and need coords to spawn it at (moving after spawning will generate points)
        private StrokeCapture _activeCapture;

        public Action OnStrokeStart;
        public Action OnStrokeEnd;
        public Action OnCast;

        private protected virtual void Awake()
        {
            _strokeData = new List<StrokeCapture>();
        }
        public void Start()
        {
        }
        private void UpdatePosition(HandSide hand, Vector3 position)
        {
            _cursorPos = position;
        }
        public void StartStroke()
        {
            if (_drawing)
                return;
            _drawing = true;
            var cursor = Instantiate(_strokePrefab, _cursorPos, Quaternion.identity).GetComponent<IStrokeProvider>();
            _activeCapture = new StrokeCapture(Time.time, cursor);
            OnStrokeStart?.Invoke();
        }
        /// <summary>
        /// Registers a spell, clears out past spell glyph components
        /// </summary>
        public bool TryCastSymbol(out SymbolData data)
        {
            //NOTE: A cooldown on capturing was implemented at one point, it has been removed since I don't see why it's needed, and would be easy to re-implement

            //if clicked Trigger before Grip, immediately stop 
            if (_drawing)
                TryEndStroke();
            if (_strokeData.Count > 0)
            {
                var processedSingle = GetSingle();
                data = processedSingle;
                OnCast?.Invoke();
                ClearSingleStrokeData();
                return true;
            }
            data = null;
            ClearSingleStrokeData();
            return false;
        }
        public void ClearSingleStrokeData()
        {
            DestroyStrokes(_strokeData);
            _strokeData.Clear();
        }

        private void Update()
        {
            if (_activeCapture != null)
                _activeCapture.StrokeProvider.SetPosition(_cursorPos);
        }


        /// <summary>
        /// Ends the previous stroke. If the stroke is too short, then delete it
        /// </summary>
        public bool TryEndStroke()
        {
            if (!_drawing || _activeCapture == null)
                return false;

            _drawing = false;
            _activeCapture.EndStroke(Time.time, Camera.main.transform.localPosition, Camera.main.transform.localRotation);
            OnStrokeEnd?.Invoke();
            if (_activeCapture.Stroke == null)//if it was too short
            {
                _activeCapture.StrokeProvider.Destroy(_casterTransform, 0);
                _activeCapture = null;
                return false;
            }
            _strokeData.Add(_activeCapture);
            _activeCapture = null;
            return true;
        }
        private SymbolData GetSingle()
        {
            return SymbolProcessor.ProcessStrokes(_strokeData, 3);
        }

        private protected void DestroyStrokes(List<StrokeCapture> currentStrokeList)
        {
            StartCoroutine(DestroySequenceStaggered(new List<StrokeCapture>(currentStrokeList)));//Need to clone since source will clear their list
        }
        private IEnumerator DestroySequenceStaggered(List<StrokeCapture> strokeList)
        {
            //var runningDuration = 0f;
            var nextDelay = 0f;
            while (strokeList.Count > 0)
            {
                var strokeDuration = strokeList[0].EndTime - strokeList[0].StartTime;
                //runningDuration += strokeDuration;
                strokeList[0].StrokeProvider.Destroy(_casterTransform, strokeDuration);//How long this stroke will last
                if (strokeList.Count > 1)
                {
                    nextDelay = strokeList[1].StartTime - strokeList[0].EndTime;//how long to wait before destroying next
                }
                //runningDuration += 0.5f;//nextDelay;  Wait .5 seconds between last stroke and triggering next
                strokeList.RemoveAt(0);
                yield return new WaitForSeconds(nextDelay);
            }
        }
    }
}