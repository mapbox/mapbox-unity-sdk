using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections.Generic;
using Prototype.NetworkLobby;

public class GameManager : NetworkBehaviour
{
    static public GameManager s_Instance;

    //this is static so tank can be added even withotu the scene loaded (i.e. from lobby)
    static public List<TankManager> m_Tanks = new List<TankManager>();             // A collection of managers for enabling and disabling different aspects of the tanks.

    public int m_NumRoundsToWin = 5;          // The number of rounds a single player has to win to win the game.
    public float m_StartDelay = 3f;           // The delay between the start of RoundStarting and RoundPlaying phases.
    public float m_EndDelay = 3f;             // The delay between the end of RoundPlaying and RoundEnding phases.
    public Text m_MessageText;                // Reference to the overlay Text to display winning text, etc.
    public GameObject m_TankPrefab;           // Reference to the prefab the players will control.

    public Transform[] m_SpawnPoint;

    [HideInInspector]
    [SyncVar]
    public bool m_GameIsFinished = false;

    //Various UI references to hide the screen between rounds.
    [Space]
    [Header("UI")]
    public CanvasGroup m_FadingScreen;  
    public CanvasGroup m_EndRoundScreen;

    private int m_RoundNumber;                  // Which round the game is currently on.
    private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts.
    private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends.
    private TankManager m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won.
    private TankManager m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won.

    void Awake()
    {
        s_Instance = this;
    }

    [ServerCallback]
    private void Start()
    {
        // Create the delays so they only have to be made once.
        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);

