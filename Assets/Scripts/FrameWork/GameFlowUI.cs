using UnityEngine;
using UnityEngine.UI;

public class GameFlowUI : MonoBehaviour
{
    //  Variables
    //  ---------

    private Text m_gameOverText = null;
    private Text m_winnerText = null;

    //  Functions
    //  ---------

    private void Start()
    {
        m_gameOverText = transform.Find("GameOverText").GetComponent<Text>();
        m_gameOverText.gameObject.SetActive(false);

        m_winnerText = transform.Find("WinnerText").GetComponent<Text>();
        m_winnerText.gameObject.SetActive(false);

        GameServices.GameState.onGameOver += ShowGameResults;
    }

    private void ShowGameResults(ETeam winner)
    {
        m_winnerText.color = GameServices.GetTeamColor(winner);
        m_winnerText.text = "Winner is " + winner.ToString() + " team";

        m_gameOverText.gameObject.SetActive(true);
        m_winnerText.gameObject.SetActive(true);
    }
}
