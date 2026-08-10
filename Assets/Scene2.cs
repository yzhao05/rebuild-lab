using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene2 : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnLoginButtonClick()
    {
        SceneManager.LoadScene("Static_Magnetic_Field");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
