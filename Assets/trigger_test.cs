using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ontrigger : MonoBehaviour
{
    public GameObject playerText; // ĞŞÕıÆ´Ğ´´íÎó

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("box")) // ĞŞÕıÆ´Ğ´´íÎó
        {
            playerText.SetActive(true);
        }
    }
}
