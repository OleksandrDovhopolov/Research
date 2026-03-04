using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json.Linq;

public class EventWebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    private readonly string _apiUrl = "wss://i4s32i7qc0.execute-api.eu-north-1.amazonaws.com/production/";

    void Start()
    {
        return;
        ws = new WebSocket(_apiUrl);

        ws.Log.Level = LogLevel.Debug;
        ws.Log.Output = (log, msg) => { Debug.LogWarning(msg); };
        
        ws.OnOpen += (sender, e) =>
        {
            Debug.LogWarning("WS Connected!");
        };

        ws.OnMessage += (sender, e) =>
        {
            Debug.LogWarning("WS Message: " + e.Data);

            JObject msg = JObject.Parse(e.Data);
            string type = msg["type"].ToString();

            if (type == "EVENT_STARTED")
            {
                Debug.LogWarning($"Ивент начался: {msg["name"]}");
            }
            else if (type == "EVENT_ENDED")
            {
                Debug.LogWarning($"Ивент завершён: {msg["name"]}");
            }
        };

        ws.OnClose += (sender, e) =>
        {
            Debug.LogWarning("WS Closed");
        };

        ws.Connect();
    }

    private void OnDestroy()
    {
        ws?.Close();
    }
}