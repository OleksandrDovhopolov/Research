using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlmbM23SDK;
using UnityEngine;

namespace Fabros.TileEditor
{
    [DisallowMultipleComponent]
    public class LocationObject : MonoBehaviour
    {
        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private const float _HIGHLIGHT_SPEED = 4f;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        [SerializeField] private string _category;
        [SerializeField] private string _subcategory;
        [SerializeField] private string _description;
        [Tooltip("Will this object spawn immediately or in background")]
        [SerializeField] private bool _needPreload;
#if UNITY_EDITOR
         [SerializeField] private Sprite _preview;
#endif

        private BaseProperty[] _baseProperties;
        private List<string> _groups;
        private string _name;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual string Uid => Name;

        public virtual string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                    _name = gameObject.name.Replace("(Clone)", "");

                return _name;
            }
        }

        public string Category
        {
            get => _category;
            set => _category = value;
        }

        public string SubCategory
        {
            get => _subcategory;
            set => _subcategory = value;
        }

        public string Description => _description;
        public LocationCell Cell { get; private set; }
        public string InstanceId { get; private set; }
        public Sprite GetPreviewSprite() =>
#if UNITY_EDITOR
            _preview;
#else
            null;
#endif
        
        public bool IsBrushMode { get; private set; }
        public event Action<bool> OnInitDone;
        public event Action OnBrushMode;
        public event Action<EditorTileCell> OnBeforeBrush;
        public event Action<EditorTileCell> OnAfterBrush;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Groups logic

        public bool IsInGroup(string groupName) => _groups.Exists(gr => gr == groupName);

        public bool AddToGroup(string groupName)
        {
            if (IsInGroup(groupName)) return false;

            _groups.Add(groupName);
            return true;
        }

        public bool RemoveFromGroup(string groupName) => _groups.Remove(groupName);

        public IEnumerable<string> IterateGroups() => _groups;

        public void DestroyObject() => Cell.RemoveObject(this);

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Properties logic

        public bool TryGetProperty(string propertyName, Type propertyType, out BaseProperty property)
        {
            property = null;

            var baseProperty = Array.Find(_baseProperties, bp => bp.GetName() == propertyName);
            if (baseProperty == null) return false;

            if (baseProperty.GetType() != propertyType) return false;

            property = baseProperty;
            return true;
        }

        public bool TryGetProperty<T>(string propertyName, out T property) where T : BaseProperty
        {
            property = null;

            var baseProperty = Array.Find(_baseProperties, bp => bp.GetName() == propertyName);
            if (baseProperty == null) return false;
            
            property = (T)baseProperty;
            return property != null;
        }
        
        public bool TryParseProperty<T>(string propertyName, out T result)
        {
            result = default;
            
            if (TryGetProperty<GenericProperty<T>>(propertyName, out var idProperty))
            {
                result = idProperty.GetGenericValue();
                return true;
            }
            
            return false;
        }
        
        public bool TryParseEnumProperty<T>(string propertyName, out T result) where T : struct
        {
            result = default;
            
            if (TryGetProperty<EnumProperty>(propertyName, out var property))
            {
                return Enum.TryParse(property.GetEnumValueName(), out result);
            }
            
            return false;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual void SetOpacity(float value)
        {

        }

        public virtual bool AllowAddObjectWithSameId(LocationObject newObject, out string denyReason)
        {
            denyReason = "Object with same UID and same offset already exists!";
            return false;
        }

        protected virtual void OnInit(LocationObjectModel objectModel) { /* */ }

        public void PrepareToBrush(EditorTileCell cell) => OnBeforeBrush?.Invoke(cell);
        public void FinishBrush(EditorTileCell cell) => OnAfterBrush?.Invoke(cell);

        public void SetBrushMode()
        {
            _baseProperties = GetComponentsInChildren<BaseProperty>(true);
            _groups = new List<string>();

            IsBrushMode = true;
            OnBrushMode?.Invoke();
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void Init(LocationCell cell, LocationObjectModel objectModel, bool isEditor)
        {
            _baseProperties = GetComponentsInChildren<BaseProperty>(true);

            var metadata = objectModel.metadata;
            Cell = cell;

            InstanceId = objectModel.instanceId;
            if (string.IsNullOrEmpty(InstanceId)) InstanceId = UIDGenerator.Get();

            _groups = objectModel.groups ?? new List<string>();

            // TODO: remove in future!
            _groups.Remove("");

            if (objectModel.metadata.Count > 0)
            {
                // Apply metadata

                // load components properties data
                foreach (var property in _baseProperties)
                {
                    var propertyName = property.GetName();

                    if (metadata.ContainsKey(propertyName))
                        property.SetValue(metadata[propertyName]);
                }
            }

            OnInit(objectModel);
            OnInitDone?.Invoke(isEditor);
        }

        public void AssignCell(LocationCell cell) => Cell = cell;

        public LocationObjectModel SaveObject()
        {
            var result = new LocationObjectModel { objectId = Uid };

            if (Cell != null)
            {
                result.cellX = Cell.X;
                result.cellY = Cell.Y;
            }

            result.instanceId = InstanceId;
            result.groups = _groups;
            result.needPreload = _needPreload;
            
            var metadata = new Hashtable();

            // save component properties
            foreach (var property in _baseProperties)
            {
                if (metadata.Contains(property.GetName()))
                {
                    metadata.Remove(property.GetName());
                }
                metadata.Add(property.GetName(), property.GetValue());
            }

            result.metadata = metadata;
            return result;
        }

        public void RemoveObject() => Cell.RemoveObject(this);

        public void Clone(LocationObject otherObject)
        {
            if (Uid != otherObject.Uid)
            {
                Debug.LogWarning("Warning! Can't clone object with different uids!");
                return;
            }

            _groups = otherObject._groups.ToList();
            foreach (var property in _baseProperties)
            {
                if (otherObject.TryGetProperty(property.GetName(), property.GetType(), out var otherObjectProperty))
                    property.SetValue(otherObjectProperty.GetValue());
                else
                    Debug.LogWarning($"Warning! Can't clone property {property.GetName()}");
            }
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private Coroutine _highlightRoutine;

        public void Highlight()
        {
            if (_highlightRoutine != null)
            {
                StopCoroutine(_highlightRoutine);
                transform.localScale = Vector3.one;
            }

            _highlightRoutine = StartCoroutine(HighlightRoutine());
        }

        private IEnumerator HighlightRoutine()
        {
            while (transform.localScale.x < 1.7f)
            {
                transform.localScale += Vector3.one * (_HIGHLIGHT_SPEED * Time.deltaTime);
                yield return null;
            }

            while (transform.localScale.x > 1f)
            {
                transform.localScale -= Vector3.one * (_HIGHLIGHT_SPEED * Time.deltaTime);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public override string ToString()
        {
            return $"<{Name} on cell ({Cell?.X} {Cell?.Y})>";
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}