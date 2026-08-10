using ChargedObjects;
using UnityEngine;
using System.Collections;

public class PlaneInPlaneDetector : MonoBehaviour
{
    public GameObject nextInstructionObject; // 下一条指令物体
    public GameObject additionalInstructionObject; // 额外的指令物体
    public GameObject text3; // 用于检测的文本对象
    public GameObject newObject; // 新对象
    public GameObject newObjectAfterDelay; // 延迟后显示的新对象

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

        // 获取所有在Plane内的Plane
        Collider[] hitColliders = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
        int planeCount = 0;
        float? firstElectricMag = null; // 用于存储第一个Plane的electric_mag

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Plane")) // 检查标签是否为"Plane"
            {
                planeCount++;
                ChargedObjects.Plane plane = hitCollider.GetComponent<ChargedObjects.Plane>();
                if (plane != null)
                {
                    if (firstElectricMag == null)
                    {
                        firstElectricMag = plane.electric_mag;
                    }
                    else if (plane.electric_mag != firstElectricMag)
                    {
                        // 如果electric_mag不相同，重置planeCount并退出循环
                        planeCount = 0;
                        break;
                    }
                }
            }
        }

        // 检查是否有两个plane在Plane内且electric_mag相同
        if (planeCount == 4 && !hasNewObjectShown)
        {
            // 隐藏两个指令物体
            nextInstructionObject.SetActive(false);
            additionalInstructionObject.SetActive(false);
            // 显示新对象
            newObject.SetActive(true);
            hasNewObjectShown = true; // 标记newObject已经显示过
            // 2秒后隐藏newObject并显示newObjectAfterDelay
            Invoke("HideNewObjectAndShowNewObjectAfterDelay", 2f);
        }
    }

    private void HideNewObjectAndShowNewObjectAfterDelay()
    {
        newObject.SetActive(false);
        newObjectAfterDelay.SetActive(true);
    }
}
