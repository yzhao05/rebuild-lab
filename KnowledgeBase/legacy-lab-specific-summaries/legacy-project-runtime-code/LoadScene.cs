using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class LoadScene : MonoBehaviour
{
    // Start is called before the first frame update
    public float moverange = 0.05f;
    public string Scene_name;
    private Vector3 RespawnPosition;
    private Quaternion RespawnRotation;
    void Start()
    {
        RespawnPosition = this.transform.position;
        RespawnRotation = this.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(RespawnPosition, this.transform.position) > moverange)
        {
            SceneManager.LoadScene(Scene_name);
        }
    }
}
