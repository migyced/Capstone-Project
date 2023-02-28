using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("stats")]
    [SerializeField]public string enemyName;
    [SerializeField]public double maxHealth;
    [SerializeField]public double health;
    [SerializeField]public float basicAttackDamage;
    [SerializeField]public float basicAttackSpeed;
    [SerializeField]public float attackRange;
    [SerializeField]public float movementSpeed;
    [SerializeField]public float movementRange;
    [SerializeField]public float idleMovementRange;
    [SerializeField]public float staggerLimit;
    public List<Ability> abilities; 

    [Header("Collider + Physics info")]
    public MeshCollider enemyCollider;
    public Rigidbody enemyBody;
    public Rigidbody playerBody;
    public Transform player;

    //public enemyMovement movement;
    //public enemyAttacks attacks;

    [Header("Animator info")]
    public Animator animator;
    public AnimatorStateInfo stateInfo;
    public bool animationFinished = true;

    [Header("Dust Piles info")]
    public float detectionRange = 5.0f;
    public float healingSpeed = 0.1f;
    public GameObject dustPilePrefab;
    public int maxDustPiles = 5;
    private List<DustPile> dustPiles = new List<DustPile>();

    void Start(){
        enemyBody = GetComponent<Rigidbody>();
        playerBody = player.GetComponent<Rigidbody>();
        animator = gameObject.GetComponentInChildren<Animator>();
        health = maxHealth;
    }

    void Update(){
        // Check for nearby dust piles that need healing
        foreach (DustPile dustPile in dustPiles) {
            if (dustPile.health < dustPile.maxHealth) {
                float distance = Vector3.Distance(transform.position, dustPile.transform.position);
                if (distance <= detectionRange) {
                    dustPile.IncreaseHealth(healingSpeed * Time.deltaTime);
                }
            }
        }

        // Check if we need to generate a new dust pile
        if (dustPiles.Count < maxDustPiles) {
            GameObject newDustPile = Instantiate(dustPilePrefab, transform.position + new Vector3(Random.Range(-1.0f, 1.0f), 0, Random.Range(-1.0f, 1.0f)), Quaternion.identity);
            DustPile newDustPileScript = newDustPile.GetComponent<DustPile>();
            newDustPileScript.SetHealth(0.1f); // set a low starting health
            dustPiles.Add(newDustPileScript);
        }
    }

    public void isHit(float damage){
        health -= damage;
        if(health <= 0){
            // Destroy the cube when it has no health left
            //Should also play death animation and then destroy once animation is finished
            Destroy(gameObject);
        }
    }
    
    public Vector3 movement;
    public Vector3 idleMovement;
    public float elapsedTime = 0;
    public bool isIdle = false;

    public void enemyMovement(){
        
    }

    public IEnumerator waitForAnimation(string animationName) {
        yield return new WaitForSeconds(animator.GetCurrentAnimatorClipInfo(0)[0].clip.length);
        animator.SetBool(animationName, false);
        animationFinished = true;
    }
}
