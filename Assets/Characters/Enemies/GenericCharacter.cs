using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GenericCharacter : MonoBehaviour, ITargetable
{
    private protected CombatManager _combatManager;
    public GameObject GetGameObject() => this.gameObject;
    public Transform GetMainTransform() => this.transform;
    public virtual TargetType GetTargetType() => TargetType.None;
    public ITargetable Target { private set; get; }
    public virtual float MeleeReach => 2f;
    public virtual float MovemntSpeed => 2f;
    private NavMeshAgent _nav;
    private bool isMobile;
    public virtual void Start()
    {
        _nav = GetComponent<NavMeshAgent>();
        _combatManager = CombatManager.CombatManagerInstance;
        OnInitialized();
    }
    public virtual void OnInitialized()
    {

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

    void Update()
    {
        if (!isMobile)
            return;

        MoveToTarget();
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
