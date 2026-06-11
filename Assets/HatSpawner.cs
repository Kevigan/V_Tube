using UnityEngine;

public class HatSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public Transform hatSpawnPoint;

    private GameObject currentHat;

    public void SpawnHat(GameObject hatPrefab)
    {
        ClearHat();

        if (hatPrefab == null) return;

        currentHat = Instantiate(hatPrefab, hatSpawnPoint);
        currentHat.transform.localPosition = Vector3.zero;
        currentHat.transform.localRotation = Quaternion.identity;
        currentHat.transform.localScale = Vector3.one;
    }

    public void ClearHat()
    {
        if (currentHat != null)
            Destroy(currentHat);

        currentHat = null;
    }
}