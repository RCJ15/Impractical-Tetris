using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tetris
{
    /// <summary>
    /// An entire line of <see cref="PlayfieldTile"/>. Will delete all Tetrominos touching the tiles if ALL the tiles in this line are activated.
    /// </summary>
    public class PlayfieldLine : MonoBehaviour
    {
        private PlayfieldTile[] _tiles;

        private int _tileCount;
        private int _currentTileCount;

        public bool Active => _currentTileCount >= _tileCount;

        private void Start()
        {
            // Set tile related variables
            _tiles = GetComponentsInChildren<PlayfieldTile>();
            _tileCount = _tiles.Length;

            // Set the line variable for each tile
            foreach (PlayfieldTile tile in _tiles)
            {
                tile.Line = this;
            }
        }

        public void ClearLine()
        {
            StartCoroutine(ClearLineAnim());
        }

        private IEnumerator ClearLineAnim()
        {
            foreach (PlayfieldTile tile in _tiles)
            {
                tile.DestroyTilesAnim();

                yield return CoroutineUtility.GetWait(0.05f);
            }
        }

        public void TileUpdated(bool active)
        {
            if (active)
            {
                _currentTileCount++;
            }
            else
            {
                _currentTileCount--;
            }
        }
    }
}
