using UnityEngine;
using HippoFeeding.Gameplay.UI;

namespace HippoFeeding.Gameplay.Player
{
	/// <summary>
	/// Trigger area where pressing F grants the player an in-hand food (toggles animator IsFoodHandled and shows the in-hand melon).
	/// Place this at the cart with melons.
	/// </summary>
	public sealed class FoodPickupZone : MonoBehaviour
	{
		[SerializeField] private string playerTag = "Player";
		[SerializeField] private KeyCode interactKey = KeyCode.F;
		[SerializeField] private PlayerHandsController playerHands;
		[SerializeField] private PickupHintUI hintUI;

		private bool playerInside;
        private bool subscribed;

		private void OnTriggerEnter(Collider other)
		{
			if (other.CompareTag(playerTag))
			{
				playerInside = true;
				if (playerHands == null)
					playerHands = other.GetComponentInChildren<PlayerHandsController>();
				if (playerHands != null && !subscribed)
				{
					playerHands.OnFoodHandledChanged += HandleFoodChanged;
					subscribed = true;
				}
				if (hintUI != null)
				{
					if (playerHands != null && playerHands.HasFood)
						hintUI.Hide();
					else
						hintUI.Show();
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (other.CompareTag(playerTag))
			{
				playerInside = false;
				if (hintUI != null) hintUI.Hide();
				if (playerHands != null && subscribed)
				{
					playerHands.OnFoodHandledChanged -= HandleFoodChanged;
					subscribed = false;
				}
			}
		}

		private void Update()
		{
			if (!playerInside || playerHands == null)
				return;
			if (Input.GetKeyDown(interactKey))
			{
				playerHands.SetHasFood(true);
				if (hintUI != null) hintUI.Hide();
			}
		}

        private void HandleFoodChanged(bool hasFood)
        {
            if (hintUI == null) return;
            if (hasFood) hintUI.Hide();
            else if (playerInside) hintUI.Show();
        }
	}
}


