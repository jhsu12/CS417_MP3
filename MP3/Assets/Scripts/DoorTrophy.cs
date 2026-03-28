using UnityEngine;

public class FinalTrophy : MonoBehaviour
{
    public EnergyDoor door1;
    public EnergyDoor2 door2;
    public EnergyDoor2 door3;

    public GameObject trophy;

    private bool trophySpawned = false;

    void Start()
    {
        if (trophy != null)
            trophy.SetActive(false);
    }

    void Update()
    {
        // Check if all doors are open
        if (!trophySpawned && door1.doorOpened && door2.doorOpened && door3.doorOpened)
        {
            trophySpawned = true;

            if (trophy != null)
                trophy.SetActive(true);

            Debug.Log("All doors opened! Trophy unlocked!");
        }

        // Rotate trophy after it appears
        if (trophySpawned && trophy != null)
        {
            trophy.transform.Rotate(0, 0, 60 * Time.deltaTime);
        }
    }
}