using ChargedObjects;
using UnityEngine;
using System.Collections;

public class WireInCubeDetector : MonoBehaviour
{
    public GameObject instructionObjectToHide; // 需要隐藏的指令物体
    public GameObject conditionObject; // 用于判定条件的物体
    public GameObject firstObjectToShow; // 第一个需要显示的物体
    public GameObject secondObjectToShow; // 第二个需要显示的物体

    private bool hasObjectToShowBeenShown = false; // 用于跟踪objectToShow是否已经显示过

    private void Update()
    {
        // 检查判定条件的物体是否处于显示状态
        if (conditionObject.activeSelf)
        {
            // 获取所有在Cube内的Wire
            Collider[] hitColliders = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
            int positiveWireCount = 0;
            float? firstPositiveElectricMag = null; // 用于存储第一个正电荷Wire的electric_mag

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Wire")) // 检查标签是否为"Wire"
                {
                    Wire wire = hitCollider.GetComponent<Wire>();
                    if (wire != null && wire.electric_mag > 0) // 检查电荷是否为正
                    {
                        positiveWireCount++;
                        if (firstPositiveElectricMag == null)
                        {
                            firstPositiveElectricMag = wire.electric_mag;
                        }
                        else if (wire.electric_mag != firstPositiveElectricMag)
                        {
                            // 如果electric_mag不相同，且已经有两个正电荷Wire
                            if (positiveWireCount == 4 && !hasObjectToShowBeenShown)
                            {
                                // 隐藏需要隐藏的指令物体
                                instructionObjectToHide.SetActive(false);
                                // 启动协程显示物体
                                StartCoroutine(ShowObjects());
                                hasObjectToShowBeenShown = true; // 标记objectToShow已经显示过
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private IEnumerator ShowObjects()
    {
        // 显示第一个物体
        firstObjectToShow.SetActive(true);
        // 等待两秒
        yield return new WaitForSeconds(2);
        // 隐藏第一个物体
        firstObjectToShow.SetActive(false);
        // 显示第二个物体
        secondObjectToShow.SetActive(true);
    }
}
