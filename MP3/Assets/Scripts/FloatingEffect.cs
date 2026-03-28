using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FloatingEffect : MonoBehaviour
{
    public float speed = 2f;
    public float height = 0.1f;

    private Rigidbody rb;
    private XRGrabInteractable interactable;
    private Vector3 startPosition;
    private bool isGrabbed;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        interactable = GetComponent<XRGrabInteractable>();

        rb.isKinematic = true;

        interactable.selectEntered.AddListener(_ =>
        {
            isGrabbed = true;
            rb.isKinematic = false;
        });

        interactable.selectExited.AddListener(_ =>
        {
            isGrabbed = false;
            rb.isKinematic = true; 
            startPosition = rb.position; 
        });
    }

    private void OnEnable()
    {
        startPosition = rb.position;
    }

    void FixedUpdate()
    {
        if (isGrabbed) return;

        float y = startPosition.y + Mathf.Sin(Time.time * speed) * height;
        Vector3 newPos = new Vector3(startPosition.x, y, startPosition.z);
        rb.MovePosition(newPos);
    }
}
