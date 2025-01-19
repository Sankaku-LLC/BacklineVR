using BacklineVR.Characters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GenericCharacter : MonoBehaviour, ITargetable, IDestructible
{
    private protected CombatManager _combatManager;
    public GameObject GetGameObject() => this.gameObject;
    public Transform GetMainTransform() => this.transform;
    public virtual TargetType GetTargetType() => TargetType.None;
    public ITargetable Target { private set; get; }
    public virtual float MeleeReach => 2f;
    public float MeleeAttackDmg = 5f;
    public float RandomVarience = 2f;
    public virtual float MovemntSpeed => 2f;
    [SerializeField]
    private HealthBarUI _healthBarUI;
    public float MaxHealth = 100f;
    public float CurrentHealth;
    private NavMeshAgent _nav;
    private bool isMobile;

    public virtual void Start()
    {
        _nav = GetComponent<NavMeshAgent>();
        _combatManager = CombatManager.CombatManagerInstance;
        CurrentHealth = MaxHealth;
        OnInitialized();
    }
    public virtual void OnInitialized()
    {

    }
   

    void Update()
    {
        if (!isMobile)
            return;

        MoveToTarget();
    }
    public void TakeDamage(float damageAmount)
    {
        CurrentHealth -= damageAmount;
        _healthBarUI.TakeDamage(damageAmount);
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
