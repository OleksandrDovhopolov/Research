using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class ObjectsCategory : MonoBehaviour
    {
        [SerializeField] private Text _name;
        [SerializeField] private Transform _subCategoriesContainer;
        [SerializeField] private ObjectsSubCategory _objectsSubCategoryPrefab;
        [SerializeField] private Button _collapseCategoryButton;

        private readonly List<ObjectsSubCategory> _subCategories = new List<ObjectsSubCategory>();

        public string CategoryName { get; private set; }

        public void Init(string categoryName, IEnumerable<LocationObject> objects, TileEditor tileEditor)
        {
            var objectsList = objects.OrderBy(ob => ob.Category).ThenBy(ob => ob.SubCategory).ToList();

            if (string.IsNullOrEmpty(categoryName)) categoryName = "No category";

            _name.text = $"  {categoryName} [+]";
            _subCategoriesContainer.gameObject.SetActive(false);

            CategoryName = categoryName;

            foreach (LocationObject locationObject in objectsList)
            {
                var subCategoryName = locationObject.SubCategory;

                subCategoryName = subCategoryName?.Trim();
                if (subCategoryName == null)
                {
                    subCategoryName = string.Empty;
                }
                
                if (_subCategories.Any(sc => sc.SubCategoryName == subCategoryName)) continue;

                var subCategory = Instantiate(_objectsSubCategoryPrefab, _subCategoriesContainer);
                subCategory.Init(subCategoryName, objectsList.Where(lo => lo.SubCategory == subCategoryName), tileEditor);
                _subCategories.Add(subCategory);
            }

            _collapseCategoryButton.onClick.AddListener(ToggleCategoryCollapse);
        }

        private void ToggleCategoryCollapse()
        {
            if (_subCategoriesContainer.gameObject.activeSelf) Collapse(false);
            else Expand(false);
        }

        public void ApplyFilter(string filterString)
        {
            _subCategories.ForEach(sc => sc.ApplyFilter(filterString));
        }

        public void Expand(bool includeSubCategories)
        {
            _subCategoriesContainer.gameObject.SetActive(true);
            _name.text = $"  {CategoryName} [-]";

            if (includeSubCategories) _subCategories.ForEach(sc => sc.Expand());
        }

        public void Collapse(bool includeSubCategories)
        {
            _subCategoriesContainer.gameObject.SetActive(false);
            _name.text = $"  {CategoryName} [+]";

            if (includeSubCategories) _subCategories.ForEach(sc => sc.Collapse());
        }
    }
}