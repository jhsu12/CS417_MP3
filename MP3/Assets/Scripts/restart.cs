using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class restart : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public InputActionReference restartButton;
    void Start()
    {
        restartButton.action.Enable();
        restartButton.action.performed += (ctx) =>
        {
            SceneManager.LoadScene("TestingXR");
        };

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
