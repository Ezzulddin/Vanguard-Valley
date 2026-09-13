using NUnit.Framework.Internal;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    private void Awake()
    {
        //Beging with max health
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log("Player took" + damage + " damage. Health: " + currentHealth);

        if ( currentHealth <= 0f)
        {
            Die();
        }

    }

    private void Die()
    {
        Debug.Log("Player died!");
        // Implement player death logic here (e.g., respawn, game over, etc.)
    }


}
