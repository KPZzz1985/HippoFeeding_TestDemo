using UnityEngine;

namespace HippoFeeding.Gameplay.Interaction
{
	/// <summary>
	/// Handles raycast pickup/hold/throw from a camera.
	/// Intended to be attached to the player and configured with a hold point.
	/// </summary>
	public sealed class PlayerItemInteractor : MonoBehaviour
	{
		[SerializeField] private Camera playerCamera;
		[SerializeField] private float interactDistance = 3f;
		[SerializeField] private LayerMask interactMask = ~0;
		[SerializeField] private Transform holdAnchor;
		[SerializeField] private float throwVelocity = 9f;
		[SerializeField] private float moveToHoldLerp = 20f;

		private CarryableItem heldItem;

		public CarryableItem HeldItem => heldItem;
		public bool IsHoldingFood => heldItem != null && heldItem.gameObject.CompareTag("Food");

		private void Awake()
		{
			if (playerCamera == null)
				playerCamera = GetComponentInChildren<Camera>();
		}

		private void Update()
		{
			if (Input.GetMouseButtonDown(0))
			{
				if (heldItem != null)
				{
					ThrowHeld();
				}
				else
				{
					TryPickup();
				}
			}

			if (Input.GetMouseButtonDown(1) && heldItem != null)
			{
				DropHeld();
			}

			UpdateHeldFollow();
		}

		private void UpdateHeldFollow()
		{
			if (heldItem == null || holdAnchor == null)
				return;

			Transform t = heldItem.transform;
			t.position = Vector3.Lerp(t.position, holdAnchor.position, moveToHoldLerp * Time.deltaTime);
			t.rotation = Quaternion.Slerp(t.rotation, holdAnchor.rotation, moveToHoldLerp * Time.deltaTime);
		}

		private void TryPickup()
		{
			if (playerCamera == null) return;

			Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
			if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
			{
				CarryableItem item = hit.collider.GetComponentInParent<CarryableItem>();
				if (item != null)
				{
					heldItem = item;
					heldItem.OnPickedUp();
					if (holdAnchor != null)
					{
						heldItem.transform.position = holdAnchor.position;
						heldItem.transform.rotation = holdAnchor.rotation;
					}
				}
			}
		}

		private void DropHeld()
		{
			if (heldItem == null) return;
			heldItem.OnDropped();
			heldItem = null;
		}

		private void ThrowHeld()
		{
			if (heldItem == null || playerCamera == null) return;
			Vector3 impulse = playerCamera.transform.forward * throwVelocity;
			heldItem.OnThrown(impulse);
			heldItem = null;
		}
	}
}


