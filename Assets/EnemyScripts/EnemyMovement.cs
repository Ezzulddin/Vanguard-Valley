using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private CharacterController controller;

    private float verticalVelocity;

    private void Awake()
    {
        // Get the CharacterController component attached to the enemy
        controller = GetComponent<CharacterController>();

    }

    private void Update()
    {
        //Keep the enemy grounded and apply gravity
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // Small negative value to keep the enemy grounded

        }

        //apply gravity
        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        //Apply vertical movement to the enemy
        Vector3 movement = Vector3.zero;
        movement.y = verticalVelocity;

        controller.Move(movement * Time.deltaTime);



    }
}
