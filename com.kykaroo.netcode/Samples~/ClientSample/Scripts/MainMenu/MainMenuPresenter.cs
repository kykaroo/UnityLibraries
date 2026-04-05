using UnityEngine;

namespace ClientSample
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

                _ = gameClient.ConnectAsync(ip, port, port);

                mainMenuView.gameObject.SetActive(false);
            });
        }
    }
}