using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    private InputAction attackAction;

    [SerializeField] private float AttackCooldown = 0.5f;

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
        attackTimer = AttackCooldown;
        // Implement attack logic here (e.g., play animation, detect hits, etc.)





        Debug.Log("Player attacked!");
    }




}
