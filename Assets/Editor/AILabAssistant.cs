using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Text;
using Unity.EditorCoroutines.Editor;

public class AILabAssistant : EditorWindow
{
    private string userRequirement = "在这里输入你的场景搭建需求...";
    private string apiKey = "YOUR_API_KEY_HERE"; // 替换为你的大模型 API Key
    private string apiUrl = "https://api.deepseek.com/v1/chat/completions"; // 可更换为OpenAI/O1等其他兼容接口
    private bool isProcessing = false;

    // 在 Unity 顶部菜单栏添加一个打开窗口的入口
    [MenuItem("Tools/AI 场景助手")]
    public static void ShowWindow()
    {
        GetWindow<AILabAssistant>("AI 场景助手");
    }

    // 绘制 Unity 内部的交互 UI
    private void OnGUI()
    {
        GUILayout.Label("EM VR Lab 效率辅助工具", EditorStyles.boldLabel);
        
        // 1. 输入框
        GUILayout.Label("请描述你想要搭建的实验场景布局:");
        userRequirement = EditorGUILayout.TextArea(userRequirement, GUILayout.Height(100));

        // 2. 核心交互按钮
        EditorGUI.BeginDisabledGroup(isProcessing);
        if (GUILayout.Button("让 AI 替我搭建场景", GUILayout.Height(40)))
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("YOUR_"))
            {
                Debug.LogError("请先在脚本中配置合法的 API Key！");
                return;
            }
            // 启动 Unity 编辑器协程处理网络请求
            isProcessing = true;
            EditorCoroutineUtility.StartCoroutineOwnerless(SendPromptToAI());
        }
        EditorGUI.EndDisabledGroup();

        // 3. 状态显示
        if (isProcessing)
        {
            EditorGUILayout.HelpBox("AI 正在疯狂编写代码并搭建场景，请稍候...", MessageType.Info);
        }
    }

    private IEnumerator SendPromptToAI()
    {
        // 严格限制 AI 只能输出纯 C# 代码，不能带有 Markdown 标记 (如 ```csharp)
        string systemPrompt = "You are an expert Unity C# developer. Write a complete Unity C# script named 'GeneratedLabScene' that inherits from MonoBehaviour. " +
                              "In its Start() method, it must programmatically create and position 3D GameObjects to build the scene according to the user's request. " +
                              "Do NOT include any explanations, markdown code blocks, or preamble. Return ONLY the raw C# text.";

        string escapedPrompt = userRequirement.Replace("\"", "\\\"");
        string jsonBody = "{\"model\": \"deepseek-chat\", \"messages\": [" +
                          "{\"role\": \"system\", \"content\": \"" + systemPrompt + "\"}," +
                          "{\"role\": \"user\", \"content\": \"" + escapedPrompt + "\"}" +
                          "], \"temperature\": 0.1}";

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 解析返回的 JSON 提取代码文本（这里简化处理，实际开发中建议使用 JsonUtility 解析 response）
                string responseText = request.downloadHandler.text;
                string generatedCode = ExtractCodeFromResponse(responseText);

                // 将代码写入本地文件
                string folderPath = Application.dataPath + "/GeneratedScenes/";
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                
                string filePath = folderPath + "GeneratedLabScene.cs";
                File.WriteAllText(filePath, generatedCode);

                Debug.Log("🎉 AI 场景脚本生成成功！路径: " + filePath);
                
                // 强制 Unity 刷新并就地编译新生成的脚本
                AssetDatabase.Refresh();
                
                // 编译完成后自动挂载并运行场景
                EditorApplication.delayCall += AutoSetupScene;
            }
            else
            {
                Debug.LogError("AI 请求失败: " + request.error);
            }
        }
        isProcessing = false;
    }

    private string ExtractCodeFromResponse(string json)
    {
        // 这是一个轻量级的解析提取逻辑，实际建议配合 Newtonsoft.Json 提取 choices[0].message.content
        // 此处假设 AI 已经乖乖返回了纯文本类代码
        var match = System.Text.RegularExpressions.Regex.Match(json, @"\""content\""\s*:\s*\""([\s\S]*?)\""");
    if (match.Success)
    {
        string rawCode = match.Groups[1].Value;
        // 处理 JSON 转义字符（如换行符 \n 和双引号 \u0022）
        rawCode = System.Text.RegularExpressions.Regex.Unescape(rawCode);
        return rawCode;
    }
        return json; 
    }

    private void AutoSetupScene()
    {
        // 创建一个空物体并将 AI 刚写好的组件挂载上去，直接激活场景搭建
        GameObject sceneBuilder = new GameObject("AI_Generated_Scene");
        // 等待 Unity 编译完成后，你可以手动或通过反射将 GeneratedLabScene 挂载上去
        Debug.Log("请在编译完成后，将 Assets/GeneratedScenes/GeneratedLabScene 脚本拖给场景中的 [AI_Generated_Scene] 物体以查看成果！");
    }
}