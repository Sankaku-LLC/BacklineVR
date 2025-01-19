using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaggerBarUI : MonoBehaviour
{
    [SerializeField]
    private float currStagger;
    public float maxStagger = 100f;
    private float lerpTimer;
    public float lerpSpeed = 2f;
    [SerializeField]
    private Image frontStaggerBar;
    [SerializeField]
    private Image backStaggerBar;
    void Start()
    {
        currStagger = 0f;
    }

    void Update()
    {
        currStagger = Mathf.Clamp(currStagger, 0, maxStagger);
        UpdateStaggerUI();
    }
    public void UpdateStaggerUI()
    {
        float fillMain = frontStaggerBar.fillAmount;
        float fillBack = backStaggerBar.fillAmount;
        float StaggerPercent = currStagger / maxStagger;
        if (fillBack > StaggerPercent)
        {
            frontStaggerBar.fillAmount = StaggerPercent;
            lerpTimer += Time.deltaTime;
            float percentComplete = lerpTimer / lerpSpeed;
            percentComplete = Mathf.Clamp(percentComplete, 0, 1);
            percentComplete = Mathf.Sqrt(2 * percentComplete - (percentComplete * percentComplete));
            backStaggerBar.fillAmount = Mathf.Lerp(fillBack, StaggerPercent, percentComplete);
        }
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
    [ContextMenu("TakeStagger")]
    public void TakeStaggerTest()
    {
        TakeStagger(10f);
    }
    public void TakeStagger(float staggerAmount)
    {
        currStagger += staggerAmount;
        lerpTimer = 0f;
    }
}
