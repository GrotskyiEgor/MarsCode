using UnityEngine;
using UnityEngine.UI;

namespace MarsOutpost
{
    public class UIButtonSound : MonoBehaviour
    {
        private void Start()
        {
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                btn.onClick.AddListener(() => GameManager.Instance?.PlayButtonClick());
            }
        }
    }
}