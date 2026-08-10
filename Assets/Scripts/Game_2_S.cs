using ChargedObjects;
using UnityEngine;
using System.Collections;

public class SphereInCubeDetector : MonoBehaviour
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
            // 获取所有在Cube内的Sphere
            Collider[] hitColliders = Physics.OverlapBox(transform.position, transform.localScale / 2, transform.rotation);
            int positiveSphereCount = 0;
            float? firstPositiveElectricMag = null; // 用于存储第一个正电荷Sphere的electric_mag

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Sphere")) // 检查标签是否为"Sphere"
                {
                    Sphere sphere = hitCollider.GetComponent<Sphere>();
                    if (sphere != null && sphere.electric_mag > 0) // 检查电荷是否为正
                    {
                        positiveSphereCount++;
                        if (firstPositiveElectricMag == null)
                        {
                            firstPositiveElectricMag = sphere.electric_mag;
                        }
                        else if (sphere.electric_mag != firstPositiveElectricMag)
                        {
                            // 如果electric_mag不相同，且已经有两个正电荷Sphere
                            if (positiveSphereCount == 4 && !hasObjectToShowBeenShown)
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
