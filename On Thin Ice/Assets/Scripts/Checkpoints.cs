using UnityEngine;

public class Checkpoints : MonoBehaviour
{
    public Transform playerTransform;
    public Transform defaultPosition;
    public Transform[] targetsTransforms;
    public Transform waterTransform;
    public float waterYCheat;

    public float[] cpDistances;
    private int closestCheckpointIndex;

    private void Start()
    {
        playerTransform.position = defaultPosition.position;
    }

    void Update()
    {
        ClosestCheckpoint();
        FallInWater();
        ManageCheckpointInputs();
    }
    void ManageCheckpointInputs()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha8))
        {
            TeleportPlayer(0);
        }
        if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha9))
        {
            TeleportPlayer(1);
        }
        if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha0))
        {
            TeleportPlayer(2);
        }
        if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Minus))
        {
            TeleportPlayer(3);
        }
        if (Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.Alpha5))
        {
            TeleportPlayer(4);
        }
    }
    void FallInWater()
    {
        if (playerTransform.position.y <= waterTransform.position.y + waterYCheat)
        {
            TeleportPlayer(closestCheckpointIndex);
        }
    }

    void ClosestCheckpoint()
    {
        int chosenIndex = 0;
        Vector3 playerTransformWithoutY = playerTransform.transform.position;
        playerTransformWithoutY.y = 0;        
        Vector3 ChosenCheckpointTransformWithoutY = targetsTransforms[chosenIndex].transform.position;
        ChosenCheckpointTransformWithoutY.y = 0;

        for (int i = 0; i < targetsTransforms.Length; i++)
        {
            Vector3 checkpointTransformWithoutY = targetsTransforms[i].transform.position;
            checkpointTransformWithoutY.y = 0;
            cpDistances[i] = Vector3.Distance(playerTransformWithoutY, checkpointTransformWithoutY);

            float distToCheckpoint = Vector3.Distance(playerTransformWithoutY, ChosenCheckpointTransformWithoutY);
            if (distToCheckpoint >= cpDistances[i])
            {
                chosenIndex = i;
            }
        }
        closestCheckpointIndex = chosenIndex;
    }

    void TeleportPlayer(int targetIndex)
    {
        playerTransform.position = targetsTransforms[targetIndex].position;
    }
}