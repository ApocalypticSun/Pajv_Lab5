using System;
using System.Collections.Generic;
using System.Net;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using UnityEngine;
using TMPro;
using PAJV.Net;

namespace PAJV
{
    public class SimpleClient : MonoBehaviour
    {
        [Header("Networking")]
        [SerializeField] private UnityClient riftClient;
        [SerializeField] private string ipAddress = "127.0.0.1";

        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;

        [Header("Chat References")]
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private TextMeshProUGUI chatHistoryText;

        private readonly Dictionary<ushort, GameObject> spawnedPlayers = new();
        private readonly List<string> chatMessages = new();

        private PlayerController localPlayerController;

        private void Start()
        {
            riftClient.ConnectInBackground(
                IPAddress.Parse(ipAddress),
                4296,
                4297,
                true,
                OnConnected
            );

            if (chatInput != null)
                chatInput.onSubmit.AddListener(SendChatMessage);
        }

        private void OnDestroy()
        {
            if (chatInput != null)
                chatInput.onSubmit.RemoveListener(SendChatMessage);

            if (riftClient != null)
                riftClient.MessageReceived -= HandleMessage;
        }

        private void Update()
        {
            if (localPlayerController != null && chatInput != null)
                localPlayerController.InputDisabled = chatInput.isFocused;
        }

        private void OnConnected(Exception e)
        {
            if (riftClient.ConnectionState == ConnectionState.Connected)
            {
                Debug.Log("Connected to server!");
                riftClient.MessageReceived += HandleMessage;
            }
            else if (e != null)
            {
                Debug.LogError($"Failed to connect: {e}");
            }
        }

        private void SendChatMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            using var writer = DarkRiftWriter.Create();
            writer.Write(text);

            using var msg = Message.Create(Tags.Chat, writer);
            riftClient.SendMessage(msg, SendMode.Reliable);

            chatInput.text = "";
            chatInput.ActivateInputField();
        }

        private void HandleMessage(object sender, MessageReceivedEventArgs args)
        {
            using var message = args.GetMessage();
            using var reader = message.GetReader();

            switch (message.Tag)
            {
                case Tags.Spawn:
                    HandleSpawn(reader);
                    break;

                case Tags.Movement:
                    HandleMovement(reader);
                    break;

                case Tags.Chat:
                    HandleChat(reader);
                    break;

                case Tags.Disconnect:
                    HandleDisconnect(reader);
                    break;
            }
        }

        private void HandleSpawn(DarkRiftReader reader)
        {
            ushort id = reader.ReadUInt16();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();

            if (spawnedPlayers.ContainsKey(id))
                return;

            bool isLocal = (id == riftClient.ID);

            var obj = CreatePlayerObject(id, new Vector3(x, y, z), isLocal);
            spawnedPlayers[id] = obj;
        }

        private GameObject CreatePlayerObject(ushort id, Vector3 position, bool isLocal)
        {
            GameObject obj = Instantiate(playerPrefab);
            obj.name = $"Player_{id}";
            obj.transform.position = position;

            if (isLocal)
            {
                localPlayerController = obj.AddComponent<PlayerController>();
                localPlayerController.Initialize(riftClient);
            }

            Debug.Log($"Spawned Player {id} (local={isLocal})");
            return obj;
        }

        private void HandleMovement(DarkRiftReader reader)
        {
            ushort id = reader.ReadUInt16();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float rotY = reader.ReadSingle();

            if (spawnedPlayers.TryGetValue(id, out var obj))
            {
                obj.transform.position = new Vector3(x, y, z);
                obj.transform.rotation = Quaternion.Euler(0, rotY, 0);
            }
        }

        private void HandleChat(DarkRiftReader reader)
        {
            ushort senderId = reader.ReadUInt16();
            string text = reader.ReadString();
            UpdateChatUI($"Player {senderId}: {text}");
        }

        private void HandleDisconnect(DarkRiftReader reader)
        {
            ushort id = reader.ReadUInt16();

            if (spawnedPlayers.TryGetValue(id, out var obj))
            {
                Destroy(obj);
                spawnedPlayers.Remove(id);
            }
        }

        private void UpdateChatUI(string newMsg)
        {
            chatMessages.Add(newMsg);

            const int maxLines = 2;
            if (chatMessages.Count > maxLines)
                chatMessages.RemoveAt(0);

            if (chatHistoryText != null)
                chatHistoryText.text = string.Join("\n", chatMessages);
        }
    }
}
