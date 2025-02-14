using BacklineVR.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTargetingSystem : TargetingSystem
{
    private protected override void CastSpell()
    {
        StartCoroutine(DelayedCastSpell());
    }
    private IEnumerator DelayedCastSpell()
    {
        foreach (var glyph in _targetingGlyphs)
        {
            var spell = Instantiate(_spellPrefab, glyph.transform.position, glyph.transform.rotation);
            yield return new WaitForSeconds(.25f);
        }
        Deactivate();
    }

    private protected override void OnStartSelect()
    {
        _activeTargetingGlyph = GetGlyph();

        //Allow grabbing a already placed glyph and setting active targeting glyph to that first?
    }

    private protected override void OnStopSelect()
    {
            print("TEMP: Stop Selection!");
    }

    private protected override void UpdateTargeting()
    {
        var angle = Quaternion.LookRotation(-_castingHand.up, Vector3.up);
        _activeTargetingGlyph.transform.SetPositionAndRotation(_castingHand.position, angle);
        //do a raycast from the hand, with a line renderer laser to the selected target being emitted from the hand position
    }
    private protected override void Deselect()
    {
        //Pop the previous selection from the stack of targets. On select add it if applicable
        base.Deselect();
    }
}
