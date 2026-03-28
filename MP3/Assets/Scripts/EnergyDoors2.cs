using UnityEngine;

public class EnergyDoor2 : MonoBehaviour
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

    public EnergyDoor door2;
    public int doornum;

    void Start()
    {
        leftClosed = leftDoor.localPosition;
        rightClosed = rightDoor.localPosition;
    }

    void Update()
    {
        if (doornum == 1)
        {
            if (!doorOpened && resourceManager.currentEnergy >= requiredEnergy)
            {
                doorOpened = true;
                Debug.Log("Door unlocked!");
            }
        }
        if (doornum == 3)
        {
            if (!doorOpened && resourceManager.currentEnergy >= requiredEnergy && door2.doorOpened)
            {
                doorOpened = true;
                Debug.Log("Door unlocked!");
            }
        }

        if (doorOpened)
        {
            leftDoor.localPosition = Vector3.Lerp(
                leftDoor.localPosition,
                leftClosed - Vector3.back * openDistance,
                Time.deltaTime * openSpeed
            );

            rightDoor.localPosition = Vector3.Lerp(
                rightDoor.localPosition,
                rightClosed - Vector3.forward * openDistance,
                Time.deltaTime * openSpeed
            );
        }
    }
}