using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField]
    private float currHealth;
    public float maxHealth = 100f;
    private float lerpTimer;
    public float lerpSpeed = 100f;
    [SerializeField]
    private Image frontHealthBar;
    [SerializeField]
    private Image backHealthBar;
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currHealth = maxHealth;
    }
    public void TakeDamage(float damage)
    {
        currHealth -= damage;
        lerpTimer = 0f;
    }
    [ContextMenu("AddHealth")]
    public void RecoverHealthTest()
    {
        RecoverHealth(30f);
    }
    [ContextMenu("RemoveHealth")]
    public void RemoveHealthTest()
    {
        TakeDamage(30f);
    }
    public void RecoverHealth(float heal)
    {
        currHealth += heal;
        lerpTimer = 0f;
    }
    void Update()
    {
        currHealth = Mathf.Clamp(currHealth, 0, maxHealth);
        UpdateHealthUI();
    }
    public void UpdateHealthUI()
    {
        float fillMain = frontHealthBar.fillAmount;
        float fillBack = backHealthBar.fillAmount;
        float healthPercent = currHealth / maxHealth;
        if(fillBack > healthPercent)
        {
            frontHealthBar.fillAmount = healthPercent;
            backHealthBar.color = Color.red;
            lerpTimer += Time.deltaTime;
            float percentComplete = lerpTimer / lerpSpeed;
            percentComplete = Mathf.Clamp(percentComplete, 0, 1);
            percentComplete = Mathf.Sqrt(2 * percentComplete - (percentComplete * percentComplete));
            backHealthBar.fillAmount = Mathf.Lerp(fillBack, healthPercent, percentComplete);
        }
        if (fillMain < healthPercent)
        { 
            backHealthBar.color = Color.green;
            backHealthBar.fillAmount = healthPercent;
            lerpTimer += Time.deltaTime;
            float percentComplete = lerpTimer / lerpSpeed;
            percentComplete = Mathf.Clamp(percentComplete, 0, 1);
            percentComplete = Mathf.Sqrt(2 * percentComplete - (percentComplete * percentComplete));
            frontHealthBar.fillAmount = Mathf.Lerp(fillMain, backHealthBar.fillAmount, percentComplete);
        }
    }
}
