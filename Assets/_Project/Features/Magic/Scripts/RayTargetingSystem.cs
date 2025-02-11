using BacklineVR.Items;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTargetingSystem : TargetingSystem
{
    private protected override void OnApplySelections(Spell spell)
    {
        print("TEMP: Cast Magic!");
        var rays = new List<Ray>(_targetingGlyphs.Count);
        foreach(var glyph in _targetingGlyphs)
        {
            rays.Add(new Ray(glyph.transform.position, glyph.transform.forward));
        }
        //spell.Activate(rays);
    }

    private protected override void OnStartSelect()
    {
            print("TEMP: Set Target!");
        //Allow grabbing a already placed glyph and setting active targeting glyph to that first?
    }

    private protected override void OnStopSelect()
    {
            print("TEMP: Stop Selection!");
    }

    private protected override void UpdateTargeting()
    {
        _activeTargetingGlyph.transform.SetPositionAndRotation(_castingHand.position, _castingHand.rotation);
        //do a raycast from the hand, with a line renderer laser to the selected target being emitted from the hand position
    }
    private protected override void Deselect()
    {
        //Pop the previous selection from the stack of targets. On select add it if applicable
        base.Deselect();
    }
}
