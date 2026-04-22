using UnityEngine;
using UnityEngine.UI;

namespace Utility
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public class GridScaler : MonoBehaviour
    {
        [SerializeField] private Vector2Int gridSize;
        private GridLayoutGroup grid;
        private RectTransform rectTransform;
        private Vector2 lastSize;

        private void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            var size = rectTransform.rect.size;
            if (size == lastSize) return;
            lastSize = size;
            Recalculate();
        }

        public void SetGridSize(Vector2Int size)
        {
            gridSize = size;
            Recalculate();
        }

        private void Recalculate()
        {
            if (gridSize.x <= 0 || gridSize.y <= 0) return;

            var padding = grid.padding;
            var spacing = grid.spacing;

            // Total space consumed by padding and spacing
            float paddingX = padding.left + padding.right;
            float paddingY = padding.top + padding.bottom;
            float spacingX = spacing.x * (gridSize.x - 1);
            float spacingY = spacing.y * (gridSize.y - 1);

            // Remaining space divided by cell count
            float cellWidth  = (rectTransform.rect.width  - paddingX - spacingX) / gridSize.x;
            float cellHeight = (rectTransform.rect.height - paddingY - spacingY) / gridSize.y;

            grid.cellSize = new Vector2(cellWidth, cellHeight);
        }
    }
}
