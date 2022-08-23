using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO; // File modification :P
using System.Text; // StringBuilder my beloved
using System.Diagnostics; // For StopWatch
using Debug = UnityEngine.Debug;
#endif

namespace Tetris.Shapes
{
    /// <summary>
    /// Reads from the "Available Shapes.txt" stored in the resources folder and loads all the shapes with matching names in that text file.
    /// </summary>
    public class ShapeLoader
    {
        //-- File Path Constants
        public const string FILE_NAME = "Available Shapes";
        public const string PREFAB_FOLDER = "Prefabs";
        public const char LINE_BREAK = '\n';

        /// <summary>
        /// Returns the "Available Shapes.txt" <see cref="TextAsset"/> stored in the resources folder.
        /// </summary>
        public static TextAsset Asset
        {
            get
            {
                // Load asset if it's null
                if (_cachedAsset == null)
                {
                    _cachedAsset = Resources.Load<TextAsset>(FILE_NAME);
                }

                return _cachedAsset;
            }
        }
        private static TextAsset _cachedAsset;

        /// <summary>
        /// Returns the text in the <see cref="Asset"/> text file.
        /// </summary>
        public static string Text
        {
            get => Asset.text;
        }

        /// <summary>
        /// The shapes loaded. NOTE: These are PREFABS and loaded directly from <see cref="Resources"/>. <para/>
        /// SO DO NOT MODIFY THE <see cref="GameObject"/> DIRECTLY, INSTEAD INSTANTIATE A NEW SHAPE AND MODIFY THAT. <para/>
        /// See: <see cref="LoadShape"/>
        /// </summary>
        public static GameObject[] Shapes { get; private set; }

        private static int? _cachedShapeLength = null;
        public static int Length
        {
            get
            {
                // Cache for slightly better performance
                if (!_cachedShapeLength.HasValue)
                {
                    _cachedShapeLength = Shapes.Length;
                }

                return _cachedShapeLength.Value;
            }
        }

        /// <summary>
        /// Loads all the shapes into the <see cref="Shapes"/> array by reading <see cref="Text"/>.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static void LoadShapes()
        {
            string[] split = Text.Split(LINE_BREAK, System.StringSplitOptions.RemoveEmptyEntries);
            List<GameObject> tempList = new List<GameObject>(); // Will hold all loaded shapes temporarily

            foreach (string line in split)
            {
                // Ignore comments or empty lines
                if (line.StartsWith("//") || string.IsNullOrEmpty(line))
                {
                    continue;
                }

                // Load object
                GameObject obj = Resources.Load<GameObject>(PREFAB_FOLDER + "/" + line.Trim());

                // Load fail :(
                if (obj == null)
                {
                    continue;
                }

                // Add to temp list
                tempList.Add(obj);
            }

            // Covert to array
            Shapes = tempList.ToArray();
        }

        /// <summary>
        /// Instantiates a new shape with the given index from the <see cref="Shapes"/> array and returns the newly created <see cref="GameObject"/>.
        /// </summary>
        public static GameObject LoadShape(int index)
        {
            return Object.Instantiate(Shapes[index]);
        }

        /// <summary>
        /// Instantiates a new shape with the given index and position from the <see cref="Shapes"/> array and returns the newly created <see cref="GameObject"/>.
        /// </summary>
        public static GameObject LoadShape(int index, Vector3 position)
        {
            return Object.Instantiate(Shapes[index], position, Quaternion.identity);
        }

        /// <summary>
        /// Instantiates a new shape with the given index, position and rotation from the <see cref="Shapes"/> array and returns the newly created <see cref="GameObject"/>.
        /// </summary>
        public static GameObject LoadShape(int index, Vector3 position, Quaternion rotation)
        {
            return Object.Instantiate(Shapes[index], position, rotation);
        }

        /// <summary>
        /// Instantiates a new shape with the given index, position, rotation and parent from the <see cref="Shapes"/> array and returns the newly created <see cref="GameObject"/>.
        /// </summary>
        public static GameObject LoadShape(int index, Vector3 position, Quaternion rotation, Transform parent)
        {
            return Object.Instantiate(Shapes[index], position, rotation, parent);
        }

#if UNITY_EDITOR
        #region Updating "Available Shapes" file automatically
        //-- Prefabs Folder Path Constants
        private static readonly string _prefabsPath = Path.Combine(Application.dataPath, "Resources", PREFAB_FOLDER);
        private static readonly Stopwatch _watch = new Stopwatch();
        private const string FILE_TOP_TEXT =
            @"// NOTE: This file is auto updated every time the unity editor is recompiled.
// DO NOT CHANGE THIS MANUALLY

";

        [InitializeOnLoadMethod]
        private static void UpdateFile()
        {
            StringBuilder builder = new StringBuilder(FILE_TOP_TEXT);
            LoadDirectory(_prefabsPath, ref builder);

            // Search through all the directories in the original directory
            foreach (string directory in Directory.GetDirectories(_prefabsPath))
            {
                LoadDirectory(directory, ref builder);
            }

            File.WriteAllText(Path.Combine(Application.dataPath, "Resources", FILE_NAME + ".txt"), builder.ToString().Trim());
        }

        [MenuItem("Developer/Update Available Shapes")]
        private static void ManualUpdateFile()
        {
            _watch.Start();

            UpdateFile();

            _watch.Stop();

            long time = _watch.ElapsedMilliseconds;
            Debug.Log("Updated all available shapes. It took: " + time + (time == 1 ? "millisecond." : " milliseconds."), Asset);
        }

        private static void LoadDirectory(string directory, ref StringBuilder builder)
        {
            // No directory :(
            if (!Directory.Exists(directory))
            {
                return;
            }

            // Loop through all files in the directory
            foreach (string filePath in Directory.GetFiles(directory))
            {
                // Get the file extension from the file path
                string fileExtension = Path.GetExtension(filePath).ToLower();

                // Ignore files that are not PREFAB
                if (fileExtension != ".prefab")
                {
                    continue;
                }

                string fileName = filePath.Substring(_prefabsPath.Length + 1);

                int lastDotIndex = fileName.LastIndexOf('.');

                fileName = fileName.Substring(0, lastDotIndex);

                builder.Append(fileName);
                builder.Append(LINE_BREAK);
            }
        }
        #endregion
#endif
    }
}