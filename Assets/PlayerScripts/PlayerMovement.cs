using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerMovement : MonoBehaviour
{

    // Variables 

    private CharacterController controller;

    private InputAction moveAction;
    private InputAction jumpAction;

    [SerializeField] private Transform cameraTransform;

    private float verticalVelocity;
    [SerializeField] private float jumpHeight = 1f;

    private void Awake()
    {
        // Get the CharacterController component attached to the player

        controller = GetComponent<CharacterController>();

        PlayerInput playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];

        jumpAction = playerInput.actions["Jump"];

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

        movement.y = verticalVelocity;

        controller.Move(movement * 5f * Time.deltaTime);

    }
}
