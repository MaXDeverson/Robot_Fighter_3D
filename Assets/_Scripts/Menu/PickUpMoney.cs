using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PickUpMoney : PickUpable
{
	[SerializeField] private int _moneyCount;
	[SerializeField] private Transform _animationObj;
	[SerializeField] private Animation _pickUpMoneyAnimation;
	[SerializeField] private Text _moneyText;
	private bool _isPickUpMoney;
	private void Awake()
	{
		type = PickUpType.Money;
	}
	public void LateUpdate()
    {
		foreach (GameObject player in Players)
		{
			if (player)
			{
				float distanceToPlayer = Vector3.Distance(player.transform.position, transform.position);

				//item in pickup range
				if (distanceToPlayer < pickUpRange && playerinRange == null)
				{
					playerinRange = player;
					OnPickup(playerinRange);
					return;

				}
			}
		}
	}
	public void SetCost(int cost)=> _moneyCount = cost;
    public override void OnPickup(GameObject player)
	{
        if (!_isPickUpMoney)
        {
            _isPickUpMoney = true;
            _animationObj.gameObject.SetActive(true);
            _animationObj.SetParent(null);
            _animationObj.transform.eulerAngles = new Vector3(0, 215, 0);
            _moneyText.text = _moneyCount + "$";
            _pickUpMoneyAnimation.Play();
            Destroy(_animationObj.gameObject, 5);
            GiveMoneyToPlayer(player);
            base.OnPickup(player);
        }
	}

	private void GiveMoneyToPlayer(GameObject player)
	{
		PlayerData.GetPlayerData().AddCash(_moneyCount);
	}
}