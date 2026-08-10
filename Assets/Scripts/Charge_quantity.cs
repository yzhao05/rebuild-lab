using UnityEngine;
using System.Collections.Generic;

public class FollowNewSphere : MonoBehaviour
{
    public Transform sphereTransform; // 原始球体的Transform
    public GameObject followerObjectPrefab; // 跟随物体的预制体
    private List<Transform> spheres = new List<Transform>(); // 存储所有球体的列表
    private List<GameObject> followers = new List<GameObject>(); // 存储所有跟随物体的列表

    void Start()
    {
        // 初始化时添加原始球体
        AddSphere(sphereTransform);
    }

    // 添加一个新的球体到列表中
    public void AddSphere(Transform newSphereTransform)
    {
        spheres.Add(newSphereTransform);
        // 创建一个新的跟随物体
        GameObject newFollower = Instantiate(followerObjectPrefab);
        followers.Add(newFollower);
    }

    void LateUpdate()
    {
        for (int i = 0; i < spheres.Count; i++)
        {
            if (spheres[i] != null && followers[i] != null)
            {
                // 将跟随物体的位置设置为当前球体的正上方
                Vector3 newPosition = new Vector3(spheres[i].position.x, spheres[i].position.y + 0.5f, spheres[i].position.z);
                followers[i].transform.position = newPosition;
            }
        }
    }
}
