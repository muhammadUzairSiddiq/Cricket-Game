using System.Collections.Generic;
using UnityEngine;
using CricketGame;

/// <summary>
/// Defines per-prefab bowling capabilities and defaults.
/// Attach this to each bowler prefab and configure allowed delivery types.
/// </summary>
public class BowlerProfile : MonoBehaviour
{
	[Header("Delivery Abilities")]
	[SerializeField] private List<DeliveryType> allowedDeliveryTypes = new List<DeliveryType> { DeliveryType.Flat };
	[SerializeField] private DeliveryType defaultDeliveryType = DeliveryType.Flat;

	[Header("Hotkey Mapping (1/2/3)")]
	[SerializeField] private DeliveryType hotkey1 = DeliveryType.Flat;
	[SerializeField] private DeliveryType hotkey2 = DeliveryType.Flat;
	[SerializeField] private DeliveryType hotkey3 = DeliveryType.Flat;


	[Header("Scene References")]
	[SerializeField] private BowlingController bowlingController;

	void Awake()
	{
		if (bowlingController == null)
		{
			bowlingController = FindObjectOfType<BowlingController>();
		}
	}

	void OnEnable()
	{
		if (bowlingController == null)
		{
			bowlingController = FindObjectOfType<BowlingController>();
		}
	}

	void Update()
	{
		if (bowlingController == null) return;

		// Delivery type hotkeys (1/2/3)
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			if (TryGetHotkeyDelivery(1, out DeliveryType t1))
			{
				bowlingController.SetDeliveryType(t1);
			}
		}
		else if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			if (TryGetHotkeyDelivery(2, out DeliveryType t2))
			{
				bowlingController.SetDeliveryType(t2);
			}
		}
		else if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			if (TryGetHotkeyDelivery(3, out DeliveryType t3))
			{
				bowlingController.SetDeliveryType(t3);
			}
		}
		
		// TAB key to switch spawn positions
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			if (bowlingController != null)
			{
				bowlingController.SwitchCurrentBowlerSpawnPosition();
			}
		}
	}

	/// <summary>
	/// Returns the configured allowed delivery types for this bowler prefab.
	/// </summary>
	public IReadOnlyList<DeliveryType> GetAllowedDeliveryTypes()
	{
		return allowedDeliveryTypes;
	}

	/// <summary>
	/// Returns the default delivery type for this bowler prefab.
	/// </summary>
	public DeliveryType GetDefaultDeliveryType()
	{
		return defaultDeliveryType;
	}

	/// <summary>
	/// Returns the delivery type mapped to number hotkeys.
	/// </summary>
	public bool TryGetHotkeyDelivery(int index1Based, out DeliveryType type)
	{
		switch (index1Based)
		{
			case 1: type = hotkey1; return true;
			case 2: type = hotkey2; return true;
			case 3: type = hotkey3; return true;
			default: type = default; return false;
		}
	}

}


