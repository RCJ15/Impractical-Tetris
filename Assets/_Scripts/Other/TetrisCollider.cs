using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tetris.Shapes
{
    /// <summary>
    /// A custom <see cref="Collider2D"/> making usage of a <see cref="CustomCollider2D"/>. <para/>
    /// Will give the user a grid which size can be adjusted. Each grid is a <see cref="bool"/> and can be switched on/off. <para/>
    /// When a tile on the grid is true, then that tile will be added as a square on the collider. The final shape is will always centered.
    /// </summary>
    [RequireComponent(typeof(CustomCollider2D))]
    public class TetrisCollider : MonoBehaviour
    {
        private const int DEFAULT_SIZE_X = 3;
        private const int DEFAULT_SIZE_Y = 3;

        [SerializeField] private CustomCollider2D col;

        [SerializeField] private Vector2 gridTiling = Vector2.one;
        [SerializeField] private Vector2Int gridSize = new Vector2Int(DEFAULT_SIZE_X, DEFAULT_SIZE_Y);
        [SerializeField] private bool[] grid = new bool[DEFAULT_SIZE_Y * DEFAULT_SIZE_X];

        [SerializeField] private Vector2 size = Vector2.one;
        [SerializeField] private float angle = 0;
        [SerializeField] private float edgeRadius = 0;

        #region Public Properties
        /// <summary>
        /// Controls how much space is between each individual tile.
        /// </summary>
        public Vector2 GridTiling { get => gridTiling; set =>gridTiling = value; }
        /// <summary>
        /// The amount of tiles in the grid on both the X and Y axis. Setting this value will modify the grids size dynamically.
        /// </summary>
        public Vector2Int GridSize
        {
            get => gridSize;
            set
            {
                // Return if the size is the same as before
                if (value == gridSize)
                {
                    return;
                }

                value.x = Mathf.Max(1, value.x);
                value.y = Mathf.Max(1, value.y);

                // Create a new grid
                bool[] newGrid = new bool[value.y * value.x];

                Vector2Int loopSize = new Vector2Int(Mathf.Min(gridSize.x, value.x), Mathf.Min(gridSize.y, value.y));

                for (int x = 0; x < loopSize.x; x++)
                {
                    for (int y = 0; y < loopSize.y; y++)
                    {
                        // Fill in the old grids values on the new grid
                        newGrid[x + y * value.x] = grid[x + y * gridSize.x];
                    }
                }

                // Set the size variable and the new grid
                gridSize = value;

                grid = newGrid;
            }
        }

        /// <summary>
        /// How big each tile individually is.
        /// </summary>
        public Vector2 Size { get => size; set => size = value; }
        /// <summary>
        /// The rotation of each tile individually.
        /// </summary>
        public float Angle { get => angle; set => angle = value; }
        /// <summary>
        /// The edge radius of each tile individually
        /// </summary>
        public float EdgeRadius { get => edgeRadius; set => edgeRadius = value; }

        /// <summary>
        /// Get or set a value on the Grid. <para/>
        /// This essentially just calls <see cref="GetValue(int, int)"/> or <see cref="SetValue(int, int, bool)"/>.
        /// </summary>
        public bool this[int x, int y]
        {
            get => GetValue(x, y);
            set => SetValue(x, y, value);
        }
        #endregion

        private PhysicsShapeGroup2D _shapeGroup = new PhysicsShapeGroup2D();

        public void GenerateCollider()
        {
            _shapeGroup.Clear();

            // Create a list of positions
            List<Vector2> gridPositions = new List<Vector2>();
            Vector2 currentPos = Vector2.zero;

            // Loop through all grid booleans and add the positions to the list
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    if (grid[x + y * gridSize.x])
                    {
                        gridPositions.Add(currentPos);
                    }

                    currentPos.y -= gridTiling.y;
                }

                currentPos.y = 0;
                currentPos.x += gridTiling.x;
            }

            // Get the mean of all the enabled positions in order to center the final result
            Vector2 offset = Vector2.zero;

            foreach (Vector2 pos in gridPositions)
            {
                offset += pos;
            }

            offset /= -gridPositions.Count;

            // Add all the shapes with the proper offset
            foreach (Vector2 pos in gridPositions)
            {
                _shapeGroup.AddBox(pos + offset, size, angle, edgeRadius);
            }

            // Add shapes
            col.SetCustomShapes(_shapeGroup);
        }

        #region Customizing Grid
        public void ClearGrid()
        {
            ClearGrid(gridSize);
        }

        public void ClearGrid(Vector2Int newSize)
        {
            newSize.x = Mathf.Max(1, newSize.x);
            newSize.y = Mathf.Max(1, newSize.x);

            grid = new bool[newSize.y * newSize.x];
        }

        public bool GetValue(int x, int y)
        {
            return grid[x + y * gridSize.x];
        }

        public void SetValue(int x, int y, bool val)
        {
            grid[x + y * gridSize.x] = val;
        }
        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateCol();
        }

        private void UpdateCol()
        {
            if (col == null)
            {
                col = GetComponent<CustomCollider2D>();
            }
        }

        [CustomEditor(typeof(TetrisCollider))]
        public class TetrisColliderEditor : Editor
        {
            private const float X_OFFSET = 30;
            private readonly static Vector2 SIZE = new Vector2(30, 30);

            private TetrisCollider _target;

            public void OnEnable()
            {
                _target = (TetrisCollider)target;

                Undo.undoRedoPerformed += OnUndo;
            }

            private void OnDisable()
            {
                Undo.undoRedoPerformed -= OnUndo;
            }

            public override void OnInspectorGUI()
            {
                DrawProp(nameof(gridTiling));
                ApplyModifiedProperties();

                SerializedProperty gridProp = serializedObject.FindProperty(nameof(grid));

                SerializedProperty sizeProp = serializedObject.FindProperty(nameof(gridSize));
                Vector2Int oldGridSize = sizeProp.vector2IntValue;

                EditorGUILayout.PropertyField(sizeProp);

                Vector2Int sizePropVal = sizeProp.vector2IntValue;

                if (sizePropVal.x < 1)
                {
                    sizePropVal.x = 1;
                }
                if (sizePropVal.y < 1)
                {
                    sizePropVal.y = 1;
                }

                sizeProp.vector2IntValue = sizePropVal;

                if (serializedObject.ApplyModifiedProperties())
                {
                    bool[] newGrid = new bool[sizePropVal.y * sizePropVal.x];

                    if (gridProp.arraySize > 0)
                    {
                        Vector2Int loopSize = new Vector2Int(Mathf.Min(oldGridSize.x, sizePropVal.x), Mathf.Min(oldGridSize.y, sizePropVal.y));

                        for (int x = 0; x < loopSize.x; x++)
                        {
                            for (int y = 0; y < loopSize.y; y++)
                            {
                                // Fill in the old grids values on the new grid
                                newGrid[x + y * sizePropVal.x] = gridProp.GetArrayElementAtIndex(x + y * oldGridSize.x).boolValue;
                            }
                        }
                    }

                    gridProp.arraySize = sizePropVal.y * sizePropVal.x;

                    if (gridProp.arraySize > 0)
                    {
                        int length = newGrid.Length;
                        for (int i = 0; i < length; i++)
                        {
                            gridProp.GetArrayElementAtIndex(i).boolValue = newGrid[i];
                        }
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Customize Tetris boxes in the grid");

                EditorGUILayout.BeginVertical("Box");

                Rect rect;

                for (int y = 0; y < sizePropVal.y; y++)
                {
                    rect = EditorGUILayout.GetControlRect();
                    rect.size = SIZE;

                    for (int x = 0; x < sizePropVal.x; x++)
                    {
                        SerializedProperty prop = gridProp.GetArrayElementAtIndex(x + y * sizePropVal.x);

                        prop.boolValue = EditorGUI.ToggleLeft(rect, GUIContent.none, prop.boolValue);

                        rect.x += X_OFFSET;
                    }

                    EditorGUILayout.Space();
                }

                EditorGUILayout.EndVertical();

                //-- Draw other fields
                EditorGUILayout.Space();

                DrawProp(nameof(size));
                DrawProp(nameof(angle));
                DrawProp(nameof(edgeRadius));

                ApplyModifiedProperties();
            }

            private void ApplyModifiedProperties()
            {
                //-- Regenerate collider everytime a value is updated
                if (serializedObject.ApplyModifiedProperties())
                {
                    _target.GenerateCollider();
                }
            }

            private void DrawProp(string name)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
            }

            private void OnUndo()
            {
                _target.GenerateCollider();
            }
        }
#endif
    }
}
