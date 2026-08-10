using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scene4 : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnExitGame()
    {
#if UNITY_EDITOR  
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
