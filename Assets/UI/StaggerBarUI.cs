using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaggerBarUI : MonoBehaviour
{
    private const float TIME_SINCE_LAST_HIT = 3f;
    [SerializeField]
    private float currStagger;
    private float maxStagger;
    private float lerpTimer;
    private float steadyTimer;
    public float lerpSpeed = 2f;
    public float healStaggerOverTime = 5f;
    public bool IsStaggered;

    [SerializeField]
    private Image frontStaggerBar;
    [SerializeField]
    private Image backStaggerBar;
    void Update()
    {
        if (!IsStaggered) { 
            steadyTimer += Time.deltaTime;
            if (steadyTimer > TIME_SINCE_LAST_HIT)
            {
                currStagger -= healStaggerOverTime;
                steadyTimer = 0f;
            }
        }
        currStagger = Mathf.Clamp(currStagger, 0, maxStagger);
        UpdateStaggerUI();
    }
    public void UpdateStaggerUI()
    {
        float fillMain = frontStaggerBar.fillAmount;
        float fillBack = backStaggerBar.fillAmount;
        float StaggerPercent = currStagger / maxStagger;
        if (IsStaggered)
        {
            frontStaggerBar.fillAmount = StaggerPercent;
            return;
        }
        //taking stagger damage
        if (fillBack > StaggerPercent)
        {
            frontStaggerBar.fillAmount = StaggerPercent;
            lerpTimer += Time.deltaTime;
            float percentComplete = lerpTimer / lerpSpeed;
            percentComplete = Mathf.Clamp(percentComplete, 0, 1);
            percentComplete = Mathf.Sqrt(2 * percentComplete - (percentComplete * percentComplete));
            backStaggerBar.fillAmount = Mathf.Lerp(fillBack, StaggerPercent, percentComplete);
        }
        
        //recovering Stagger
        if (fillMain < StaggerPercent)
        {
            backStaggerBar.fillAmount = StaggerPercent;
            lerpTimer += Time.deltaTime;
            float percentComplete = lerpTimer / lerpSpeed;
            percentComplete = Mathf.Clamp(percentComplete, 0, 1);
            percentComplete = Mathf.Sqrt(2 * percentComplete - (percentComplete * percentComplete));
            frontStaggerBar.fillAmount = Mathf.Lerp(fillMain, backStaggerBar.fillAmount, percentComplete);
        }

       
    }
    public void SetMaxStagger(float newMaxStagger)
    {
        maxStagger = newMaxStagger;
        currStagger = 0f;
    }
    public void SetStaggeredCondition(bool staggered)
    {
        IsStaggered = staggered;
        frontStaggerBar.color = staggered ? Color.yellow : Color.white ;
    }
    [ContextMenu("TakeStagger")]
    public void TakeStaggerTest()
    {
        SetStagger(10f);
    }
    public void SetStagger(float staggerAmount)
    {
        lerpTimer = 0f;
        steadyTimer = 0f;
        currStagger = staggerAmount;
    }

}
