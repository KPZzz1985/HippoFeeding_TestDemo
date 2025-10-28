using UnityEngine;

namespace HippoFeeding.Gameplay.Interaction
{
	/// <summary>
	/// Base component for items that can be picked up, held and thrown by the player.
	/// Keeps rigidbody and collider references and exposes simple hooks.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	public class CarryableItem : MonoBehaviour
	{
		[SerializeField] private Rigidbody itemRigidbody;
		[SerializeField] private Collider[] itemColliders;
		[SerializeField] private bool startAsKinematic = false;

		public Rigidbody Rigidbody => itemRigidbody;
		public bool IsHeld { get; private set; }

		protected virtual void Reset()
		{
			itemRigidbody = GetComponent<Rigidbody>();
			itemColliders = GetComponentsInChildren<Collider>();
		}

		protected virtual void Awake()
		{
			if (itemRigidbody == null)
				itemRigidbody = GetComponent<Rigidbody>();
			if (itemColliders == null || itemColliders.Length == 0)
				itemColliders = GetComponentsInChildren<Collider>();
			itemRigidbody.isKinematic = startAsKinematic;
		}

		public virtual void OnPickedUp()
		{
			IsHeld = true;
			itemRigidbody.isKinematic = true;
		}

		public virtual void OnDropped()
		{
			IsHeld = false;
			itemRigidbody.isKinematic = false;
		}

		public virtual void OnThrown(Vector3 impulse)
		{
			OnDropped();
			itemRigidbody.AddForce(impulse, ForceMode.VelocityChange);
		}
	}
}


