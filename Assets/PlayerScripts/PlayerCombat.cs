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

        Debug.Log("Number of objects hit: " + hits.Length);

        foreach (Collider hit in hits)
        {

            Debug.Log("Attack hit" + hit.gameObject.name);

            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();

            if (enemy != null)
            {
                Debug.Log("Enemy found! Applying damage.");
                enemy.TakeDamage(attackDamage);
            }
        }


        //print to console when the player attacks
        Debug.Log("Player attacked!");
    }




}
