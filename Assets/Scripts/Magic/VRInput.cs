using System;
using System.Collections;
using System.Collections.Generic;
using CurseVR.SymbolSystem;
using UnityEngine;
namespace BacklineVR.Casting
{
    public enum MirrorSettings { None = 1, MirrorLeft = 2, MirrorRight = 3 }
    /// <summary>
    /// This class acts as the input method for all VR input for stroke inputs. Reserved for input related activity
    /// Note that a single static access point is used, rather than two. This is for easy handling of dual casting
    /// ASSUMPTION: This will be bound before localplayer binds it's grab methods
    /// This class has absolute priority in grabbing
    /// </summary>
    public class VRInput : MonoBehaviour
    {
        public static List<VRInput> Inputs = new List<VRInput>();

        [SerializeField]
        private protected Transform _casterTransform;
        [SerializeField]
        private protected GameObject _strokePrefab;//StrokePrefab is a gameobject with a particle system that spawns over space to create a trail
        private protected List<StrokeCapture> _strokeData;

        private protected bool _drawing = false;//Used to keep track if we're currently casting, so that we can terminate running strokes as needed
        private protected Vector3 _cursorPos;//Used to cache the current pos in case we spawn and need coords to spawn it at (moving after spawning will generate points)
        private protected StrokeCapture _activeCapture;
        private int _inputIndex = 0;


        public static MirrorSettings ActiveMirrorSettings;
        public Action OnStrokeStart;
        public Action OnStrokeEnd;
        public Action OnCast;
        private static VRInput _left;
        private static VRInput _right;

        [SerializeField]
        private bool _isLeft;
        private protected virtual void Awake()
        {
            _strokeData = new List<StrokeCapture>();

            _inputIndex = Inputs.Count;
            Inputs.Add(this);
            if (_isLeft)
            {
                _left = this;
            }
            else
            {
                _right = this;
            }
        }
        private protected void OnDestroy()
        {
            Inputs.Remove(this);
        }

        /// <summary>
        /// Registers a spell, clears out past spell glyph components
        /// </summary>
        public bool TryCastSymbol(out List<SymbolData> data)
        {
            //NOTE: A cooldown on capturing was implemented at one point, it has been removed since I don't see why it's needed, and would be easy to re-implement

            //if clicked Trigger before Grip, immediately stop 
            if (_drawing)
                TryEndStroke();

            data = new List<SymbolData>();

            if (_strokeData.Count > 0)
            {
                var processedSingle = GetSingle();
                data.Add(processedSingle);
            }

            SymbolData processedCombined = null;
            if (_left._strokeData.Count > 0 && _right._strokeData.Count > 0)
            {
                processedCombined = GetCombined();
                data.Add(processedCombined);
            }

            if(data.Count > 0)
            {
                OnCast?.Invoke();
                return true;
            }
            return false;
        }
        public void ClearSingleStrokeData()
        {
            DestroyStrokes(_strokeData);
            _strokeData.Clear();
        }
        public void ClearCombinedStrokeData()
        {
            DestroyStrokes(GetCombinedRaw());//This clears from the clone
            _left._strokeData.Clear();
            _right._strokeData.Clear();
        }
        private void Update()
        {
            _cursorPos = transform.position;
            if (_activeCapture != null)
                _activeCapture.StrokeProvider.SetPosition(_cursorPos);
        }

        /// <summary>
        /// Adds a new stroke
        /// </summary>
        public void BeginStroke()
        {
            _drawing = true;
            var cursor = Instantiate(_strokePrefab, _cursorPos, Quaternion.identity).GetComponent<IStrokeProvider>();
            _activeCapture = new StrokeCapture(Time.time, cursor);
            OnStrokeStart?.Invoke();
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
            if (_isLeft && ActiveMirrorSettings == MirrorSettings.MirrorLeft || !_isLeft && ActiveMirrorSettings == MirrorSettings.MirrorRight)
            {
                var mirroredList = new List<StrokeCapture>(_strokeData.Count);
                for (var i = 0; i < _strokeData.Count; i++)
                {
                    var clonedCapture = new StrokeCapture(_strokeData[i]);
                    clonedCapture.Stroke = MirrorStrokeHorizontally(_strokeData[i].Stroke);
                    mirroredList.Add(clonedCapture);
                }
                return SymbolProcessor.ProcessStrokes(mirroredList, 3);
            }
            return SymbolProcessor.ProcessStrokes(_strokeData, 3);
        }
        private SymbolData GetCombined()
        {
            return SymbolProcessor.ProcessStrokes(GetCombinedRaw(), 3);
        }
        private List<StrokeCapture> GetCombinedRaw()
        {
            var leftIdx = 0;
            var rightIdx = 0;
            var leftStrokeData = _left._strokeData;
            var rightStrokeData = _right._strokeData;
            var combinedList = new List<StrokeCapture>(leftStrokeData.Count + rightStrokeData.Count);
            for (var i = 0; i < leftStrokeData.Count + rightStrokeData.Count; i++)
            {
                //If either of them run out, finish rest adding the other one
                var leftStrokeTime = leftIdx < leftStrokeData.Count ? leftStrokeData[leftIdx].StartTime : float.MaxValue;
                var rightStrokeTime = rightIdx < rightStrokeData.Count ? rightStrokeData[rightIdx].StartTime : float.MaxValue;
                if (leftStrokeTime < rightStrokeTime)
                {
                    combinedList.Add(leftStrokeData[leftIdx]);
                    leftIdx++;
                }
                else
                {
                    combinedList.Add(rightStrokeData[rightIdx]);
                    rightIdx++;
                }
            }
            return combinedList;
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
        private Stroke MirrorStrokeHorizontally(Stroke source)
        {
            var flipped = new Stroke();
            Vector startPoint = source[0];
            flipped.Add(startPoint);
            for (int i = 1; i < source.Count; i++)
            {
                flipped.Add(new Vector(flipped[i - 1].x - (source[i].x - source[i - 1].x), source[i].y, source[i].z));//Add the flipped displacement to the previous point stored
            }
            return flipped;
        }
    }
}