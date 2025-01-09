using UnityEngine;

public class Checkpoints : MonoBehaviour
{
    public Transform player;
    public Transform defaultPosition;
    public Transform[] targets;


    private void Start()
    {
        player.position = defaultPosition.position;
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha8))
        {
            TeleportPlayer(0); 
        }
        else if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha9))
        {
            TeleportPlayer(1); 
        }
        else if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha0))
        {
            TeleportPlayer(2); 
        }
        else if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Alpha4))
        {
            TeleportPlayer(3);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.Alpha5))
        {
            TeleportPlayer(4);
        }
        else if (Input.GetKeyDown(KeyCode.Keypad6) || Input.GetKeyDown(KeyCode.Alpha6))
        {
            TeleportPlayer(5);
        }
    }

    void TeleportPlayer(int targetIndex)
    {
     
            player.position = targets[targetIndex].position; 
      
    }
}