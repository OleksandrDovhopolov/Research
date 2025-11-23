using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class AdaptiveGridSize : MonoBehaviour
{
    public int columns = 3;  // Фиксированное количество колонок
    private GridLayoutGroup grid;

    void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
    }

    void Update()
    {
        AdjustCellSize();
    }

    void AdjustCellSize()
    {
        RectTransform rt = GetComponent<RectTransform>();
        int totalChildren = transform.childCount;

        // Вычисляем количество строк, округляя вверх
        int rows = Mathf.CeilToInt((float)totalChildren / columns);

        float paddingHorizontal = grid.padding.left + grid.padding.right;
        float paddingVertical = grid.padding.top + grid.padding.bottom;
        float spacingHorizontal = grid.spacing.x * (columns - 1);
        float spacingVertical = grid.spacing.y * (rows - 1);

        float cellWidth = (rt.rect.width - paddingHorizontal - spacingHorizontal) / columns;
        float cellHeight = (rt.rect.height - paddingVertical - spacingVertical) / rows;

        // Выбираем минимальный размер, чтобы элементы вписывались в контейнер
        float size = Mathf.Min(cellWidth, cellHeight);
        grid.cellSize = new Vector2(size, size);
    }
}