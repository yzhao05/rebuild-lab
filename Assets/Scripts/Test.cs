using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public GameObject player_Step3;
    public GameObject button;
     // 确保拼写正确
    public AudioSource audioSource; // 引用音频源组件
    public AudioClip audioClip; // 要播放的音频剪辑

    private bool audioPlayed = false; // 用于跟踪音频是否已经播放过

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("box")) // 确保拼写正确
        {
          
          
                button.SetActive(false);
                player_Step3.SetActive(true);


                // 播放音频
                if (!audioPlayed && audioSource != null && audioClip != null)
                {
                    audioSource.PlayOneShot(audioClip);
                    audioPlayed = true; // 设置为已播放
                }
            
        }
    }
}
