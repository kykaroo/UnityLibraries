using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NetcodePackage.Samples.Example.Scripts.MainMenu
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField ipInputField = default!;
        [SerializeField] private TMP_InputField portInputField = default!;
        [SerializeField] private Button joinButton = default!;
        [SerializeField] private Button quitButton = default!;
        public Button QuitButton => quitButton;
        public TMP_InputField IpInputField => ipInputField;
        public TMP_InputField PortInputField => portInputField;
        public Button JoinButton => joinButton;
    }
}