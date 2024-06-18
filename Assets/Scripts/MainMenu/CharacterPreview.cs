using UnityEngine;

public class CharacterPreview : MonoBehaviour
{
    [SerializeField] private GameObject[] characterModels;
    private GameObject wizardDemo;      // Shows the wizard that was chozen

    // Start is called before the first frame update
    void Start()
    {
        GeneratePreview();
    }

    public void SetCharacter(int type)
    {
        PlayerPrefs.SetInt("Character", type);
        GeneratePreview();
    }

    private void GeneratePreview()
    {
        if (wizardDemo != null)
        {
            Destroy(wizardDemo);
        }

        int type = PlayerPrefs.GetInt("Character", 0);
        wizardDemo = Instantiate(characterModels[type], this.transform);
    }
}
