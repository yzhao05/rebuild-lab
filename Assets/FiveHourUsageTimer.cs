using UnityEngine;
using System;

public class FiveHourUsageTimer : MonoBehaviour
{
    private DateTime startTime;
    private const int MAX_USAGE_HOURS = 24;

    void Start()
    {
        // 如果是第一次启动或者重新启动，记录当前时间作为新的开始时间
        if (!PlayerPrefs.HasKey("StartTime"))
        {
            startTime = DateTime.UtcNow;
            PlayerPrefs.SetString("StartTime", startTime.ToString("o")); // 使用ISO 8601格式存储时间
        }
        else
        {
            startTime = DateTime.Parse(PlayerPrefs.GetString("StartTime"));
        }

        // 检查是否超过使用时间限制
        if (IsUsageTimeExceeded())
        {
            Debug.LogWarning("使用时间已超过5小时限制。");
            Application.Quit();
        }
    }

    private bool IsUsageTimeExceeded()
    {
        // 计算从开始到现在的时间差
        TimeSpan elapsedTime = DateTime.UtcNow - startTime;

        // 判断是否超过5小时
        return elapsedTime.TotalHours >= MAX_USAGE_HOURS;
    }
}