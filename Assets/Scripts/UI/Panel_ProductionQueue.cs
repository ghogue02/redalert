using UnityEngine;
using UnityEngine.UI;
using System.Text;
using RedAlert.Build;

namespace RedAlert.UI
{
    /// <summary>
    /// Minimal UGUI panel to display a factory BuildQueue with progress and cancel-last button.
    /// Item prefab hierarchy expected:
    /// - Name (Text)
    /// - Cost (Text)
    /// - Progress (Image - Filled Horizontal)
    /// - Cancel (Button) [only enabled on last item]
    /// </summary>
    public class Panel_ProductionQueue : MonoBehaviour
    {
        [SerializeField] private BuildQueue _queue;
        [SerializeField] private Transform _itemsRoot;
        [SerializeField] private GameObject _itemPrefab;

        private readonly StringBuilder _sb = new StringBuilder(64);

        private void Awake()
        {
            if (_queue == null) _queue = FindObjectOfType<BuildQueue>();
        }

        private void Update()
        {
            if (_queue == null || _itemsRoot == null || _itemPrefab == null) return;

            // Rebuild lightweight list (queues are small for Week 2 scope).
            for (int i = _itemsRoot.childCount - 1; i >= 0; i--)
                Destroy(_itemsRoot.GetChild(i).gameObject);

            var list = _queue.Queue;
            for (int i = 0; i < list.Count; i++)
            {
                var go = Instantiate(_itemPrefab, _itemsRoot);
                var nameText = go.transform.Find("Name")?.GetComponent<Text>();
                var costText = go.transform.Find("Cost")?.GetComponent<Text>();
                var fill = go.transform.Find("Progress")?.GetComponent<Image>();
                var cancel = go.transform.Find("Cancel")?.GetComponent<Button>();

                if (nameText != null)
                    nameText.text = string.IsNullOrEmpty(list[i].Id) ? "Item" : list[i].Id;

                if (costText != null)
                    costText.text = list[i].Cost.ToString();

                if (fill != null)
                {
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                    fill.fillOrigin = 0;
                    fill.fillAmount = (i == 0) ? _queue.CurrentProgress01() : 0f;
                }

                if (cancel != null)
                {
                    bool isLast = i == list.Count - 1;
                    cancel.gameObject.SetActive(isLast);
                    if (isLast)
                    {
                        cancel.onClick.RemoveAllListeners();
                        cancel.onClick.AddListener(() => _queue.CancelLast());
                    }
                }
            }
        }
    }
}