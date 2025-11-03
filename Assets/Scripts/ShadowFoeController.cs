using UnityEngine;
using System.Collections;

public class ShadowFoeController : MonoBehaviour
{
    [Header("Components")]
    public Animator foeAnimator;
    public Rigidbody2D rb;
    public Transform player;

    [Header("Stats")]
    public int maxHP = 80;
    private int currentHP;
    public float walkSpeed = 2.5f;

    [Header("AI Settings")]
    public float detectionRadius = 6f;
    public float attackCooldown = 2f;

    private bool isDead = false;
    private bool isAttacking = false;
    private bool isHurt = false;
    private float lastAttackTime = -Mathf.Infinity;

    [Header("Attack Settings")]
    public Transform attackPoint;
    public LayerMask playerLayer;

    // Individual attack stats
    public int dashDamage = 15;
    public float dashRange = 1.5f;

    public int smogDamage = 8;
    public float smogRange = 2.5f;
    public float poisonDuration = 30f; // Smog debuff duration

    public int spikeDamage = 20;
    public float spikeRange = 1.2f;

    void Start()
    {
        currentHP = maxHP;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (isHurt)
        {
            StopMovement();
            return;
        }

        if (distance <= detectionRadius)
        {
            FacePlayer();

            if (!isAttacking)
            {
                if (distance > Mathf.Max(dashRange, smogRange, spikeRange))
                    WalkTowardPlayer();
                else
                    TryAttack();
            }
        }
        else
        {
            StopMovement();
        }

        foeAnimator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));
    }

    void FacePlayer()
    {
        Vector3 scale = transform.localScale;
        scale.x = player.position.x > transform.position.x ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    void WalkTowardPlayer()
    {
        foeAnimator.SetBool("isAttacking", false);
        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(dir.x * walkSpeed, rb.velocity.y);
    }

    void StopMovement()
    {
        rb.velocity = Vector2.zero;
    }

    void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;
        StopMovement();
        isAttacking = true;
        foeAnimator.SetBool("isAttacking", true);

        // Randomly select attack
        float r = Random.value;
        if (r < 0.33f)
            StartCoroutine(PerformAttack("DashAttack", dashDamage, dashRange, false));
        else if (r < 0.66f)
            StartCoroutine(PerformAttack("SmogAttack", smogDamage, smogRange, true));
        else
            StartCoroutine(PerformAttack("SpikeAttack", spikeDamage, spikeRange, false));
    }

    IEnumerator PerformAttack(string triggerName, int damage, float range, bool applyPoison)
    {
        foeAnimator.SetTrigger(triggerName);

        // Delay to match the attack’s impact frame
        yield return new WaitForSeconds(0.4f);

        // Detect player in range
        Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, range, playerLayer);
        if (hit != null)
        {
            if (hit.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage);
                if (applyPoison)
                    playerHealth.ApplyPoison(poisonDuration);
            }
        }

        // End attack after animation duration
        yield return new WaitForSeconds(0.7f);
        isAttacking = false;
        foeAnimator.SetBool("isAttacking", false);
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        currentHP -= dmg;

        if (!isAttacking)
            StartCoroutine(HandleHurt());

        if (currentHP <= 0)
            Die();
    }

    IEnumerator HandleHurt()
    {
        isHurt = true;
        foeAnimator.SetBool("isHurt", true);
        StopMovement();
        yield return new WaitForSeconds(0.4f);
        foeAnimator.SetBool("isHurt", false);
        isHurt = false;
    }

    void Die()
    {
        isDead = true;
        StopMovement();
        foeAnimator.SetBool("isDead", true);
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, Mathf.Max(dashRange, smogRange, spikeRange));
    }
}
