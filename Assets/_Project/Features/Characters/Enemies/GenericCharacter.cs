using BacklineVR.Characters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;

public class GenericCharacter : MonoBehaviour, ITargetable, IDestructible
{
    private protected CombatManager _combatManager;
    public GameObject GetGameObject() => this.gameObject;
    public Transform GetMainTransform() => this.transform;
    public virtual TargetType GetTargetType() => TargetType.None;
    public ITargetable Target { private set; get; }
    public virtual float MeleeReach => 1f;
    public float MeleeAttackDmg = 5f;
    public float RandomVarience = 2f;
    public float CritAttackDmg = 10f;
    public float MovemntSpeed = 2f;
    [SerializeField]
    private HealthBarUI _healthBarUI;
    [SerializeField]
    private StaggerBarUI _staggerBarUI;
    [SerializeField]
    private ParticleSystem _deathParticleEffect;
    private BehaviorTree _bt;
    private Animator _anim;
    public float MaxHealth = 100f;
    public float CurrentHealth;
    public float MaxStaggerGauge = 100f;
    public float CurrentStagger;
    public float StaggerLerpTimer;
    public float RecoverStaggerSpeed = 5f;
    private NavMeshAgent _nav;
    private Rigidbody _rb;
    private bool isMobile;
    private bool _isKilled;
    private bool _isStaggered;
    public bool IsDestroyed() => _isKilled;
    public bool IsStaggered() => _isStaggered;

    public virtual void Start()
    {
        _nav = GetComponent<NavMeshAgent>();
        _bt = GetComponent<BehaviorTree>();
        _rb = GetComponent<Rigidbody>();
        _combatManager = CombatManager.CombatManagerInstance;
        CurrentHealth = MaxHealth;
        CurrentStagger = 0f;
        _anim = GetComponent<Animator>();
        _healthBarUI.SetMaxHealth(MaxHealth);
        _staggerBarUI.SetMaxStagger(MaxStaggerGauge);
        OnInitialized();
    }
    public virtual void OnInitialized()
    {

    }
   

    void Update()
    {
        UpdateStagger();
        if (!isMobile)
            return;

        MoveToTarget();
    }
    public void TakeDamage(float damageAmount)
    {
        bool isCritical = _isStaggered;
        float CriticalDamage = isCritical ? CritAttackDmg : 0f;
        float DamageCalc = damageAmount + CriticalDamage;
        CurrentHealth -= DamageCalc;
        CurrentStagger += DamageCalc + 10;

        CheckHealth(isCritical);
        UpdateStagger();
    }
    public void TakeDamage(float damageAmount, bool forceCrit)
    {

        bool isCritical = forceCrit || _isStaggered;
        float CriticalDamage = isCritical ? CritAttackDmg : 0f;
        float DamageCalc = damageAmount + CriticalDamage;
        CurrentHealth -= DamageCalc;
        CurrentStagger += DamageCalc + 10;
        CheckHealth(isCritical);
        UpdateStagger();
    }
    private void CheckHealth(bool isCritical)
    {
        _healthBarUI.SetHealth(CurrentHealth, isCritical);
        if (CurrentHealth < 0)
            OnKilled();
    }
    private void UpdateStagger()
    {
        if (_isStaggered)
        {
            if (StaggerLerpTimer >= RecoverStaggerSpeed)
            {
                CurrentStagger = 0f;
                _staggerBarUI.SetStagger(CurrentStagger);
                SetStaggered(false);
                return;
            }
            StaggerLerpTimer += Time.deltaTime;
            float percentComplete = StaggerLerpTimer / RecoverStaggerSpeed;
            percentComplete = Mathf.Clamp(percentComplete, 0, 1);
            CurrentStagger = Mathf.Lerp(MaxStaggerGauge, 0, percentComplete);
            _staggerBarUI.SetStagger(CurrentStagger);
          
            return;
        }
        _staggerBarUI.SetStagger(CurrentStagger);
        if (CurrentStagger >= MaxStaggerGauge)
        {
            CurrentStagger = MaxStaggerGauge;
            SetStaggered(true);
        }

    }
    public virtual void OnKilled()
    {
        _isKilled = true;
        isMobile = false;
        _nav.enabled = false;
        _rb.isKinematic = true;
        CurrentStagger = 0f;
        _staggerBarUI.SetStagger(CurrentStagger);
        _anim.SetBool("IsKilled", true);
        _deathParticleEffect.Play();
        _bt.SendEvent("IsKilled");
    }
    public virtual void SetStaggered(bool staggered)
    {
        _isStaggered = staggered;
        _staggerBarUI.SetStaggeredCondition(staggered);
        StaggerLerpTimer = 0;
    }
    public void RecoverHealth(float healAmount)
    {
        CurrentHealth += healAmount;
        _healthBarUI.RecoverHealth(healAmount);
    }
    public void DamageTarget(float damangeAmount)
    {
        Target.GetGameObject().GetComponent<IDestructible>().TakeDamage(damangeAmount);
    }
    public void HealTarget(float healAmount)
    {
        Target.GetGameObject().GetComponent<IDestructible>().RecoverHealth(healAmount);
    }
    public float GetMeleeAttackDamage()
    {
       return Random.Range(MeleeAttackDmg - RandomVarience, MeleeAttackDmg + RandomVarience);
    }
    public void SetTarget(ITargetable newTarget)
    {
        Target = newTarget;
    }
    public float DistanceToTarget()
    {
        return CombatManager.GetSquaredDistance(GetMainTransform().position, Target.GetMainTransform().position);
    }
    public void StartMovement()
    {
        isMobile = true;
    }

    private void MoveToTarget()
    {
        _nav.SetDestination(Target.GetMainTransform().position);
        _nav.speed = MovemntSpeed;
        _nav.stoppingDistance = MeleeReach;
        if (DistanceToTarget() < MeleeReach)
        {
            OnReachedDestination();
        }
    }
    private void OnReachedDestination()
    {
        _nav.stoppingDistance = MeleeReach;
        isMobile = false;
    }
}
