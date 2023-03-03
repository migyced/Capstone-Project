using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerAbility : MonoBehaviour
{
    public virtual float baseCooldown
    {
        get { return 1f;}
    }
    [HideInInspector] public float cooldownTimer = 0f;
    public abstract void Activate();
    public void UpdateCooldown(float time)
    {
        cooldownTimer -= time;
        cooldownTimer = Mathf.Max(0f, cooldownTimer);
    }
    public bool IsReady() 
    {
        return (cooldownTimer <= 0f);
    }

    public abstract void updateMe(float time);
    public abstract void Initialize(playerController player, Animator animator);
}
