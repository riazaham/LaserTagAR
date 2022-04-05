using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class TCPClient : MonoBehaviour { 

	public Text playerNum;

	#region private members 	
	private TcpClient socketConnection; 	
	private Thread clientReceiveThread; 	
	private string IPAddress = "127.0.0.1";
	private int port = 3333;
	private PlayerData p1, p2;

	#endregion  	

   // =============== Create singleton instance ============== //
    private static TCPClient _instance;
    public static TCPClient tCPClient { get { return _instance; } }
    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else _instance = this;
    }

	// Use this for initialization 	
	void Start () {
		ConnectToTcpServer();   
	}  	
	
	/// <summary> 	
	/// Setup socket connection. 	
	/// </summary> 	
	private void ConnectToTcpServer () { 		
		try {  			
			Debug.Log("Connecting to server " + IPAddress + "/" + port + " ...");
			clientReceiveThread = new Thread (new ThreadStart(ListenForData)); 			
			clientReceiveThread.IsBackground = true; 			
			clientReceiveThread.Start();  	
			Debug.Log("Successfully connected to " + IPAddress + "/" + port);	
		} 		
		catch (Exception e) { 			
			Debug.Log("On client connect exception " + e); 	
		} 	
	}  	
	/// <summary> 	
	/// Runs in background clientReceiveThread; Listens for incomming data. 	
	/// </summary>     
	private void ListenForData() { 		
		try { 			
			socketConnection = new TcpClient(IPAddress, port);  			
			Byte[] bytes = new Byte[1024];             
			string leftOverMessage = ""; 						
			while (true) { 				
				// Get a stream object for reading 				
				using (NetworkStream stream = socketConnection.GetStream()) { 					
					int length; 					
					// Read incomming stream into byte arrary. 					
					while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) {
						var incomingData = new byte[length]; 						
						Array.Copy(bytes, 0, incomingData, 0, length); 						
						// Convert byte array to string message. 	
						string serverMessage = leftOverMessage + Encoding.ASCII.GetString(incomingData);
						Debug.Log("server message: " + serverMessage);
						string[] messages = serverMessage.Split('\n');

						for (int i = 0; i < messages.Length - 1; i++) {
							Debug.unityLogger.Log("CAPSTONE", "message: " + messages[i]);
							Players players = JsonUtility.FromJson<Players>(messages[i]);
							Debug.Log(messages[i]);
							this.p1 = players.p1;
							this.p2 = players.p2;
						}

						leftOverMessage = messages[messages.Length - 1];		
					} 				
				} 			
			}         
		}         
		catch (SocketException socketException) {             
			Debug.unityLogger.Log("CAPTSTONE", "Socket exception: " + socketException);         
		}     
	}  	
	/// <summary> 	
	/// Send message to server using socket connection. 	
	/// </summary> 	
	public void SendMessage(bool isOpponentVisible) {         
		if (socketConnection == null) {             
			return;         
		}  		
		try { 			
			// Get a stream object for writing. 			
			NetworkStream stream = socketConnection.GetStream(); 			
			if (stream.CanWrite) {                 
				string clientMessage = isOpponentVisible.ToString() + "\n"; 				
				// Convert string message to byte array.                 
				byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage); 				
				// Write byte array to socketConnection stream.                 
				stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);                 
				Debug.Log("Message sent: " + clientMessage);             
			}         
		} 		
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}     
	} 

	private void EnqueuePlayerData() {
		if (playerNum.text.Equals("Player 1")) {
			PlayerScript.Player.playersQueue.Enqueue(this.p1);
			OpponentScript.Opponent.playersQueue.Enqueue(this.p2);
		} else {
			PlayerScript.Player.playersQueue.Enqueue(this.p2);
			OpponentScript.Opponent.playersQueue.Enqueue(this.p1);
		}
	}

	public void Reconnect() {
		if (clientReceiveThread.IsAlive) {
			clientReceiveThread.Abort();
		}
		PlayerScript.Player.ResetVariables();
		this.ConnectToTcpServer();
	}
	
	public void ChangePlayer() {
		if (playerNum.text.Equals("Player 1")) playerNum.text = "Player 2";
		else playerNum.text = "Player 1";
		this.EnqueuePlayerData();
	}

	//DEBUG
	public void SendGameState(int newShield) {
        string state = "{\"p1\": {\"hp\": 70, \"action\": \"none\", \"bullets\": 3, \"grenades\": 1, \"shield_time\": 0, \"shield_health\": " + newShield.ToString() + ", \"num_deaths\": 0, \"num_shield\": 1}, \"p2\": {\"hp\": 90, \"action\": \"none\", \"bullets\": 6, \"grenades\": 2, \"shield_time\": 0, \"shield_health\": 30, \"num_deaths\": 0, \"num_shield\": 3}}";
        Players players = JsonUtility.FromJson<Players>(state);
		this.p1 = players.p1;
		this.p2 = players.p2;
		this.EnqueuePlayerData();
	}
}
