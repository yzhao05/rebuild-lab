using ChargedObjects;
using UnityEngine;
using System.Collections;

public class WireInCubeDetector3 : MonoBehaviour
{
    public GameObject instructionObjectToHide; // 需要隐藏的指令物体
    public GameObject conditionObject; // 用于判定条件的物体
    public GameObject firstObjectToDisplay; // 第一个需要显示的物体
    public GameObject secondObjectToDisplay; // 第二个需要显示的物体

    private bool hasDisplayedObjects = false; // 用于跟踪物体是否已经显示过

    private void Update()
    {
        // 检查判定条件的物体是否处于显示状态
        if (conditionObject.activeSelf)
        {
            // 获取所有在Cube内的Wire
            Collider[] collidersInCube = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
            bool hasPositiveWire = false;
            bool hasNegativeWire = false;

            foreach (var colliderInCube in collidersInCube)
            {
                if (colliderInCube.CompareTag("Wire")) // 检查标签是否为"Wire"
                {
                    Wire wire = colliderInCube.GetComponent<Wire>();
                    if (wire != null)
                    {
                        if (wire.electric_mag > 0) // 检查电荷是否为正
                        {
                            hasPositiveWire = true;
                        }
                        else if (wire.electric_mag < 0) // 检查电荷是否为负
                        {
                            hasNegativeWire = true;
                        }

                        // 如果已经找到一正一负的Wire
                        if (hasPositiveWire && hasNegativeWire && !hasDisplayedObjects)
                        {
                            // 隐藏需要隐藏的指令物体
                            instructionObjectToHide.SetActive(false);
                            // 启动协程显示物体
                            StartCoroutine(DisplayObjects());
                            hasDisplayedObjects = true; // 标记物体已经显示过
                            break;
                        }
                    }
                }
            }
        }
    }

    private IEnumerator DisplayObjects()
    {
        // 显示第一个物体
        firstObjectToDisplay.SetActive(true);
        // 等待两秒
        yield return new WaitForSeconds(2);
        // 隐藏第一个物体
        firstObjectToDisplay.SetActive(false);
        // 显示第二个物体
        secondObjectToDisplay.SetActive(true);
    }
}

