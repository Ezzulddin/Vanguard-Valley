using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    //Movement settings
    [SerializeField] private Transform player;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stoppingDistance = 2f;


    //Attack settings
    [SerializeField] private float attackDamage = 5f;
    [SerializeField] private float attackCooldown = 1f;

    private float attackTimer;
    private CharacterController controller;

    private void Awake()
    {
        // Get the CharacterController component attached to the enemy
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f; // Keep the enemy on the same horizontal plane

        if (direction.magnitude > stoppingDistance)
        {
            direction.Normalize();

            controller.Move(direction * moveSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
        }
        else
        {
            Attack();
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    private void Attack()
    {
        if (attackTimer > 0f)
        {
            return;
        }

        attackTimer = attackCooldown;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }

}
