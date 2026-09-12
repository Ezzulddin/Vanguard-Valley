using UnityEngine;

public class CursorLock : MonoBehaviour
{
    private void Start()
    {
        // Lock the cursor to the center of the screen and make it invisible
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        //Press Escape to release cursor

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        //Left click again to lock cursor
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }


    }

}
