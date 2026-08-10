using System;

[Serializable]
public class BridgeResponse
{
    public int status_code;
    public string message;
    public DifyResponse dify_response;
}

[Serializable]
public class DifyResponse
{
    public DifyData data;
}

[Serializable]
public class DifyData
{
    public DifyOutputs outputs;
}

[Serializable]
public class DifyOutputs
{
    public string assistant_response_json;
}