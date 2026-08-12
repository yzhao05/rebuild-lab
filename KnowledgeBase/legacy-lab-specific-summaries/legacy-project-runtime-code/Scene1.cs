using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Scene1 : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnLoginButtonClick()
    {
        SceneManager.LoadScene("Static_Electric_Field");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
