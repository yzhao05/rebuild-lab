using ChargedObjects;
using UnityEngine;

public class WireInCubeDetector2 : MonoBehaviour
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

        // 获取所有在Cube内的Wire
        Collider[] hitColliders = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
        int wireCount = 0;
        float? firstElectricMag = null; // 用于存储第一个Wire的electric_mag

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Wire")) // 检查标签是否为"Wire"
            {
                wireCount++;
                Wire wire = hitCollider.GetComponent<Wire>();
                if (wire != null)
                {
                    if (firstElectricMag == null)
                    {
                        firstElectricMag = wire.electric_mag;
                    }
                    else if (wire.electric_mag != firstElectricMag)
                    {
                        // 如果electric_mag不相同，重置wireCount并退出循环
                        wireCount = 0;
                        break;
                    }
                }
            }
        }

        // 检查是否有两个wire在Cube内且electric_mag相同
        if (wireCount == 4 && !hasNewObjectShown)
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
