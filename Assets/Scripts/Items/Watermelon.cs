using UnityEngine;
using HippoFeeding.Gameplay.Interaction;

namespace HippoFeeding.Gameplay.Items
{
	/// <summary>
	/// Watermelon item that can swap from whole to sliced prefab for chewing or break on impact.
	/// Very lightweight for prototype purposes.
	/// </summary>
	public sealed class Watermelon : CarryableItem
	{
		[SerializeField] private GameObject wholeVisual;
		[SerializeField] private GameObject slicedVisual;
		[SerializeField] private float breakSpeed = 7f;

		private bool isSliced;

		protected override void Awake()
		{
			base.Awake();
			SetSliced(false);
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (IsHeld)
				return;
			if (collision.relativeVelocity.magnitude >= breakSpeed)
			{
				SetSliced(true);
			}
		}

		public void SetSliced(bool sliced)
		{
			isSliced = sliced;
			if (wholeVisual != null) wholeVisual.SetActive(!sliced);
			if (slicedVisual != null) slicedVisual.SetActive(sliced);
		}
	}
}


