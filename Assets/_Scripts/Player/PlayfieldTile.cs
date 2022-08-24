using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tetris
{
    /// <summary>
    /// A singular tile on the Playfield. Code is spaghetti because I refuse to use <see cref="Animator"/> for the mere fact that it updates whilst inactive which = worse performance :/
    /// </summary>
    public class PlayfieldTile : MonoBehaviour
    {
        //-- Size Scaling
        private static readonly Vector3 _bigSize = new Vector3(1.25f, 1.25f);
        private Vector3 _startSize;

        //-- Sprites
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private SpriteRenderer flashSr;

        [Space]
        [SerializeField] private Color enabledCol = Color.green;
        private Color _disabledCol;

        //-- List of all objects that are touching this tile
        private List<GameObject> _touchingObjects = new List<GameObject>();

        //-- Line
        private PlayfieldLine _line;
        public PlayfieldLine Line { get => _line; set => _line = value; }

        //-- Active state
        private int _shapesTouching;
        public bool Active => _shapesTouching > 0;
        private bool _cachedActiveState;

        private Coroutine _fadeCoroutine;
        private Coroutine _bounceCoroutine;
        private Coroutine _flashCoroutine;

        private void Start()
        {
            _disabledCol = sr.color;
            _startSize = transform.localScale;
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            // Check if the object is the correct layer
            // Layer ID is hard coded but idc
            if (col.gameObject.layer == 3)
            {
                _shapesTouching++;

                UpdateAppearance();

                _touchingObjects.Add(col.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D col)
        {
            // Layer ID is hard coded again
            if (col.gameObject.layer == 3)
            {
                _shapesTouching--;

                UpdateAppearance();

                _touchingObjects.Remove(col.gameObject);
            }
        }

        private void UpdateAppearance()
        {
            // Do not update if appearance is still the same as before
            if (_cachedActiveState == Active)
            {
                return;
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _cachedActiveState = Active;

            if (_cachedActiveState)
            {
                sr.color = enabledCol;
            }
            else
            {
                _fadeCoroutine = StartCoroutine(FadeToColor(_disabledCol, 0.5f));
            }

            _line.TileUpdated(Active);
        }

        public void DestroyTouchingObjects()
        {
            for (int i = 0; i < _touchingObjects.Count; i++)
            {
                GameObject obj = _touchingObjects[i];

                if (obj == null)
                {
                    continue;
                }

                Destroy(obj);
            }
        }

        #region The code that is spaghetti
        private IEnumerator FadeToColor(Color col, float time)
        {
            float timer = time;

            Color startCol = sr.color;

            while (timer > 0)
            {
                sr.color = Color.Lerp(col, startCol, timer / time);

                timer -= Time.deltaTime;

                yield return null;
            }

            sr.color = col;
        }

        public void DestroyTilesAnim()
        {
            StartCoroutine(DestroyTilesAnimCoroutine());
        }

        private IEnumerator DestroyTilesAnimCoroutine()
        {
            DoBounceAnim();

            yield return CoroutineUtility.GetWait(0.1f);

            DestroyTouchingObjects();

            Flash(0.5f);
        }

        /// <summary>
        /// Makes the block do a bouncy bounce animation
        /// </summary>
        public void DoBounceAnim()
        {
            if (_bounceCoroutine != null)
            {
                StopCoroutine(_bounceCoroutine);
            }

            _bounceCoroutine = StartCoroutine(BounceAnim());
        }

        private IEnumerator BounceAnim()
        {
            yield return StartCoroutine(ScaleOverTime(_bigSize, 0.1f));
            yield return StartCoroutine(ScaleOverTime(_startSize, 0.5f, BounceOutEasing));
        }

        private IEnumerator ScaleOverTime(Vector3 targetScale, float time, Func<float, float> method = null)
        {
            float timer = time;

            Vector3 startScale = transform.localScale;

            while (timer > 0)
            {
                float t = 1 - timer / time;
                transform.localScale = Vector3.Lerp(startScale, targetScale, method == null ? t : method.Invoke(t));

                timer -= Time.deltaTime;

                yield return null;
            }

            transform.localScale = targetScale;
        }

        #region Stolen Easing Methods
        /// <summary>
        /// Function stolen from here: https://easings.net/ <para/>
        /// Simply does the famous "Sine In" easing which uses Cosine despite being called Sine In.
        /// </summary>
        public static float SineIn(float x)
        {
            return 1 - Mathf.Cos((x * Mathf.PI) / 2);
        }

        /// <summary>
        /// Function stolen from here: https://easings.net/ <para/>
        /// Simply does the famous "Bounce Out" easing which btw looks absolutely disgusting in code.
        /// </summary>
        private float BounceOutEasing(float x)
        {
            float n1 = 7.5625f;
            float d1 = 2.75f;

            if (x < 1 / d1)
            {
                return n1 * x * x;
            }
            else if (x < 2 / d1)
            {
                return n1 * (x -= 1.5f / d1) * x + 0.75f;
            }
            else if (x < 2.5f / d1)
            {
                return n1 * (x -= 2.25f / d1) * x + 0.9375f;
            }
            else
            {
                return n1 * (x -= 2.625f / d1) * x + 0.984375f;
            }
        }
        #endregion

        public void Flash(float time)
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }

            _flashCoroutine = StartCoroutine(FlashCoroutine(time));
        }

        private IEnumerator FlashCoroutine(float time)
        {
            float timer = time;

            Color startCol = flashSr.color;
            startCol.a = 0;

            Color currentCol = flashSr.color;

            while (timer > 0)
            {
                currentCol.a = timer / time;

                flashSr.color = currentCol;

                timer -= Time.deltaTime;

                yield return null;
            }

            flashSr.color = startCol;
        }
        #endregion
    }
}
