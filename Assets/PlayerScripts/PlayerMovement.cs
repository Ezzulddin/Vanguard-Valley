using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    // Components and input actions for player controller

    private CharacterController controller;

    private InputAction sprintAction;
    private InputAction moveAction;
    private InputAction jumpAction;

    // Store player's vertical velocity for gravity and jumping

    private float verticalVelocity;



    //Reference to the camera transform for movement direction
    [SerializeField] private Transform cameraTransform;

    //Movement settings, exposed in the Unity Inspector for easy tweaking
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;

    private void Awake()
    {
        // Get the CharacterController component attached to the player

        controller = GetComponent<CharacterController>();

        // Get the PlayerInput component to access input actions
        // defined in the Input Actions asset

        PlayerInput playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];

        jumpAction = playerInput.actions["Jump"];

        sprintAction = playerInput.actions["Sprint"];

    }

    private void Update()
    {
        // Character WASD movement based on camera orientation

        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement = forward * input.y + right * input.x;


        if (movement.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
             );

        }



        // Use sprint speed if the sprint action is pressed, otherwise use normal move speed

        float currentSpeed = sprintAction.IsPressed() ? sprintSpeed : moveSpeed;

        Vector3 horizontalMovement = movement * currentSpeed;

        // Gravity on the player

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        //Jump 

        if (controller.isGrounded && jumpAction.WasPressedThisFrame())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
        }

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 finalMovement = horizontalMovement;
        finalMovement.y = verticalVelocity;

        controller.Move(finalMovement * Time.deltaTime);

    }
}
