using UnityEngine;

/// <summary>
/// EconomyManger responsible for spending and adding money from and for the balance.
/// </summary>

public class EconomyManager : MonoBehaviour
{
    public int CurrentMoney => GameManager.Instance.Data.money; // money Balance

    public void AddMoney(int amount) // Function to add money
    {
        GameManager.Instance.Data.money += amount;
        Debug.Log("[Economy] Added " + amount +
                  ". New balance: " + CurrentMoney);
      
    }

    public bool TrySpend(int amount) // Function to spend money
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
