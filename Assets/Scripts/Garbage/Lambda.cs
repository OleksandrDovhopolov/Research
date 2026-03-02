using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace  core
{
    [Serializable]
    public class GameEvent
    {
        public string id;
        public string name;
        public string startDate;
        public string endDate;
    }

    [Serializable]
    public class GameEventList
    {
        public GameEvent[] events;
    }
    
    public class Lambda : MonoBehaviour
    {
        private readonly string _apiUrl = "https://xhd5uqiok6.execute-api.eu-north-1.amazonaws.com/active-events";
        
        private void Start()
        {
            StartCoroutine(FetchActiveEvents());
        }

        private IEnumerator FetchActiveEvents()
        {
            using var request = UnityWebRequest.Get(_apiUrl);
            // Отправляем запрос
            yield return request.SendWebRequest();

            if (request.result is UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError("ConnectionError :  " + request.error);
            }
            else if (request.result is UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Protocol : " + request.error);
            }
            else
            {
                var jsonResponse = request.downloadHandler.text;
                Debug.Log("Ответ API: " + jsonResponse);

                // Преобразуем JSON в объекты
                try
                {
                    GameEventList eventList =
                        JsonUtility.FromJson<GameEventList>("{\"events\":" + jsonResponse + "}");
                    foreach (var gameEvent in eventList.events)
                    {
                        Debug.LogWarning(
                            $"ID: {gameEvent.id}, Name: {gameEvent.name}, Start: {gameEvent.startDate}, End: {gameEvent.endDate}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Ошибка при разборе JSON: " + e.Message);
                }
            }
        }
    }
}

