using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SocketManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private XRSocketInteractor socket;
    public float maxResource;
    private float currResource = 0;

    public float maxGenerators;
    private float currGenerators = 0;

    public string resourceName;
    public TextMeshProUGUI generator_counterText;
    public TextMeshProUGUI mineral_counterText;

    public GameObject generatorParent;
    public GameObject padParent;

    public ResourceManager manager;

    public AudioSource audioSource;
    public AudioClip mineralComplete;
    public AudioClip clickerComplete;

    void Start()
    {
        socket = GetComponent<XRSocketInteractor>();
        socket.selectEntered.AddListener(_ =>
        {
            addResource();
        });

        // text
        //generator_counterText.text = $"{currGenerators}/{maxGenerators} crafted.";
        mineral_counterText.text = $"{currResource}/{maxResource} of {resourceName} acquired.";
    }

    // adds one resource to the counter 
    private void addResource()
    {
        audioSource.clip = clickerComplete;
        audioSource.Play();
        currResource += 1;
        GameObject selectTarget = socket.GetOldestInteractableSelected().transform.gameObject;
        if (selectTarget != null && selectTarget.tag == "Mineral")
        {
            Destroy(selectTarget);
            mineral_counterText.text = $"{currResource}/{maxResource} of {resourceName} acquired.";
            if (currResource == maxResource)
            {
                audioSource.clip = mineralComplete;
                audioSource.Play();
                currGenerators += 1;
                manager.num_mineralgenerators += 1;
                //generator_counterText.text = $"{currGenerators}/{maxGenerators} crafted.";
                int gen_i = (int)currGenerators - 1;
                Transform child = generatorParent.transform.GetChild(gen_i);
                Transform child_pad = padParent.transform.GetChild(gen_i);
                child.gameObject.SetActive(true);
                child_pad.gameObject.SetActive(false);

                currResource = 0;
                mineral_counterText.text = $"{currResource}/{maxResource} of {resourceName} acquired.";
            }
        }

    }
}
