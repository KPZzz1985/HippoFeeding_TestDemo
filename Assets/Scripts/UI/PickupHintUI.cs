using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HippoFeeding.Gameplay.UI
{
    /// <summary>
    /// Simple UI hint that shows an animated sprite (sequence) with scale in/out and sounds.
    /// Place on a GameObject with Canvas/CanvasGroup and Image.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PickupHintUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform root;
        [SerializeField] private Image image;

        [Header("Animation")]
        [SerializeField] private List<Sprite> frames = new List<Sprite>();
        [SerializeField] private float framesPerSecond = 8f;
        [SerializeField] private Vector2 scaleRange = new Vector2(0f, 1f);
        [SerializeField] private float showDuration = 0.15f;
        [SerializeField] private float hideDuration = 0.15f;

        [Header("Audio")] 
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip sfxAppear;
        [SerializeField] private float sfxAppearVolume = 0.9f;
        [SerializeField] private AudioClip sfxDisappear;
        [SerializeField] private float sfxDisappearVolume = 0.9f;

        private CanvasGroup canvasGroup;
        private Coroutine showRoutine;
        private Coroutine animRoutine;
        private bool isShown;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (root == null) root = transform as RectTransform;
            if (image == null) image = GetComponentInChildren<Image>(true);
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            SetActiveInstant(false);
        }

        public void Show()
        {
            if (isShown) return;
            isShown = true;
            if (showRoutine != null) StopCoroutine(showRoutine);
            showRoutine = StartCoroutine(ScaleRoutine(scaleRange.x, scaleRange.y, showDuration, playSfx:true));
            if (animRoutine == null) animRoutine = StartCoroutine(FramesLoop());
        }

        public void Hide()
        {
            if (!isShown) return;
            isShown = false;
            if (showRoutine != null) StopCoroutine(showRoutine);
            showRoutine = StartCoroutine(ScaleRoutine(root.localScale.x, scaleRange.x, hideDuration, playSfx:false));
        }

        private void SetActiveInstant(bool active)
        {
            canvasGroup.alpha = active ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            root.localScale = Vector3.one * (active ? scaleRange.y : scaleRange.x);
            if (!active && animRoutine != null)
            {
                StopCoroutine(animRoutine);
                animRoutine = null;
            }
        }

        private IEnumerator ScaleRoutine(float from, float to, float duration, bool playSfx)
        {
            if (playSfx && sfxAppear != null && audioSource != null)
                audioSource.PlayOneShot(sfxAppear, sfxAppearVolume);
            else if (!playSfx && sfxDisappear != null && audioSource != null)
                audioSource.PlayOneShot(sfxDisappear, sfxDisappearVolume);

            canvasGroup.alpha = 1f;
            float t = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float s = Mathf.Lerp(from, to, k);
                root.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            root.localScale = new Vector3(to, to, 1f);
            if (Mathf.Approximately(to, scaleRange.x))
            {
                // fully hidden
                canvasGroup.alpha = 0f;
                if (animRoutine != null)
                {
                    StopCoroutine(animRoutine);
                    animRoutine = null;
                }
            }
        }

        private IEnumerator FramesLoop()
        {
            if (frames == null || frames.Count == 0 || image == null)
                yield break;
            int i = 0;
            float frameTime = 1f / Mathf.Max(1f, framesPerSecond);
            while (isShown)
            {
                image.sprite = frames[i];
                i = (i + 1) % frames.Count;
                yield return new WaitForSeconds(frameTime);
            }
        }
    }
}


