using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

namespace HippoFeeding.Gameplay.Items
{
	/// <summary>
	/// Gradually shrinks all child renderers to zero and destroys the root.
	/// Each piece gets a randomized delay to avoid popping all at once.
	/// Requires UniTask.
	/// </summary>
	public sealed class SlicedPiecesAutoDespawn : MonoBehaviour
	{
		[SerializeField] private float minDelay = 0.5f;
		[SerializeField] private float maxDelay = 1.6f;
		[SerializeField] private float shrinkDuration = 0.7f;

		private void Start()
		{
			BeginDespawnSequence().Forget();
		}

		private async UniTaskVoid BeginDespawnSequence()
		{
			var children = GetComponentsInChildren<Transform>();
			foreach (var t in children)
			{
				if (t == transform) continue;
				ShrinkAndDisable(t).Forget();
			}

			float maxTotal = maxDelay + shrinkDuration + 0.1f;
			await UniTask.Delay(TimeSpan.FromSeconds(maxTotal));
			Destroy(gameObject);
		}

		private async UniTaskVoid ShrinkAndDisable(Transform t)
		{
			float delay = UnityEngine.Random.Range(minDelay, maxDelay);
			await UniTask.Delay(TimeSpan.FromSeconds(delay));

			Vector3 start = t.localScale;
			float time = 0f;
			while (time < shrinkDuration)
			{
				time += Time.deltaTime;
				float k = Mathf.Clamp01(time / shrinkDuration);
				t.localScale = Vector3.Lerp(start, Vector3.zero, k);
				await UniTask.Yield();
			}
			t.localScale = Vector3.zero;
		}
	}
}


