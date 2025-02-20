using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AvatarCalibrationManager : MonoBehaviour
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
    public GameObject instructions, calibrationMenu, background;

    public TextMeshProUGUI instructionsTMP;

    public Transform[] options;
    public Sprite checkIcon, nextIcon;
    public Image prevMarker, nextMarker;

    //Interactive elements
    public Button prevButton, nextButton;
    //User can press either symbol or index to go to index, and both are unlocked/locked as appropriate
    public Button[] symbols;
    public Button[] actions;

    public int availability;

    /// <summary>
    /// Implement colors for disable/enable/ active indication, and controlling if you can move on
    /// </summary>
    public bool currentStepComplete;
    public int targetIdx;
    /// <summary>
    /// How many index units should the scroller traverse per second?
    /// </summary>
    public float scrollAnimationRate = 0.5f;
    float curIdx;

    // Start is called before the first frame update
    void Start()
    {
        //hardcoded because it starts at the center always
        targetIdx = 0;
        curIdx = options.Length / 2;
        availability = -1;
        Scroll();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentStepComplete)
        {
            currentStepComplete = false;
            nextButton.interactable = true;
        }
    }


    public void MoveNext()
    {
        if (curIdx == options.Length - 1)
        {
            //Execute saving code
            print("Execute saving code");
            OnSave();
        }
        else
        {
            //Target idx is currently the integer index I'm moving from, availability is the last completed and saved step
            if (targetIdx > availability)
            {
                //If the target index is beyond my availability when the next button is pressed, disable the next button and make my current step available
                availability = targetIdx;
                nextButton.interactable = false;
                //Because I'm moving onto not yet available territory, enable the shortcut buttons to that territory now that it's unlocked
                symbols[targetIdx + 1].interactable = true;
                actions[targetIdx + 1].interactable = true;
            }

            SetIdx(curIdx + 1);

        }
    }
    public void MovePrevious()
    {
        SetIdx(curIdx - 1);
        nextButton.interactable = true;
    }
    public void SetIdx(float idx)
    {
        if (idx != targetIdx)
        {
        }

        targetIdx = Mathf.Clamp(Mathf.RoundToInt(idx), 0, options.Length - 1);
        if (targetIdx - 1 == availability)
        {
            //If the target index is equal to my current availability (so if I go forwards to a new step and then go back) when the next button is pressed, disable it for the coming one because that's the border
            nextButton.interactable = false;
        }
        else if (targetIdx - 1 < availability)
        {
            //If the target index is less than my current availability, then I can move around as I want because it's already been made available
            nextButton.interactable = true;
        }

    }
    public void OnStart()
    {
        calibrationMenu.SetActive(true);
        instructions.SetActive(false);
        background.SetActive(false);

        nextMarker.enabled = true;

        StartCoroutine(AnimatedScrollController());


    }
    public void OnCancel()
    {
        targetIdx = 0;
        curIdx = options.Length / 2;
        availability = -1;
        Scroll();
        for (int i = 1; i < symbols.Length; i++)
        {
            symbols[i].interactable = false;
            actions[i].interactable = false;
        }
        StopAllCoroutines();
        calibrationMenu.SetActive(false);
        instructions.SetActive(true);
        background.SetActive(true);
        nextMarker.sprite = nextIcon;
    }
    void OnSave()
    {
        //This will set current used version to the internal avatar configuration of the module
        nextButton.interactable = false;
        nextMarker.enabled = false;
        nextMarker.sprite = nextIcon;
    }

    internal void OnComplete()
    {
        //Re-load body with saved copy
        OnCancel();
    }

    IEnumerator AnimatedScrollController()
    {
        float delta = 0;

    CHECK:
        while (true)
        {
            if (curIdx != targetIdx)
            {
                break;
            }
            yield return null;
        }

        //Disable or switch markers as applicable
        if (targetIdx == 0)
        {
            prevMarker.enabled = false;
        }


        //Would only get here if curIdx != targetIdx, and since past the bounds this change isn't allowed through input validation, it's guaranteed to prompt movement in the correct way
        if (curIdx == 0)
        {
            prevMarker.enabled = true;
        }
        else if (curIdx == options.Length - 1)
        {
            nextMarker.sprite = nextIcon;
        }


        while (true)
        {
            delta = scrollAnimationRate * Time.deltaTime;
            //If they are closer than can be covered in a jump
            if (Mathf.Abs(targetIdx - curIdx) < delta)
            {
                curIdx = targetIdx;
                Scroll();
                break;
            }
            else
            {
                curIdx += Mathf.Sign(targetIdx - curIdx) * delta;
                Scroll();
            }
            yield return null;
        }
        //Icon set to the check after it transitions because it looks more natural to have it that way
        if (curIdx == options.Length - 1)
        {
            nextMarker.sprite = checkIcon;
        }

        goto CHECK;
    }
    void Scroll()
    {
        for (int i = 0; i < options.Length; i++)
        {
            float t = i - curIdx;
            int sign = t > 0 ? 1 : -1;
            float t_abs = sign * t;
            float scale = sFunction(t_abs);
            float zPos = zFunction(t_abs);
            float xPos = xFunction(t_abs);
            options[i].localPosition = new Vector3(sign * xPos, 0, zPos);
            options[i].localScale = Vector3.one * scale;
        }
    }

    float sFunction(float t)
    {
        return .025f * ((t - 7) * t + 20);
    }
    float zFunction(float t)
    {
        return .025f * (t + 1) * t;
    }
    float xFunction(float t)
    {
        float x_abs = 0.7f + (0.35f - 0.025f * t) * (t - 1);
        if (t < 1)
        {
            x_abs -= 0.35f * Mathf.Pow(1 - t, 2);
        }
        return x_abs;
    }

}
