using UnityEngine;

public class EnergyDoor : MonoBehaviour
{
    public ResourceManager resourceManager;

    public float requiredEnergy = 50f;

    public bool doorOpened = false;

    public Transform leftDoor;
    public Transform rightDoor;

    public float openDistance = 1.5f;
    public float openSpeed = 2f;

    Vector3 leftClosed;
    Vector3 rightClosed;
    public int doornum;
    public EnergyDoor2 door1;

    void Start()
    {
        leftClosed = leftDoor.localPosition;
        rightClosed = rightDoor.localPosition;
    }

    void Update()
    {
        if (doornum == 2)
        {
            if (!doorOpened && resourceManager.currentEnergy >= requiredEnergy && door1.doorOpened)
            {
                doorOpened = true;
                Debug.Log("Door unlocked!");
            }
        }

        if (doorOpened)
        {
            leftDoor.localPosition = Vector3.Lerp(
                leftDoor.localPosition,
                leftClosed + leftDoor.right * openDistance,
                Time.deltaTime * openSpeed
            );

            rightDoor.localPosition = Vector3.Lerp(
                rightDoor.localPosition,
                rightClosed - rightDoor.right * openDistance,
                Time.deltaTime * openSpeed
            );
        }
    }
}