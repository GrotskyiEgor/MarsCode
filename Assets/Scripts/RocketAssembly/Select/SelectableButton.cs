using MarsOutpost;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

//public class SelectableItem : MonoBehaviour
//{
//    #region -==- Indicator -==-

//    [Header("Indicator Objects (Active / Inactive)")]
//    [SerializeField] private GameObject indicatorChoiceTrueObject;
//    [SerializeField] private GameObject indicatorpChoiceFalseObject;

//    #endregion

//    #region -==- Choice Astronaut -==-

//    private TextMeshProUGUI nameText;
//    private TextMeshProUGUI professionText;

//    public AstronautCard assignedData;

//    #endregion

//    private bool isSelected = false;
//    public bool IsSelected => isSelected; // Allows SelectionManager to check the selection status

//    // Use Awake to ensure text references are found before SelectionManager distributes cards in Start()
//    private void Awake()
//    {
//        FindTextComponents();

//        if (indicatorChoiceTrueObject != null)
//        {
//            indicatorChoiceTrueObject.SetActive(false);
//            indicatorpChoiceFalseObject.SetActive(true);
//        }
//    }

//    private void FindTextComponents()
//    {
//        // Find all TextMeshProUGUI components on this object and its children
//        TextMeshProUGUI[] items = GetComponentsInChildren<TextMeshProUGUI>(true);

//        if (items.Length >= 2)
//        {
//            nameText = items[0];
//            professionText = items[1];
//        }
//        else
//        {
//            Debug.LogWarning($"Card {gameObject.name} found fewer than two TextMeshProUGUI components!", this);
//        }
//    }

//    public void InitializeCard(AstronautCard cardData)
//    {
//        assignedData = cardData;

//        // Fallback check: if text references are missing, try finding them again
//        if (nameText == null || professionText == null)
//        {
//            FindTextComponents();
//        }

//        if (nameText != null) nameText.text = $"{assignedData.name} {assignedData.surname}";
//        if (professionText != null) professionText.text = assignedData.profession;
//    }

//    private void OnMouseDown()
//    {
//        // Open the info panel via SelectionManager when clicking the card
//        if (SelectionManager.Instance != null)
//        {
//            SelectionManager.Instance.OpenCardInfo(this);
//        }

//        if (SelectionManager.Instance != null && SelectionManager.Instance.IsInfoOpen)
//            return;

//        if (SelectionManager.Instance != null)
//        {
//            SelectionManager.Instance.OpenCardInfo(this);
//        }
//    }

//    public void SetSelected(bool state)
//    {
//        isSelected = state;

//        if (indicatorChoiceTrueObject != null)
//        {
//            indicatorChoiceTrueObject.SetActive(isSelected);
//            indicatorpChoiceFalseObject.SetActive(!isSelected);
//        }

//        Debug.Log(gameObject.name + " Select: " + isSelected);
//    }
//}

public class SelectableItem : MonoBehaviour
{
    #region -==- Indicator -==-

    [Header("Indicator Objects (Active / Inactive)")]
    [SerializeField] private GameObject indicatorChoiceTrueObject;
    [SerializeField] private GameObject indicatorpChoiceFalseObject;

    #endregion

    #region -==- Choice Astronaut -==-

    private TextMeshProUGUI nameText;
    private TextMeshProUGUI professionText;

    public AstronautCard assignedData;

    #endregion

    private bool isSelected = false;
    public bool IsSelected => isSelected; // Allows SelectionManager to check the selection status

    // Use Awake to ensure text references are found before SelectionManager distributes cards in Start()
    private void Awake()
    {
        FindTextComponents();

        if (indicatorChoiceTrueObject != null)
        {
            indicatorChoiceTrueObject.SetActive(false);
            indicatorpChoiceFalseObject.SetActive(true);
        }
    }

    private void FindTextComponents()
    {
        // Find all TextMeshProUGUI components on this object and its children
        TextMeshProUGUI[] items = GetComponentsInChildren<TextMeshProUGUI>(true);

        if (items.Length >= 2)
        {
            nameText = items[0];
            professionText = items[1];
        }
        else
        {
            Debug.LogWarning($"Card {gameObject.name} found fewer than two TextMeshProUGUI components!", this);
        }
    }

    public void InitializeCard(AstronautCard cardData)
    {
        assignedData = cardData;

        // Fallback check: if text references are missing, try finding them again
        if (nameText == null || professionText == null)
        {
            FindTextComponents();
        }

        if (nameText != null) nameText.text = $"{assignedData.name} {assignedData.surname}";
        if (professionText != null) professionText.text = assignedData.profession;
    }

    private void OnMouseDown()
    {
        // Completely ignore 3D card clicks if the modal info panel is currently active
        if (SelectionManager.Instance != null && SelectionManager.Instance.IsInfoOpen)
            return;

        // Open the info panel via SelectionManager when clicking the card
        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.OpenCardInfo(this);
        }
    }

    private void OnMouseEnter()
    {
        // Ignore hover effects while modal window is open
        if (SelectionManager.Instance != null && SelectionManager.Instance.IsInfoOpen)
            return;

        // Add your hover highlight logic here if needed
    }

    public void SetSelected(bool state)
    {
        isSelected = state;

        if (indicatorChoiceTrueObject != null)
        {
            indicatorChoiceTrueObject.SetActive(isSelected);
            indicatorpChoiceFalseObject.SetActive(!isSelected);
        }

        Debug.Log(gameObject.name + " Select: " + isSelected);
    }
}