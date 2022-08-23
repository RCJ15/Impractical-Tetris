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
    /// Will give the user a grid which can be size adjusted. Each grid is a <see cref="bool"/> and can be switched on/off. <para/>
    /// When a grid is true, then that grid will be a square on the collider. The final shape is always centered.
    /// </summary>
    [RequireComponent(typeof(CustomCollider2D))]
    public class TetrisCollider : MonoBehaviour
    {
        [SerializeField] private CustomCollider2D col;

        [SerializeField] private Vector2 gridTiling = Vector2.one;
        [SerializeField] private Vector2Int gridSize = new Vector2Int(3, 3);
        [SerializeField] private bool[] grid;

        [SerializeField] private Vector2 size = Vector2.one;
        [SerializeField] private float angle = 0;
        [SerializeField] private float edgeRadius = 0;

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
                    gridProp.arraySize = sizePropVal.y * sizePropVal.x;
                }

                EditorGUILayout.LabelField("Customize Tetris boxes in the grid");

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
