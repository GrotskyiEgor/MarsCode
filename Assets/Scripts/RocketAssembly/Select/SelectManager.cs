using MarsOutpost;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
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

    public static SelectionManager Instance { get; private set; }

    // Allows other scripts to check if the modal window currently holds focus
    public bool IsInfoOpen => infoPanel != null && infoPanel.activeSelf;

    #region -==- DATA -==-

    private List<AstronautCard> allAstronauts = new List<AstronautCard>
    {
        new AstronautCard { id = 1, module = null, taskNumber = 0, name = "Alex", surname = "Morgan", profession = "Commander" },
        new AstronautCard { id = 2, module = null, taskNumber = 0, name = "Sarah", surname = "Williams", profession = "Pilot" },
        new AstronautCard { id = 3, module = null, taskNumber = 0, name = "Daniel", surname = "Anderson", profession = "Engineer" },
        new AstronautCard { id = 4, module = null, taskNumber = 0, name = "Emily", surname = "Carter", profession = "Scientist" },
        new AstronautCard { id = 5, module = null, taskNumber = 0, name = "Michael", surname = "Bennett", profession = "Doctor" },
        new AstronautCard { id = 6, module = null, taskNumber = 0, name = "Olivia", surname = "Parker", profession = "Biologist" },
        new AstronautCard { id = 7, module = null, taskNumber = 0, name = "James", surname = "Wilson", profession = "Mechanic" },
        new AstronautCard { id = 8, module = null, taskNumber = 0, name = "Sophia", surname = "Turner", profession = "Geologist" },
        new AstronautCard { id = 9, module = null, taskNumber = 0, name = "William", surname = "Harris", profession = "Technician" },
        new AstronautCard { id = 10, module = null, taskNumber = 0, name = "Isabella", surname = "Scott", profession = "Astronomer" },
        new AstronautCard { id = 11, module = null, taskNumber = 0, name = "Ethan", surname = "Brooks", profession = "Engineer" },
        new AstronautCard { id = 12, module = null, taskNumber = 0, name = "Mia", surname = "Collins", profession = "Doctor" },
        new AstronautCard { id = 13, module = null, taskNumber = 0, name = "Noah", surname = "Reed", profession = "Pilot" },
        new AstronautCard { id = 14, module = null, taskNumber = 0, name = "Ava", surname = "Mitchell", profession = "Botanist" },
        new AstronautCard { id = 15, module = null, taskNumber = 0, name = "Lucas", surname = "Foster", profession = "Physicist" },
        new AstronautCard { id = 16, module = null, taskNumber = 0, name = "Charlotte", surname = "Hayes", profession = "Psychologist" },
        new AstronautCard { id = 17, module = null, taskNumber = 0, name = "Benjamin", surname = "Cooper", profession = "Mechanic" },
        new AstronautCard { id = 18, module = null, taskNumber = 0, name = "Amelia", surname = "Ward", profession = "Chemist" },
        new AstronautCard { id = 19, module = null, taskNumber = 0, name = "Henry", surname = "Peterson", profession = "Geologist" },
        new AstronautCard { id = 20, module = null, taskNumber = 0, name = "Grace", surname = "Murphy", profession = "Biologist" },
        new AstronautCard { id = 21, module = null, taskNumber = 0, name = "Jack", surname = "Richardson", profession = "Technician" },
        new AstronautCard { id = 22, module = null, taskNumber = 0, name = "Ella", surname = "Bailey", profession = "Scientist" },
        new AstronautCard { id = 23, module = null, taskNumber = 0, name = "Ryan", surname = "Wood", profession = "Commander" },
        new AstronautCard { id = 24, module = null, taskNumber = 0, name = "Lily", surname = "Bennett", profession = "Doctor" },
        new AstronautCard { id = 25, module = null, taskNumber = 0, name = "David", surname = "Ross", profession = "Engineer" }
    };
    #endregion

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        SetupUIButtons();
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

        if (cards.Length > allAstronauts.Count)
        {
            Debug.LogWarning("Cards > peoples in list");
            return;
        }

        // Shuffle list
        for (int i = 0; i < allAstronauts.Count; i++)
        {
            AstronautCard temp = allAstronauts[i];
            int randomIndex = Random.Range(i, allAstronauts.Count);
            allAstronauts[i] = allAstronauts[randomIndex];
            allAstronauts[randomIndex] = temp;
        }

        // Apply data to scene cards
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].InitializeCard(allAstronauts[i]);
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
}