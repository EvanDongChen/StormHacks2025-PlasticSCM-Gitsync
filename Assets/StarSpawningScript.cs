using UnityEngine;

public class StarSpawningScript : MonoBehaviour
{
    [Header("Star Spawning Settings")]
    public GameObject starPrefab;
    public int numberOfStars = 50;
    
    [Header("Spawn Range")]
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -5f;
    public float maxY = 5f;
    public float zPosition = 0f;
    
    [Header("Spawning Controls")]
    public bool spawnOnStart = true;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (spawnOnStart)
        {
            SpawnStars();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Press Space to spawn stars manually
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnStars();
        }
    }
    
    public void SpawnStars()
    {
        if (starPrefab == null)
        {
            Debug.LogWarning("Star prefab is not assigned!");
            return;
        }
        
        for (int i = 0; i < numberOfStars; i++)
        {
            // Generate random position within the specified ranges
            float randomX = UnityEngine.Random.Range(minX, maxX);
            float randomY = UnityEngine.Random.Range(minY, maxY);
            Vector3 spawnPosition = new Vector3(randomX, randomY, zPosition);
            
            // Spawn the star
            GameObject newStar = Instantiate(starPrefab, spawnPosition, Quaternion.identity);
            
            // Add twinkling component by name (avoids compile-time dependency)
            System.Type twinkleType = System.Type.GetType("StarTwinkle");
            if (twinkleType != null)
            {
                var twinkleComponent = newStar.AddComponent(twinkleType);
                
                // Set spawn ranges for respawning using reflection
                var setRangesMethod = twinkleType.GetMethod("SetSpawnRanges");
                if (setRangesMethod != null)
                {
                    setRangesMethod.Invoke(twinkleComponent, new object[] { minX, maxX, minY, maxY, zPosition });
                }
            }
        }
    }
}
