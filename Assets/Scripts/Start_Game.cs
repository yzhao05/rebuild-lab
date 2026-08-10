using UnityEngine;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour
{
    public GameObject objectToShow; // 要显示的对象
    public GameObject text3; // 名为Text(3)的对象
    public GameObject newObjectToShow; // 新增的要显示的对象
    private static bool buttonPressed = false; // 表示按钮是否被按过的静态变量
    public Button[] buttons; // 存放所有按钮的数组
   // private bool isText3shown = false;//标记text3是否已经显示

    private void Start()
    {
        // 获取按钮组件并添加点击事件监听器
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(ShowObject);
        }
    }

    private void ShowObject()
    {
        // 首先检查text3是否是活跃的
        if (text3 != null && text3.activeSelf)
        {
            // 先隐藏text3
            text3.SetActive(false);

            // 显示按钮
            foreach (Button b in buttons)
            {
                b.gameObject.SetActive(true);
            }

            // 显示对象
            if (objectToShow != null)
            {
                objectToShow.SetActive(true);
            }

            // 显示新增的对象
            if (newObjectToShow != null)
            {
                newObjectToShow.SetActive(true);
            }

            // 设置按钮被按过的标记
            buttonPressed = true;
        }
    }

    // 检查按钮是否被按过的方法
    public static bool IsButtonPressed()
    {
        return buttonPressed;
    }
}

