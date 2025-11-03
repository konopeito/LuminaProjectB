using UnityEngine;

public class SwordFoeController : MonoBehaviour
{
    [Header("Components")]
    public Animator foeAnimator;
    public Rigidbody2D rb;
    public Transform player;

    [Header("Stats")]
    public int maxHP = 100;
    private int currentHP;
    public float walkSpeed = 2f;
    public float runSpeed = 4f;

    [Header("AI Settings")]
    public float detectionRadius = 5f; // Player detection
    public float attackRadius = 2f;    // Distance to start attack
    public float attackCooldown = 1.5f; // Seconds between attacks
    private float lastAttackTime = -Mathf.Infinity;

    private bool isDead = false;
    private bool isAttacking = false;

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

        if (distance <= detectionRadius)
        {
            FacePlayer();

            if (!isAttacking)
            {
                if (distance > attackRadius)
                {
                    // Move toward player
                    Move(distance > 5f); // run if far, walk if mid-distance
                }
                else
                {
                    StopMovement();
                    TryAttack();
                }
            }
        }
        else
        {
            StopMovement();
        }
    }

    void FacePlayer()
    {
        Vector3 scale = transform.localScale;
        scale.x = player.position.x > transform.position.x ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    void Move(bool running)
    {
        foeAnimator.SetBool("isWalking", !running);
        foeAnimator.SetBool("isRunning", running);
        float speed = running ? runSpeed : walkSpeed;
        Vector2 dir = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
    }

    void StopMovement()
    {
        rb.velocity = Vector2.zero;
        foeAnimator.SetBool("isWalking", false);
        foeAnimator.SetBool("isRunning", false);
    }

    void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;
        isAttacking = true;
        foeAnimator.SetBool("isAttacking", true);

        int atkType = Random.value < 0.7f ? 1 : 2;
        foeAnimator.SetInteger("attackType", atkType);

        if (Random.value < 0.3f)
            foeAnimator.SetTrigger("combo");

        Invoke(nameof(ResetAttack), 0.5f); // Match animation length
    }

    void ResetAttack()
    {
        isAttacking = false;
        foeAnimator.SetBool("isAttacking", false);
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;
        currentHP -= dmg;

        foeAnimator.SetBool("isHurt", true);
        Invoke(nameof(ResetHurt), 0.3f);

        if (currentHP <= 0) Die();
    }

    void ResetHurt() => foeAnimator.SetBool("isHurt", false);

    void Die()
    {
        isDead = true;
        StopMovement();
        foeAnimator.SetBool("isDead", true);
        Destroy(gameObject, 2f);
    }
}
