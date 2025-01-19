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
    private NavMeshAgent _nav;
    private CharacterController _characterController;
    private Rigidbody _rb;
    private bool isMobile;
    private bool _isKilled;
    public bool IsDestroyed() => _isKilled;

    public virtual void Start()
    {
        _nav = GetComponent<NavMeshAgent>();
        _bt = GetComponent<BehaviorTree>();
        _rb = GetComponent<Rigidbody>();
        _characterController = GetComponent<CharacterController>();
        _combatManager = CombatManager.CombatManagerInstance;
        CurrentHealth = MaxHealth;
        _anim = GetComponent<Animator>();
        _healthBarUI.SetMaxHealth(MaxHealth);
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
        _staggerBarUI.TakeStagger(damageAmount+10);
        if (CurrentHealth < 0)
            OnKilled();        
        
    }
    public virtual void OnKilled()
    {
        _isKilled = true;
        isMobile = false;
        _nav.enabled = false;
        _rb.isKinematic = true;
        _characterController.enabled = false;
        _anim.SetBool("IsKilled", true);
        _deathParticleEffect.Play();
        _bt.SendEvent("IsKilled");
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
