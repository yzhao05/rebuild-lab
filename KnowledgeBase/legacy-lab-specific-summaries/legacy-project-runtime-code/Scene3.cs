using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene3 : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnLoginButtionClick()
    {
        SceneManager.LoadScene("Wave_Polarization");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
