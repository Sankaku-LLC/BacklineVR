using BacklineVR.Core;
using BacklineVR.Items;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    private protected int _maxTargets;
    private protected Transform _castingHand;
    [SerializeField]
    private protected GameObject _castingGlyph;
    [SerializeField]
    private protected GameObject _targetingGlyphTemplate;

    private protected GameObject _activeTargetingGlyph;
    // Start is called before the first frame update
    private void Awake()
    {
        Initialize();
        _targetingGlyphTemplate.SetActive(false);
    }
    private protected virtual void Initialize()
    {
        _selectActive = false;
    }
    private void Start()
    {
        Setup();
    }
    private protected virtual void Setup()
    {

    }
    public void Activate(Transform castingHand, int targetCount)
    {
        print("Weapon armed!");
        _castingHand = castingHand;
        _targetIndex = -1;
        _maxTargets = targetCount;
        var headTransform = Player.Instance.Head.transform;
        var castingPos = headTransform.position;
        castingPos.y = 0;
        var castingRot = Quaternion.Euler(0, headTransform.eulerAngles.y, 0);
        _castingGlyph.transform.SetPositionAndRotation(castingPos, castingRot);
        _castingGlyph.SetActive(true);
    }
    public void Deactivate()
    {
        print("Weapon disarmed!");
        _castingGlyph.SetActive(false);
    }
    public void StartSelect()
    {
        _selectActive = true;
        _activeTargetingGlyph = GetGlyph();
        OnStartSelect();
    }
    private protected abstract void OnStartSelect();
    public void StopSelect()
    {
        _selectActive = false;
        OnStopSelect();
    }
    private protected abstract void OnStopSelect();
    public void ApplySelections(Spell spell)
    {
        StopSelect();
        OnApplySelections(spell);
    }
    private protected virtual void Deselect()
    {
        _activeTargetingGlyph.SetActive(false);
        _activeTargetingGlyph = null;
        _targetIndex = Mathf.Max(0, _targetIndex - 1);
    }
    private protected abstract void OnApplySelections(Spell spell);

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
        if( _targetIndex < _targetingGlyphs.Count && _targetIndex < _maxTargets)
        {
            var newGlyph = Instantiate(_targetingGlyphTemplate);
            _targetingGlyphs.Add(newGlyph);
            _targetIndex++;
            return newGlyph;
        }
        //If already spawned, use next
        _targetIndex = (_targetIndex + 1) % _maxTargets;
        var nextGlyph = _targetingGlyphs[_targetIndex];
        nextGlyph.SetActive(true);
        return nextGlyph;
    }
}
