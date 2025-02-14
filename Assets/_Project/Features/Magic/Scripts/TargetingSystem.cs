using BacklineVR.Core;
using BacklineVR.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// These are used to apply spells onto targets when casting with a magic item
/// </summary>
public abstract class TargetingSystem : MonoBehaviour
{
    private protected const int MAX_TARGET_COUNT = 32;
    private protected List<GameObject> _targetingGlyphs = new List<GameObject>(MAX_TARGET_COUNT);

    private protected bool _selectActive;

    private protected int _targetIndex;
    [SerializeField]
    private protected int _maxTargets;
    private protected Transform _castingHand;
    [SerializeField]
    private protected GameObject _castingGlyph;
    [SerializeField]
    private protected GameObject _targetingGlyphTemplate;
    [SerializeField]
    private protected GameObject _spellPrefab;

    private protected GameObject _activeTargetingGlyph;
    // Start is called before the first frame update
    private void Awake()
    {
        Initialize();
        _selectActive = false;
        _targetingGlyphTemplate.SetActive(false);
    }
    private protected virtual void Initialize()
    {
    }
    private void Start()
    {
        Setup();
    }
    private protected virtual void Setup()
    {

    }
    public void Activate(Transform castingHand)
    {
        _castingHand = castingHand;
        _targetIndex = -1;
        var headTransform = Player.Instance.Head.transform;
        var castingPos = headTransform.position;
        castingPos.y = 0;
        var castingRot = Quaternion.Euler(90, headTransform.eulerAngles.y, 0);
        _castingGlyph.transform.parent = null;
        _castingGlyph.transform.SetPositionAndRotation(castingPos, castingRot);
        _castingGlyph.SetActive(true);
    }
    public void Deactivate()
    {
        _castingGlyph.SetActive(false);
        for (var i = _targetingGlyphs.Count - 1; i >= 0; i--)
            Destroy(_targetingGlyphs[i]);
        _targetingGlyphs.Clear();
        _targetIndex = -1;
    }
    public void StartSelect()
    {
        _selectActive = true;
        OnStartSelect();
    }
    private protected abstract void OnStartSelect();
    public void StopSelect()
    {
        _selectActive = false;
        OnStopSelect();
    }
    private protected abstract void OnStopSelect();
    public void ApplySelections()
    {
        StopSelect();
        CastSpell();
    }
    private protected virtual void Deselect()
    {
        _activeTargetingGlyph.SetActive(false);
        _activeTargetingGlyph = null;
        _targetIndex = Mathf.Max(0, _targetIndex - 1);
    }
    private protected abstract void CastSpell();

    private void FixedUpdate()
    {
        if (!_selectActive)
            return;
        UpdateTargeting();
    }
    private protected abstract void UpdateTargeting();
    private protected GameObject GetGlyph()
    {
        //If we need to spawn another and have the capacity for it
        if(_targetingGlyphs.Count < _maxTargets && _targetIndex + 1 < _maxTargets)
        {
            var newGlyph = Instantiate(_targetingGlyphTemplate);
            _targetingGlyphs.Add(newGlyph);
            _targetIndex++;
            newGlyph.SetActive(true);
            return newGlyph;
        }
        //If already spawned, use next
        _targetIndex = (_targetIndex + 1) % _maxTargets;
        var nextGlyph = _targetingGlyphs[_targetIndex];
        nextGlyph.SetActive(true);
        return nextGlyph;
    }
}
