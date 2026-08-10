using ChargedObjects;
using UnityEngine;

public class SphereInCubeDetector2 : MonoBehaviour
{
    public GameObject nextInstructionObject; // 下一条指令物体
    public GameObject additionalInstructionObject; // 额外的指令物体
    public GameObject text3; // 用于检测的文本对象
    public GameObject newObject; // 新对象
    public GameObject newObjectAfterDelay; // 延迟后显示的新对象
    public GameObject nextInstructionObject2;

    private bool hasNewObjectShown = false; // 用于跟踪newObject是否已经显示过

    private void Update()
    {
        // 检查按钮是否被按过
        if (!ButtonHandler.IsButtonPressed())
        {
            return;
        }

        // 检查text3是否显示
        if (text3.activeSelf)
        {
            // 隐藏text3
            text3.SetActive(false);
        }

        // 获取所有在Cube内的Sphere
        Collider[] hitColliders = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
        int sphereCount = 0;
        float? firstElectricMag = null; // 用于存储第一个Sphere的electric_mag

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Sphere")) // 检查标签是否为"Sphere"
            {
                Sphere sphere = hitCollider.GetComponent<Sphere>();
                if (sphere != null && sphere.electric_mag > 0) // 检查electric_mag是否为正数
                {
                    sphereCount++;
                    if (firstElectricMag == null)
                    {
                        firstElectricMag = sphere.electric_mag;
                    }
                    else if (sphere.electric_mag != firstElectricMag)
                    {
                        // 如果electric_mag不相同，重置sphereCount并退出循环
                        sphereCount = 0;
                        break;
                    }
                }
            }
        }

        // 检查是否有两个sphere在Cube内且electric_mag相同
        if (sphereCount == 4 && !hasNewObjectShown)
        {
            // 隐藏两个指令物体
            nextInstructionObject.SetActive(false);
            additionalInstructionObject.SetActive(false);
            // 显示新对象
            newObject.SetActive(true);
            hasNewObjectShown = true; // 标记newObject已经显示过
            // 延迟1.5秒后隐藏newObject并显示nextInstructionObject2
            Invoke("ShowNextInstructionObject2", 1.5f);
        }
    }

    private void ShowNextInstructionObject2()
    {
        newObject.SetActive(false);
        nextInstructionObject2.SetActive(true);
    }

    private void HideNewObjectAndShowNewObjectAfterDelay()
    {
        newObject.SetActive(false);
        newObjectAfterDelay.SetActive(true);
    }
}
