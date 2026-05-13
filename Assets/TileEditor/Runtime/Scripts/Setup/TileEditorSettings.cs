using UnityEngine;

namespace Fabros.TileEditor
{
    public enum CameraType { Orthographic2D, Perspective3D }

    [CreateAssetMenu(fileName = "TileEditorSettings", menuName = "TileEditor/Create Settings")]
    public class TileEditorSettings : ScriptableObject
    {
        public float gridSizeX;
        public float gridSizeY;
        public CameraType cameraType;
        public bool allowAddObjectToSameCell;
    }
}
