using System;
using UnityEngine;

public class GameState : MonoBehaviour
{
    //  Variables
    //  ---------

    public Action<ETeam> onGameOver;
    public ETeam firstTeamColor = ETeam.Neutral;
    public ETeam secondTeamColor = ETeam.Neutral;
    public bool isGameOver { get; private set; }

    #region Team and scoring methods

    // Scores are based on the number of factories per team
    private int[] m_teamScores = new int[2];
    public int[] TeamScores => m_teamScores;

    public ETeam GetOpponent(ETeam team) => team == firstTeamColor ? secondTeamColor : firstTeamColor;
    public void IncreaseTeamScore(ETeam team)
    {
        if (team < ETeam.Neutral)
            ++m_teamScores[(int)team];
    }
    public void DecreaseTeamScore(ETeam team)
    {
        if (team >= ETeam.Neutral)
            return;

        --m_teamScores[(int)team];

        if (m_teamScores[(int)team] <= 0)
            onGameOver(GetOpponent(team));
    }
    #endregion

    #region MonoBehaviour methods
    private void Start()
    {
    }

    #endregion
}
