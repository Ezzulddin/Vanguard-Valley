using UnityEngine;
using UnityEngine.InputSystem;

public class CursorLock : MonoBehaviour
{

    private void Start()
    {
        //Lock the cursor to the center of the game window
        Cursor.lockState = CursorLockMode.Locked;

        //Hide the cursor from view
        Cursor.visible = false;

    }

    private void Update()
    {
        // Press ESC to release the mouse cursor 
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Left click to lock the cursor again
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }


    }


}
