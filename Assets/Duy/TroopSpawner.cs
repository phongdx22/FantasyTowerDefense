using System.Collections;
using System.Collections.Generic;
using TDTK;
using UnityEngine;

public class TroopSpawner : MonoBehaviour
{
    public GameObject troopPrefab;          // The prefab for the troop to spawn
    public List<Transform> spawnPoints;     // List of spawn points
    public float spawnDelay = 0.5f;         // Delay between spawns
    public float amountToSpawn = 3f;         // No of troops to spawn

    private Dictionary<Transform, GameObject> occupiedSpawnPoints = new Dictionary<Transform, GameObject>(); // Track which spawn points are occupied
    private int nextSpawnIndex = 0;         // Index for cycling through spawn points

    public void StartSpawning()
    {
        StartCoroutine(SpawnInitialTroops());
    }

    private IEnumerator SpawnInitialTroops()
    {
        yield return new WaitForSeconds(spawnDelay);
        while (/*occupiedSpawnPoints.Count < spawnPoints.Count*/ amountToSpawn > 0)
        {
            SpawnTroop();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    public void SpawnTroop()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points assigned!");
            return;
        }

        // Find the next available spawn point
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform spawnPoint = spawnPoints[nextSpawnIndex];
            if (!occupiedSpawnPoints.ContainsKey(spawnPoint)) // Check if the spawn point is available
            {
                // Spawn the troop at the available spawn point
                GameObject troop = Instantiate(troopPrefab, spawnPoint.position, spawnPoint.rotation);
                TroopAI troopAI = troop.GetComponent<TroopAI>();
                troopAI.SetSpawner(this, spawnPoint); // Pass the spawner and spawn point to the troop

                // Mark the spawn point as occupied
                occupiedSpawnPoints[spawnPoint] = troop;
                amountToSpawn = amountToSpawn - 1;

                // Update the spawn index to the next one in the list
                nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Count;

                break;
            }

            // Move to the next spawn point in a cyclic manner
            nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Count;
        }
    }

    public void OnTroopDestroyed(GameObject troop, Transform spawnPoint)
    {
        // Free up the spawn point when a troop is destroyed
        if (occupiedSpawnPoints.ContainsKey(spawnPoint) && occupiedSpawnPoints[spawnPoint] == troop)
        {
            occupiedSpawnPoints.Remove(spawnPoint);
        }

        // Start respawning a new troop after a delay
        StartCoroutine(RespawnTroop());
    }

    private IEnumerator RespawnTroop()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnTroop();
    }
}
