using System;

[Serializable]
public class AssistantResponse
{
    public Reply reply;
}

[Serializable]
public class Reply
{
    public string text;
    public string tts_text;
}
