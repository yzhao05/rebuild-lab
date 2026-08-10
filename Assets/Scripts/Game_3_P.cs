using ChargedObjects;
using UnityEngine;
using System.Collections;

public class PlaneInCubeDetector3 : MonoBehaviour
{
    public GameObject instructionObjToHide; // 需要隐藏的指令物体
    public GameObject conditionObj; // 用于判定条件的物体
    public GameObject firstObjToShow; // 第一个需要显示的物体
    public GameObject secondObjToShow; // 第二个需要显示的物体

    private bool hasObjToShowBeenShown = false; // 用于跟踪objectToShow是否已经显示过

    private void Update()
    {
        // 检查判定条件的物体是否处于显示状态
        if (conditionObj.activeSelf)
        {
            // 获取所有在Cube内的Plane
            Collider[] hitColliders = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
            bool hasPositivePlane = false;
            bool hasNegativePlane = false;

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Plane")) // 检查标签是否为"Plane"
                {
                    ChargedObjects.Plane plane = hitCollider.GetComponent<ChargedObjects.Plane>();
                    if (plane != null)
                    {
                        if (plane.electric_mag > 0) // 检查电荷是否为正
                        {
                            hasPositivePlane = true;
                        }
                        else if (plane.electric_mag < 0) // 检查电荷是否为负
                        {
                            hasNegativePlane = true;
                        }

                        // 如果已经找到一个正电荷和一个负电荷的Plane
                        if (hasPositivePlane && hasNegativePlane && !hasObjToShowBeenShown)
                        {
                            // 隐藏需要隐藏的指令物体
                            instructionObjToHide.SetActive(false);
                            // 启动协程显示物体
                            StartCoroutine(ShowObjects2());
                            hasObjToShowBeenShown = true; // 标记objectToShow已经显示过
                            break;
                        }
                    }
                }
            }
        }
    }

    private IEnumerator ShowObjects2()
    {
        // 显示第一个物体
        firstObjToShow.SetActive(true);
        // 等待两秒
        yield return new WaitForSeconds(2);
        // 隐藏第一个物体
        firstObjToShow.SetActive(false);
        // 显示第二个物体
        secondObjToShow.SetActive(true);
    }
}
