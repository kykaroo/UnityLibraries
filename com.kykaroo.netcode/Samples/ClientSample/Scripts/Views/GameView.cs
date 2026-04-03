using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace com.kykaroo.netcode.Samples.ClientSample.Scripts.Views
{
    public class GameView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI tickDiffValueText = default!;
        [SerializeField] private TextMeshProUGUI serverTickValueText = default!;
        [SerializeField] private TextMeshProUGUI clientTickValueText = default!;
        [SerializeField] private TextMeshProUGUI pingValueText = default!;
        [SerializeField] private TextMeshProUGUI fpsValueText = default!;
        [SerializeField] private TextMeshProUGUI playerIdValueText = default!;
        [SerializeField] private TMP_InputField targetFpsInputField = default!;
        [SerializeField] private Slider targetFpsSlider = default!;

        public TextMeshProUGUI TickDiffValueText => tickDiffValueText;
        public TextMeshProUGUI PingValueText => pingValueText;
        public TextMeshProUGUI ServerTickValueText => serverTickValueText;
        public TextMeshProUGUI ClientTickValueText => clientTickValueText;
        public TextMeshProUGUI FpsValueText => fpsValueText;
        public TextMeshProUGUI PlayerIdValueText => playerIdValueText;
        public TMP_InputField TargetFpsInputField => targetFpsInputField;
        public Slider TargetFpsSlider => targetFpsSlider;
    }
}