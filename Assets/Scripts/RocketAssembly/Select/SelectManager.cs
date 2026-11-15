using MarsOutpost;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Max people count")]
    [SerializeField] private int maxAllowedSelections = 3;
    private List<SelectableItem> selectedItems = new List<SelectableItem>();

    [Header("Info Panel UI")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TextMeshProUGUI infoNameText;
    [SerializeField] private TextMeshProUGUI infoProfessionText;

    [Header("Info Panel Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;
    private TextMeshProUGUI confirmButtonText;

    private SelectableItem currentInspectingItem;

    public static SelectManager Instance { get; private set; }

    // Allows other scripts to check if the modal window currently holds focus
    public bool IsInfoOpen => infoPanel != null && infoPanel.activeSelf;

    #region -==- DATA -==-
    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void SetupUIButtons()
    {
        // Automatically bind confirm button click and find its text component
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            confirmButtonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Automatically bind close button click
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseCardInfo);
        }
    }

    private void Start()
    {
        if (infoPanel != null) infoPanel.SetActive(false); // Hide info panel on start
        DistributeUniqueCards();
    }

    private void Update()
    {
        // Close info panel on Escape key
        if (IsInfoOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCardInfo();
        }
    }

    private void DistributeUniqueCards()
    {
        SelectableItem[] cards = FindObjectsByType<SelectableItem>();

        int requiredMale = 3;
        int requiredFemale = 2;
        int allRequired = requiredMale + requiredFemale;

        if (cards.Length < allRequired)
        {
            Debug.LogWarning("Not enough cards in the scene!");
            return;
        }

        // Получаем копию списка астронавтов
        List<AstronautCard> astronauts =
            new List<AstronautCard>(AstronautPresets.DefaultAstronauts);

        // Разделяем по полу
        List<AstronautCard> males = astronauts.FindAll(x => x.sex == "Male");
        List<AstronautCard> females = astronauts.FindAll(x => x.sex == "Female");

        if (males.Count < requiredMale || females.Count < requiredFemale)
        {
            Debug.LogWarning("Not enough astronauts of required gender!");
            return;
        }

        // Перемешиваем списки
        ShuffleAstronauts(males);
        ShuffleAstronauts(females);
        ShuffleList(new List<SelectableItem>(cards));

        // Выбираем нужное количество
        List<AstronautCard> selectedAstronauts = new List<AstronautCard>();

        selectedAstronauts.AddRange(males.GetRange(0, requiredMale));
        selectedAstronauts.AddRange(females.GetRange(0, requiredFemale));

        // Перемешиваем итоговый список
        ShuffleAstronauts(selectedAstronauts);

        // Перемешиваем карточки
        List<SelectableItem> shuffledCards = new List<SelectableItem>(cards);
        ShuffleList(shuffledCards);

        // Распределяем данные
        for (int i = 0; i < allRequired; i++)
        {
            shuffledCards[i].InitializeCard(selectedAstronauts[i]);
        }
    }

    private void ShuffleAstronauts(List<AstronautCard> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            AstronautCard temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Helper method to shuffle any list randomly
    private void ShuffleList(List<SelectableItem> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            SelectableItem temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Open info panel and display basic astronaut info
    public void OpenCardInfo(SelectableItem item)
    {
        currentInspectingItem = item;
        AstronautCard data = item.assignedData;

        if (infoNameText != null)
            infoNameText.text = $"{data.name} {data.surname}";

        if (infoProfessionText != null)
            infoProfessionText.text = data.profession;

        SelectableItem[] allCards = FindObjectsByType<SelectableItem>();
        foreach (var card in allCards)
        {
            Collider col = card.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        // Update confirm button label based on current selection state
        if (confirmButtonText != null)
            confirmButtonText.text = item.IsSelected ? "Remove" : "Select";

        if (infoPanel != null)
            infoPanel.SetActive(true);
    }

    public void OnConfirmButtonClicked()
    {
        if (currentInspectingItem != null)
        {
            TryToggleSelection(currentInspectingItem);
        }
        CloseCardInfo();
    }

    public void CloseCardInfo()
    {
        currentInspectingItem = null;

        SelectableItem[] allCards = FindObjectsByType<SelectableItem>();

        foreach (var card in allCards)
        {
            Collider col = card.GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        if (infoPanel != null) infoPanel.SetActive(false);

    }

    public bool TryToggleSelection(SelectableItem item)
    {
        if (selectedItems.Contains(item))
        {
            selectedItems.Remove(item);
            item.SetSelected(false);
            return true;
        }
        else
        {
            if (selectedItems.Count >= maxAllowedSelections)
            {
                Debug.Log("Max select count: " + maxAllowedSelections);
                return false;
            }

            selectedItems.Add(item);
            item.SetSelected(true);
            return true;
        }
    }

    public int[] GetIdSelectedCards()
    {
        int[] res = new int[maxAllowedSelections];
        for(int i = 0; i < maxAllowedSelections; i++)
        {
            res[i] = selectedItems[i].assignedData.id;
        }
        return res;
    }

    public bool IsMaxAllowedCardSelect() => selectedItems.Count == maxAllowedSelections;
}