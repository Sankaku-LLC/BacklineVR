using BacklineVR.Core;
using BacklineVR.Items;
using CurseVR.Director;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This will select targets and spawn glyphs in their hierarchy
/// It will check the forward direction from the casting hand, compare its angle to the nearest target and if within some acceptable angle select them
/// It will choose the one it most aligns with
/// No autoselect
/// </summary>
public class EntityTargetingSystem : TargetingSystem
{
    private const float CONE_SELECTION_THRESHOLD_THETA = 30;//10 degrees within hand forward to select
    private const float SKIP_SELECTION_CHECK_THRESHOLD = 2;//If within 2 degrees of the previous selection don't bother updating
    [SerializeField]
    private bool _targetAllies;
    private List<ITargetable> _targetPool;
    private CombatManager _combatManager;
    private Transform _currentTarget;
    private protected override void Initialize()
    {
        _combatManager = GlobalDirector.Get<CombatManager>();
    }
    private protected override void Setup()
    {
        _targetPool = _targetAllies ? _combatManager.AllSpawnedAllies : _combatManager.AllSpawnedEnemies;
    }
    private protected override void CastSpell()
    {
        StartCoroutine(DelayedCastSpell());
    }
    private IEnumerator DelayedCastSpell()
    {
        foreach (var glyph in _targetingGlyphs)
        {
            if (!glyph.activeSelf)
                continue;
            var spell = Instantiate(_spellPrefab, glyph.transform.position, Quaternion.identity);
            yield return new WaitForSeconds(.25f);
        }
        Deactivate();
    }

    private protected override void OnStartSelect()
    {
        //Trigger begin highlighting
        _currentTarget = null;
    }

    private protected override void OnStopSelect()
    {
        //Place on current target, move on
    }

    private protected override void UpdateTargeting()
    {
        var closestAngle = CONE_SELECTION_THRESHOLD_THETA;
        if (_currentTarget != null)
        {
            closestAngle = Vector3.Angle(-_castingHand.up, _currentTarget.position - _castingHand.position);
            if (closestAngle < SKIP_SELECTION_CHECK_THRESHOLD)
                return;
        }
        ITargetable closestTarget = null;
        foreach (var target in _targetPool)
        {
            var targetTransform = target.GetMainTransform();
            var vectorToTarget = targetTransform.position - _castingHand.position;
            var angle = Vector3.Angle(-_castingHand.up, vectorToTarget);
            if (angle < closestAngle)
            {
                closestAngle = angle;
                closestTarget = target;
            }
        }
        if (closestTarget == null)
            return;
        _activeTargetingGlyph = GetGlyph();

        if (closestTarget is GenericCharacter npc)
        {
            var glyphTransform = _activeTargetingGlyph.transform;
            glyphTransform.parent = npc.transform;
            Util.ResetTransform(glyphTransform, false);
        }
        _currentTarget = closestTarget.GetMainTransform();
    }

    private protected override void Deselect()
    {
    }
}
