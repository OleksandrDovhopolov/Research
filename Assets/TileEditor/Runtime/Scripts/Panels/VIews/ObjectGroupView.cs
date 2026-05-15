using System;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class ObjectGroupView : MonoBehaviour
    {
        [SerializeField] private Text _groupName;
        [SerializeField] private SimpleButton _removeFromGroupButton;

        public void Init(string groupName, Action onRemoveAction)
        {
            _groupName.text = groupName;
            _removeFromGroupButton.Init(onRemoveAction);
        }
    }
}