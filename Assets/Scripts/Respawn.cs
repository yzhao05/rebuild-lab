using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Respawn : MonoBehaviour
{
    // Start is called before the first frame update
    private bool move = false;
    public GameObject prefab;
    public float moverange = 0.25f;
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
        if (move) return;
        if (Vector3.Distance(RespawnPosition, this.transform.position) >moverange) {
            Instantiate(prefab, RespawnPosition, RespawnRotation);
            move = true;
        }
    }
}
