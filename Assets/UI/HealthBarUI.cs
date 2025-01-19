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
    public float lerpSpeed = 2f;
    [SerializeField]
    private Image frontHealthBar;
    [SerializeField]
    private Image backHealthBar;

    void Start()
    {
        currHealth = maxHealth;   
    }
    [ContextMenu("TakeDamage")]
    private void TakeDamage()
    {
        TakeDamage(UnityEngine.Random.Range(5, 12));
    }
    [ContextMenu("HealDamage")]
    private void HealthDamage()
    {
        HealDamage(UnityEngine.Random.Range(5, 12));
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
            percentComplete = percentComplete * percentComplete;
            backHealthBar.fillAmount = Mathf.Lerp(fillBack, healthPercent, percentComplete);
        }
        if (fillMain < healthPercent)
        { 
            backHealthBar.color = Color.green;
            backHealthBar.fillAmount = healthPercent;
            lerpTimer += Time.deltaTime;
            float percentComplete = lerpTimer / lerpSpeed;
            percentComplete = percentComplete * percentComplete;
            frontHealthBar.fillAmount = Mathf.Lerp(fillMain, backHealthBar.fillAmount, percentComplete);
        }
    }
    public void TakeDamage(float damage)
    {
        currHealth -= damage;
        lerpTimer = 0f;
    }
    public void HealDamage(float heal)
    {
        currHealth += heal;
        lerpTimer = 0f;
    }
}
