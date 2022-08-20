using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tetris.Shapes
{
    /// <summary>
    /// A script that will automatically generate a mesh for a <see cref="MeshRenderer"/> using a 2D collider mesh. <para/>
    /// NOTE: This script will exist in builds, but will have no functionality.
    /// </summary>
    public class GenerateTetrisMesh : MonoBehaviour
    {
#if UNITY_EDITOR
        //-- 

        private void Start()
        {

        }

        private void Update()
        {

        }
#else
        //-- Dead script
#endif
    }
}