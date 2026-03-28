using UnityEngine;

public class Rotate : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float degreeSpeed = 20f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up * degreeSpeed * Time.deltaTime, Space.World); 
    }
}
