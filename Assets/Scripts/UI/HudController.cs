using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [SerializeField] private Transform lifeContainer;
    [SerializeField] private Transform bombContainer;
    [SerializeField] private Text powerText;
    [SerializeField] private Text scoreText;
    [SerializeField] private GameObject lifeIconPrefab;
    [SerializeField] private GameObject bombIconPrefab;

    public void Configure(PlayerStats playerStats, Transform lifeRow, Transform bombRow, Text power, Text score, GameObject lifeIcon, GameObject bombIcon)
    {
        stats = playerStats;
        lifeContainer = lifeRow;
        bombContainer = bombRow;
        powerText = power;
        scoreText = score;
        lifeIconPrefab = lifeIcon;
        bombIconPrefab = bombIcon;
    }

    private void OnEnable()
    {
        stats.OnLifeChanged += RefreshLife;
        stats.OnSpellChanged += RefreshSpell;
        stats.OnPowerChanged += RefreshPower;
    }

    private void OnDisable()
    {
        stats.OnLifeChanged -= RefreshLife;
        stats.OnSpellChanged -= RefreshSpell;
        stats.OnPowerChanged -= RefreshPower;
    }

    private void Start()
    {
        scoreText.text = "0";
    }

    private void RefreshLife(int count) => RefreshIcons(lifeContainer, lifeIconPrefab, count);

    private void RefreshSpell(int count) => RefreshIcons(bombContainer, bombIconPrefab, count);

    private void RefreshPower(float value) => powerText.text = value.ToString("0.00");

    private void RefreshIcons(Transform container, GameObject iconPrefab, int count)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
        for (int i = 0; i < count; i++)
        {
            Instantiate(iconPrefab, container, false);
        }
    }
}
