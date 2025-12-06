using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public int CurrentMoney => GameManager.Instance.Data.money;

    public void AddMoney(int amount)
    {
        GameManager.Instance.Data.money += amount;
        Debug.Log("[Economy] Added " + amount +
                  ". New balance: " + CurrentMoney);
      
    }

    public bool TrySpend(int amount)
    {
        if (GameManager.Instance.Data.money < amount)
        {
            Debug.Log("[Economy] Not enough money. Have: " +
                      GameManager.Instance.Data.money +
                      " need: " + amount);
            return false;
        }

        GameManager.Instance.Data.money -= amount;
        Debug.Log("[Economy] Spent " + amount +
                  ". New balance: " + CurrentMoney);
        return true;
    }
}
