using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class GroupView : MonoBehaviour
    {
        [SerializeField] private Text _groupNameText;
        [SerializeField] private SimpleButton _toggleGroupButton;
        [SerializeField] private SimpleButton _highlightGroupButton;
        [SerializeField] private SimpleButton _inspectGroupButton;
        [SerializeField] private SimpleButton _deleteGroupButton;

        public void Init(string groupName, Location location, Action inspectGroupAction)
        {
            var groupObjects = location.GetObjectsFromGroup(groupName).ToList();

            _groupNameText.text = $"{groupName} ({groupObjects.Count})";

            _toggleGroupButton.Init(() =>
            {
                if (groupObjects.Count == 0) return;
                var newActive = !groupObjects[0].gameObject.activeSelf;
                groupObjects.ForEach(lo => lo.gameObject.SetActive(newActive));
            });

            _highlightGroupButton.Init(() => groupObjects.ForEach(lo => lo.Highlight()));

            _inspectGroupButton.Init(() => { inspectGroupAction?.Invoke(); });

            _deleteGroupButton.Init(() =>
            {
                location.RemoveGroup(groupName);
                Destroy(gameObject);
            });
        }
    }
}