using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NetcodePackage.Samples.Example.Scripts.MainMenu
{
    public class MainMenuPresenter
    {
        public MainMenuPresenter(MainMenuView mainMenuView, GameClient gameClient)
        {
            mainMenuView.QuitButton.onClick.AddListener(Application.Quit);

            mainMenuView.JoinButton.onClick.AddListener(() =>
            {
                var ip = mainMenuView.IpInputField.text;
                var port = int.Parse(mainMenuView.PortInputField.text);

                gameClient.ConnectAsync(ip, port, port).Forget();

                mainMenuView.gameObject.SetActive(false);
            });
        }
    }
}