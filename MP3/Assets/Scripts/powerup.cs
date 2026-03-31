using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

public class powerup : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private XRSocketInteractor socket;
    public ResourceManager manager;
    public GameObject powerUpprefab;

    public int lifetime;
    public int spawnInterval;
    public Transform[] spawnPoints;
    void Start()
    {
        socket = GetComponent<XRSocketInteractor>();
        socket.selectEntered.AddListener(_ =>
        {
            checkPowerup();
        });

        StartCoroutine(SpawnRoutine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void checkPowerup()
    {
        GameObject selectTarget = socket.GetOldestInteractableSelected().transform.gameObject;
        if (selectTarget != null && selectTarget.tag == "MineralPro")
        {
            manager.mineralBoostRate = 4f;
            manager.mineralRate += 4f;
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // Spawn powerup
            int index = Random.Range(0, spawnPoints.Length);
            Transform spawnpoint = spawnPoints[index];
            GameObject powerup = Instantiate(powerUpprefab, spawnpoint.position, Quaternion.identity);

            yield return new WaitForSeconds(lifetime);
            if (powerup != null)
                Destroy(powerup);
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
