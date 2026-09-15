using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    private InputAction attackAction;

    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackCooldown = 0.5f;

    //reference to attack hitbox
    [SerializeField] private BoxCollider attackHitbox;

    [SerializeField] private LayerMask enemyLayer;

    [SerializeField] private Animator animator;

    private float attackTimer;

    private void Awake()
    {
        //Get Player Input component and access the attack action
        PlayerInput playerInput = GetComponent<PlayerInput>();
        attackAction = playerInput.actions["Attack"];


    }

    private void Update()
    {
        // Reduce the attack cooldown over time
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }


        //Start an attack when the Attack button is pressed
        // and the attack cooldown has finished
        if (attackAction.WasPressedThisFrame() && attackTimer <= 0f)
        {
            Attack();
        }

    }

    private void Attack()
    {
        // Reset the attack cooldown timer
        attackTimer = attackCooldown;

        Debug.Log("Animator enabled: " + animator.enabled);
        Debug.Log("Triggering isPunching on: " + animator.gameObject.name);



        animator.SetTrigger("isPunching");

        Physics.SyncTransforms();

        // Implement attack logic here (e.g., play animation, detect hits, etc.)
        // Check for enemies in range
        Collider[] hits = Physics.OverlapBox(
            attackHitbox.bounds.center,
            attackHitbox.bounds.extents,
            attackHitbox.transform.rotation,
            enemyLayer,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {

            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();

            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
            }
        }
    }

}
