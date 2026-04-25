using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Utility
{
    public class UIRaycastDebugger : MonoBehaviour
    {
        [SerializeField] private TMP_Text _output;
        
        private readonly List<RaycastResult> _results = new();
        private PointerEventData _pointerEventData;

        private void Awake()
        {
            _pointerEventData = new PointerEventData(EventSystem.current);
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                _output.text = "<color=#888>no mouse device</color>";
                return;
            }
            
            _pointerEventData.position = mouse.position.ReadValue();;
            _results.Clear();
            EventSystem.current.RaycastAll(_pointerEventData, _results);

            if (_results.Count == 0)
            {
                _output.text = "<color=#888>no hits</color>";
                return;
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _results.Count; i++)
            {
                var r = _results[i];
                sb.AppendLine($"<b>[{i}]</b> {r.gameObject.name}");
                sb.AppendLine($"  depth: {r.depth} | sort: {r.sortingOrder} | layer: {r.sortingLayer}");
                sb.AppendLine($"  component: {r.gameObject.GetComponent<Graphic>()?.GetType().Name ?? "?"}");
                sb.AppendLine($"  canvas: {r.gameObject.GetComponentInParent<Canvas>()?.name ?? "?"}");
            }

            _output.text = sb.ToString();
        }
    }
}