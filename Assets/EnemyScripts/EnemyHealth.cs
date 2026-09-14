using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    private void Awake()
    {
        //Start the enemy with full health
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        //Reduce the enemy's health by the damage amount
        currentHealth -= damage;

        Debug.Log("Enemy took " + damage + " damage. Health: " + currentHealth);

        //Check if the enemy's health has dropped to zero or below
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy died!");

        Destroy(gameObject);
    }



}
