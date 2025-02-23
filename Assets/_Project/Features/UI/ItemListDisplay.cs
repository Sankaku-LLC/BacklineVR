using System.Collections;
using System.Collections.Generic;
using BacklineVR.Core;
using CurseVR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemListDisplay : MonoBehaviour
{

    /// <summary>
    /// Calibration UI code:
    /// Scrolling system parameter controllers:
    /// Let t = |idx-2|, where idx is 0-4 of which step in calibration
    /// s_(t+1) = s_t - .15 + .05t, s_0 = .5
    /// The closed-form solution for scale bcomes .025 * ((t-7) * t + 20)
    /// z_(t+1) = s_t + .05t, s_0 = 0
    /// The closed-form solution for z becomes .025 * (t+1) * t
    /// x_(t+1) = x_t + .3 - .05t, x_0 = .7  [NOTE: This is valid from the first index and onwards, this is actually a piecewise function]
    /// The closed-form solution for x, from 1 to infinity  (x(t) becomes .7 + (0.7 - .05t)/2 * (t-1)
    /// The piecewise function x'(t) is: x(t) - 0.35 * (1-x)^p 0->1, x(t) otherwise,  for some high p that's still cheap, from 0 to 1. Determine from testing.
    /// From 0 to 1:  
    /// </summary>

    [SerializeField]
    private bool _moveNext;

    [SerializeField]
    private bool _moveBack;

    [SerializeField]
    private GameObject _listParent;
    [SerializeField]
    private GameObject _entryPrefab;


    /// <summary>
    /// Implement colors for disable/enable/ active indication, and controlling if you can move on
    /// </summary>
    private int _targetIdx;
    /// <summary>
    /// How many index units should the scroller traverse per second?
    /// </summary>
    [SerializeField]
    private float _scrollAnimationRate = 0.5f;
    [SerializeField]
    private float _scale = 1;
    private float _curIdx;

    [SerializeField]
    private int _windowSize = 3;

    [SerializeField]
    private bool _isHorizontal = true;

    private Coroutine _animatedScroll;
    private readonly List<GameObject> _entries = new List<GameObject>(64);
    private readonly List<Transform> _options = new List<Transform>(64);

    public bool CanMoveNext { get; private set; }
    public bool CanMoveBack { get; private set; }

    public void Show(List<ItemFrequencyPair<string>> entries)
    {
        foreach (var entry in entries)
        {
            var spawnedUIGroup = Instantiate(_entryPrefab, _listParent.transform).GetComponent<UIGroup>();
            spawnedUIGroup.Initialize();
            spawnedUIGroup.Set("ItemName", entry.Item);
            spawnedUIGroup.Set("ItemCount", entry.Frequency + "");
            spawnedUIGroup.gameObject.name = entry.Item;
            _entries.Add(spawnedUIGroup.gameObject);
            _options.Add(spawnedUIGroup.transform);
        }
        CanMoveNext = true;
        CanMoveBack = true;
        //hardcoded because it starts at the center always
        _curIdx = _options.Count / 2f;

        InitializeBounds();
        SetIdx(Mathf.RoundToInt(_curIdx));
        Scroll();
    }
    private void InitializeBounds()
    {
        for (int i = 0; i < _options.Count; i++)
        {
            float t = Mathf.Abs(i - _curIdx);
            if (t > _windowSize)
            {
                _options[i].gameObject.SetActive(false);
                continue;
            }
            else
            {
                _options[i].gameObject.SetActive(true);
            }
        }
    }
    // Update is called once per frame
    private void Update()
    {
        if (_moveNext)
        {
            _moveNext = false;
            if (CanMoveNext)
                MoveNext();
        }
        if (_moveBack)
        {
            _moveBack = false;
            if (CanMoveBack)
                MoveBack();
        }
    }


    public void MoveNext()
    {
        SetIdx(_targetIdx + 1);
    }
    public void MoveBack()
    {
        SetIdx(_targetIdx - 1);
    }
    public void SetIdx(float idx)
    {
        if (idx == _targetIdx)
        {
            return;
        }
        _targetIdx = Mathf.Clamp(Mathf.RoundToInt(idx), 0, _options.Count - 1);

        CanMoveNext = !(_targetIdx == _options.Count - 1);
        CanMoveBack = !(_targetIdx == 0);

        if (_animatedScroll == null)
        {
            _animatedScroll = StartCoroutine(DoAnimatedScroll());
        }
    }
    private IEnumerator DoAnimatedScroll()
    {
        var delta = _scrollAnimationRate * Time.deltaTime;
        var diff = _targetIdx - _curIdx;
        var startIdx = _curIdx;
        //while they are further than can be covered in a jump
        while (Mathf.Abs(diff) > delta)
        {
            delta = _scrollAnimationRate * Time.deltaTime;
            _curIdx += Mathf.Sign(diff) * delta;
            diff = _targetIdx - _curIdx;
            if (Mathf.Abs(_curIdx - startIdx) > 1)
            {
                //Stepped forward, enable/ disable around window
                startIdx = _curIdx;
            }
            Scroll();
            yield return null;
        }
        _curIdx = _targetIdx;
        Scroll();
        _animatedScroll = null;
    }


    private void Scroll()
    {
        var isAdding = _curIdx < _targetIdx;
        var lowerBound = (int)Mathf.Max(0, _curIdx - _windowSize + 1);
        var upperBound = (int)Mathf.Min(_options.Count - 1, _curIdx + _windowSize);
        for (int i = lowerBound; i <= upperBound; i++)
        {
            float t = i - _curIdx;
            int sign = t > 0 ? 1 : -1;
            float absT = sign * t;
            float scale = sFunction(absT);
            float zPos = zFunction(absT);
            float xPos = xFunction(absT);
            //if (absT > _windowSize)//if outside bounds
            //{
            //    _options[i].gameObject.SetActive(false);
            //    continue;
            //}
            //else if(absT == _windowSize - 1)//if within bounds
            //{
            //    _options[i].gameObject.SetActive(true);
            //}
            _options[i].localPosition = _scale * (_isHorizontal ? new Vector3(sign * xPos, 0, zPos) : new Vector3(0, sign * xPos, zPos));
            _options[i].localScale = Vector3.one * scale * _scale;
        }
        if (lowerBound > 0)
        {
            _options[lowerBound - 1].gameObject.SetActive(!isAdding);
        }
        if (upperBound < _options.Count - 1)
        {
            _options[upperBound + 1].gameObject.SetActive(isAdding);
        }
    }

    private float sFunction(float t)
    {
        return .025f * ((t - 7) * t + 20);
    }
    private float zFunction(float t)
    {
        return .025f * (t + 1) * t;
    }
    private float xFunction(float t)
    {
        float x_abs = 0.7f + (0.35f - 0.025f * t) * (t - 1);
        if (t < 1)
        {
            x_abs -= 0.35f * Mathf.Pow(1 - t, 2);
        }
        return x_abs;
    }

}
