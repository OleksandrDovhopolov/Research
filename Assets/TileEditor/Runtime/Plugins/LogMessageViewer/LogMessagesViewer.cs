using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace TileEditor
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LogMessagesViewer : MonoBehaviour
    {
        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private const float _SHOW_SPEED = .8f;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public Color warningColor;
        public Color errorColor;
        //public Text messageText;
        public TextMeshProUGUI messageText;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private Coroutine _showLogRoutine;
        private CanvasGroup _canvasGroup;
        private float _startY;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _startY = transform.localPosition.y;
            gameObject.SetActive(false);
            Application.logMessageReceived += ReceivedLog;
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= ReceivedLog;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private void ReceivedLog(string condition, string stacktrace, LogType type)
        {
            switch (type)
            {
                case LogType.Log: break;
                case LogType.Warning:
                    Show(condition, warningColor);
                    break;

                case LogType.Exception:
                case LogType.Error:
                case LogType.Assert:
                    Show(condition, errorColor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private void Show(string message, Color color)
        {
            gameObject.SetActive(true);
            messageText.text = message;
            messageText.color = color;

            if (_showLogRoutine != null) StopCoroutine(_showLogRoutine);

            _showLogRoutine = StartCoroutine(ShowMessageRoutine());
        }

        private IEnumerator ShowMessageRoutine()
        {
            while (_canvasGroup.alpha < 1)
            {
                _canvasGroup.alpha += _SHOW_SPEED * Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            while (_canvasGroup.alpha > 0)
            {
                _canvasGroup.alpha -= _SHOW_SPEED * Time.deltaTime;
                yield return null;
            }

            _showLogRoutine = null;
            gameObject.SetActive(false);
        }
    }
}