using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HatSelectionUI : MonoBehaviour
{
    [Header("References")]
    public HatSpawner hatSpawner;
    public Transform buttonParent;
    public Button buttonPrefab;

    [Header("Hats")]
    public GameObject[] hatPrefabs;

    void Start()
    {
        CreateNoneButton();

        foreach (GameObject hat in hatPrefabs)
        {
            CreateHatButton(hat);
        }
    }

    void CreateNoneButton()
    {
        Button btn = Instantiate(buttonPrefab, buttonParent);
        btn.name = "Hat_None_Button";

        TMP_Text text = btn.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = "None";

        btn.onClick.AddListener(() =>
        {
            hatSpawner.ClearHat();
        });
    }

    void CreateHatButton(GameObject hatPrefab)
    {
        Button btn = Instantiate(buttonPrefab, buttonParent);
        btn.name = "Hat_" + hatPrefab.name + "_Button";

        TMP_Text text = btn.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = hatPrefab.name;

        btn.onClick.AddListener(() =>
        {
            hatSpawner.SpawnHat(hatPrefab);
        });
    }
}