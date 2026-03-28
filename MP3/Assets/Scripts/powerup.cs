using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class powerup : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private XRSocketInteractor socket;
    public ResourceManager manager;
    void Start()
    {
        socket = GetComponent<XRSocketInteractor>();
        socket.selectEntered.AddListener(_ =>
        {
            checkPowerup();
        });
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
}
