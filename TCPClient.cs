using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class TCPClient : MonoBehaviour {  	

	PlayerData playerData = new PlayerData();
	PlayerScript playerScript = new PlayerScript();

	#region private members 	
	private TcpClient socketConnection; 	
	private Thread clientReceiveThread; 	
	#endregion  	
	// Use this for initialization 	
	void Start () {
		ConnectToTcpServer();
		InitializePlayerData();
		// TODO: InitialiseOpponentData();     
	}  	
	// Update is called once per frame
	void Update () {         
		// if (Input.GetKeyDown(KeyCode.Space)) {             
		// 	SendMessage();         
		// }     
	}  	
	/// <summary> 	
	/// Setup socket connection. 	
	/// </summary> 	
	private void ConnectToTcpServer () { 		
		try {  			
			clientReceiveThread = new Thread (new ThreadStart(ListenForData)); 			
			clientReceiveThread.IsBackground = true; 			
			clientReceiveThread.Start();  		
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
			socketConnection = new TcpClient("172.25.107.61", 3333);  			
			Byte[] bytes = new Byte[1024];             
			while (true) { 				
				// Get a stream object for reading 				
				using (NetworkStream stream = socketConnection.GetStream()) { 					
					int length; 					
					// Read incomming stream into byte arrary. 					
					while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) { 						
						var incomingData = new byte[length]; 						
						Array.Copy(bytes, 0, incomingData, 0, length); 						
						// Convert byte array to string message. 	
						string serverMessage = Encoding.ASCII.GetString(incomingData);
						Debug.Log("server message: " + serverMessage);
						playerData = JsonUtility.FromJson<PlayerData>(serverMessage);
						playerScript.UpdatePlayerGameState(playerData);	
					} 				
				} 			
			}         
		}         
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
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
				string clientMessage = isOpponentVisible.ToString(); 				
				// Convert string message to byte array.                 
				byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage); 				
				// Write byte array to socketConnection stream.                 
				stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);                 
				Debug.Log("Client sent his message - should be received by server");             
			}         
		} 		
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		}     
	} 

	private void InitializePlayerData() {
		playerData.setPlayerHP(0);
		playerData.setPlayerSP(0);
		playerData.setPlayerGrenadeAmmo(0);
		playerData.setPlayerGunAmmo(0);
		playerData.setPlayerShieldAmmo(0);
	}
}
