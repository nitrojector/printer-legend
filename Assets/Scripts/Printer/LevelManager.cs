using UnityEngine;

namespace Printer
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private PrinterReference reference;

        public PrinterReference Reference => reference;
    }
}