        // Once the tanks have been created and the camera is using them as targets, start the game.
        StartCoroutine(GameLoop());
    }

    /// <summary>
    /// Add a tank from the lobby hook
    /// </summary>
    /// <param name="tank">The actual GameObject instantiated by the lobby, which is a NetworkBehaviour</param>
    /// <param name="playerNum">The number of the player (based on their slot position in the lobby)</param>
    /// <param name="c">The color of the player, choosen in the lobby</param>
    /// <param name="name">The name of the Player, choosen in the lobby</param>
    /// <param name="localID">The localID. e.g. if 2 player are on the same machine this will be 1 & 2</param>
    static public void AddTank(GameObject tank, int playerNum, Color c, string name, int localID)
    {
        TankManager tmp = new TankManager();
        tmp.m_Instance = tank;
        tmp.m_PlayerNumber = playerNum;
        tmp.m_PlayerColor = c;
        tmp.m_PlayerName = name;
        tmp.m_LocalPlayerID = localID;
        tmp.Setup();

        m_Tanks.Add(tmp);
    }

    public void RemoveTank(GameObject tank)
    {
        TankManager toRemove = null;
        foreach (var tmp in m_Tanks)
        {
            if (tmp.m_Instance == tank)
            {
                toRemove = tmp;
                break;
            }
        }

        if (toRemove != null)
            m_Tanks.Remove(toRemove);
    }

    // This is called from start and will run each phase of the game one after another. ONLY ON SERVER (as Start is only called on server)
    private IEnumerator GameLoop()
    {
        while (m_Tanks.Count < 2)
            yield return null;

        //wait to be sure that all are ready to start
        yield return new WaitForSeconds(2.0f);

        // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
        yield return StartCoroutine(RoundStarting());

        // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
        yield return StartCoroutine(RoundPlaying());

        // Once execution has returned here, run the 'RoundEnding' coroutine.
        yield return StartCoroutine(RoundEnding());

        // This code is not run until 'RoundEnding' has finished.  At which point, check if there is a winner of the game.
        if (m_GameWinner != null)
        {// If there is a game winner, wait for certain amount or all player confirmed to start a game again
            m_GameIsFinished = true;
            float leftWaitTime = 15.0f;
            bool allAreReady = false;
            int flooredWaitTime = 15;

            while (leftWaitTime > 0.0f && !allAreReady)
            {
                yield return null;

                allAreReady = true;
                foreach (var tmp in m_Tanks)
                {
                    allAreReady &= tmp.IsReady();
                }

                leftWaitTime -= Time.deltaTime;

                int newFlooredWaitTime = Mathf.FloorToInt(leftWaitTime);

                if (newFlooredWaitTime != flooredWaitTime)
                {
                    flooredWaitTime = newFlooredWaitTime;
                    string message = EndMessage(flooredWaitTime);
                    RpcUpdateMessage(message);
                }
            }

            LobbyManager.s_Singleton.ServerReturnToLobby();
        }
        else
        {
            // If there isn't a winner yet, restart this coroutine so the loop continues.
            // Note that this coroutine doesn't yield.  This means that the current version of the GameLoop will end.
            StartCoroutine(GameLoop());
        }
    }





    private IEnumerator RoundStarting()
    {
		StoreServerTransform ();
		
        //we notify all clients that the round is starting
        RpcRoundStarting();

        // Wait for the specified length of time until yielding control back to the game loop.
        yield return m_StartWait;
    }

	private void StoreServerTransform()
	{
		Transform t = Camera.main.transform.parent;
		SyncPointsHolder sph = t.gameObject.GetComponent<SyncPointsHolder> ();
		if (sph != null) {
			Vector3[] syncPoints = sph.syncPoints;
			RpcSendCameraTransform (t.position, t.rotation, syncPoints [0], syncPoints [1]);
		}
	}


	[ClientRpc]
	void RpcSendCameraTransform(Vector3 pos, Quaternion rot, Vector3 syncPoint0, Vector3 syncPoint1)
	{
		Transform t = Camera.main.transform.parent;
		//t.position = pos;
		//t.rotation = rot;

		SyncPointsHolder sph = t.gameObject.GetComponent<SyncPointsHolder> ();
		if (sph != null) {
			Vector3[] clientsyncPoints = sph.syncPoints;

			Transform coordChangeTransform = t.parent;

			coordChangeTransform.position = clientsyncPoints [0] - syncPoint0;
			Vector3 serverSyncVector = syncPoint1 - syncPoint0;
			Vector3 clientSyncVector = clientsyncPoints [1] - clientsyncPoints [0];

			coordChangeTransform.rotation = Quaternion.FromToRotation (serverSyncVector, clientSyncVector);
		}
	}


    [ClientRpc]
    void RpcRoundStarting()
    {
        // As soon as the round starts reset the tanks and make sure they can't move.
        ResetAllTanks();
        DisableTankControl();

        // Increment the round number and display text showing the players what round it is.
        m_RoundNumber++;
        m_MessageText.text = "ROUND " + m_RoundNumber;


        StartCoroutine(ClientRoundStartingFade());
    }


    private IEnumerator ClientRoundStartingFade()
    {
        float elapsedTime = 0.0f;
        float wait = m_StartDelay - 0.5f;

        yield return null;

        while (elapsedTime < wait)
        {
            if(m_RoundNumber == 1)
                m_FadingScreen.alpha = 1.0f - (elapsedTime / wait);
            else
                m_EndRoundScreen.alpha = 1.0f - (elapsedTime / wait);

            elapsedTime += Time.deltaTime;

            //sometime, synchronization lag behind because of packet drop, so we make sure our tank are reseted
            if (elapsedTime / wait < 0.5f)
                ResetAllTanks();

            yield return null;
        }
    }

    private IEnumerator RoundPlaying()
    {
        //notify clients that the round is now started, they should allow player to move.
        RpcRoundPlaying();

        // While there is not one tank left...
        while (!OneTankLeft())
        {
            // ... return on the next frame.
            yield return null;
        }
    }

    [ClientRpc]
    void RpcRoundPlaying()
    {
        // As soon as the round begins playing let the players control the tanks.
        EnableTankControl();

        // Clear the text from the screen.
        m_MessageText.text = string.Empty;
    }

    private IEnumerator RoundEnding()
    {
        // Clear the winner from the previous round.
        m_RoundWinner = null;

        // See if there is a winner now the round is over.
        m_RoundWinner = GetRoundWinner();

        // If there is a winner, increment their score.
        if (m_RoundWinner != null)
            m_RoundWinner.m_Wins++;

        // Now the winner's score has been incremented, see if someone has one the game.
        m_GameWinner = GetGameWinner();

        RpcUpdateMessage(EndMessage(0));

        //notify client they should disable tank control
        RpcRoundEnding();

        // Wait for the specified length of time until yielding control back to the game loop.
        yield return m_EndWait;
    }

    [ClientRpc]
    private void RpcRoundEnding()
    {
        DisableTankControl();
        StartCoroutine(ClientRoundEndingFade());
    }

    [ClientRpc]
    private void RpcUpdateMessage(string msg)
    {
        m_MessageText.text = msg;
    }

    private IEnumerator ClientRoundEndingFade()
    {
        float elapsedTime = 0.0f;
        float wait = m_EndDelay;
        while (elapsedTime < wait)
        {
            m_EndRoundScreen.alpha = (elapsedTime / wait);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // This is used to check if there is one or fewer tanks remaining and thus the round should end.
    private bool OneTankLeft()
    {
        // Start the count of tanks left at zero.
        int numTanksLeft = 0;

        // Go through all the tanks...
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            // ... and if they are active, increment the counter.
            if (m_Tanks[i].m_TankRenderers.activeSelf)
                numTanksLeft++;
        }

        // If there are one or fewer tanks remaining return true, otherwise return false.
        return numTanksLeft <= 1;
    }


    // This function is to find out if there is a winner of the round.
    // This function is called with the assumption that 1 or fewer tanks are currently active.
    private TankManager GetRoundWinner()
    {
        // Go through all the tanks...
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            // ... and if one of them is active, it is the winner so return it.
            if (m_Tanks[i].m_TankRenderers.activeSelf)
                return m_Tanks[i];
        }

        // If none of the tanks are active it is a draw so return null.
        return null;
    }


    // This function is to find out if there is a winner of the game.
    private TankManager GetGameWinner()
    {
        int maxScore = 0;

        // Go through all the tanks...
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            if(m_Tanks[i].m_Wins > maxScore)
            {
                maxScore = m_Tanks[i].m_Wins;
            }

            // ... and if one of them has enough rounds to win the game, return it.
            if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                return m_Tanks[i];
        }

        //go throught a second time to enable/disable the crown on tanks
        //(note : we don't enter it if the maxScore is 0, as no one is current leader yet!)
        for (int i = 0; i < m_Tanks.Count && maxScore > 0; i++)
        {
            m_Tanks[i].SetLeader(maxScore == m_Tanks[i].m_Wins);
        }

        // If no tanks have enough rounds to win, return null.
        return null;
    }


    // Returns a string of each player's score in their tank's color.
    private string EndMessage(int waitTime)
    {
        // By default, there is no winner of the round so it's a draw.
        string message = "DRAW!";


        // If there is a game winner set the message to say which player has won the game.
        if (m_GameWinner != null)
            message = "<color=#" + ColorUtility.ToHtmlStringRGB(m_GameWinner.m_PlayerColor) + ">"+ m_GameWinner.m_PlayerName + "</color> WINS THE GAME!";
        // If there is a winner, change the message to display 'PLAYER #' in their color and a winning message.
        else if (m_RoundWinner != null)
            message = "<color=#" + ColorUtility.ToHtmlStringRGB(m_RoundWinner.m_PlayerColor) + ">" + m_RoundWinner.m_PlayerName + "</color> WINS THE ROUND!";

        // After either the message of a draw or a winner, add some space before the leader board.
        message += "\n\n";

        // Go through all the tanks and display their scores with their 'PLAYER #' in their color.
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            message += "<color=#" + ColorUtility.ToHtmlStringRGB(m_Tanks[i].m_PlayerColor) + ">" + m_Tanks[i].m_PlayerName + "</color>: " + m_Tanks[i].m_Wins + " WINS " 
                + (m_Tanks[i].IsReady()? "<size=15>READY</size>" : "") + " \n";
        }

        if (m_GameWinner != null)
            message += "\n\n<size=20 > Return to lobby in " + waitTime + "\nPress Fire to get ready</size>";

        return message;
    }


    // This function is used to turn all the tanks back on and reset their positions and properties.
    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            m_Tanks[i].m_SpawnPoint = m_SpawnPoint[m_Tanks[i].m_Setup.m_PlayerNumber];
            m_Tanks[i].Reset();
        }
    }


    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            m_Tanks[i].EnableControl();
        }
    }


    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Count; i++)
        {
            m_Tanks[i].DisableControl();
        }
    }
}
