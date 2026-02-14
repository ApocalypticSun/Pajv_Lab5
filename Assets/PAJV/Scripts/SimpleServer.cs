using DarkRift;
using DarkRift.Server;
using DarkRift.Server.Unity;
using System.Collections.Generic;
using UnityEngine;
using PAJV.Net;

namespace PAJV
{
    public class SimpleServer : MonoBehaviour
    {
        [SerializeField] private XmlUnityServer riftServer;

        private struct PlayerData
        {
            public ushort ID;
            public float X, Y, Z;
            public float RotY;
        }

        private readonly Dictionary<ushort, PlayerData> connectedPlayers = new();

        private void Start()
        {
            Application.runInBackground = true;

            var cm = riftServer.Server.ClientManager;
            cm.ClientConnected += HandleClientConnected;
            cm.ClientDisconnected += HandleClientDisconnected;
        }

        private void HandleClientConnected(object sender, ClientConnectedEventArgs args)
        {
            var client = args.Client;

            var newPlayer = new PlayerData
            {
                ID = client.ID,
                X = 0,
                Y = 1,
                Z = 0,
                RotY = 0
            };

            connectedPlayers[client.ID] = newPlayer;

            var clients = riftServer.Server.ClientManager.GetAllClients();

            // Spawn pentru toată lumea (inclusiv clientul nou)
            BroadcastSpawn(clients, newPlayer);

            client.MessageReceived += HandleClientMessageReceived;

            Debug.Log($"[Server] Client {client.ID} connected!");
        }

        private void HandleClientDisconnected(object sender, ClientDisconnectedEventArgs args)
        {
            var client = args.Client;
            client.MessageReceived -= HandleClientMessageReceived;

            connectedPlayers.Remove(client.ID);

            var clients = riftServer.Server.ClientManager.GetAllClients();
            BroadcastDisconnect(clients, client.ID);

            Debug.Log($"[Server] Client {client.ID} disconnected!");
        }

        private void HandleClientMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            var senderClient = args.Client;

            using var message = args.GetMessage();
            using var reader = message.GetReader();

            switch (message.Tag)
            {
                case Tags.Movement:
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    float rotY = reader.ReadSingle();

                    if (connectedPlayers.TryGetValue(senderClient.ID, out var p))
                    {
                        p.X = x;
                        p.Y = y;
                        p.Z = z;
                        p.RotY = rotY;
                        connectedPlayers[senderClient.ID] = p;
                    }

                    var clients = riftServer.Server.ClientManager.GetAllClients();
                    BroadcastMovementExcept(clients, senderClient.ID, x, y, z, rotY);
                    break;
                }

                case Tags.Chat:
                {
                    string text = reader.ReadString();
                    var clients = riftServer.Server.ClientManager.GetAllClients();
                    BroadcastChat(clients, senderClient.ID, text);
                    break;
                }
            }
        }

        // ---------- Packet methods ----------

        private static void BroadcastSpawn(IClient[] allClients, PlayerData p)
        {
            using var writer = DarkRiftWriter.Create();
            writer.Write(p.ID);
            writer.WriteVec3(p.X, p.Y, p.Z);

            using var msg = Message.Create(Tags.Spawn, writer);
            foreach (var c in allClients)
                c.SendMessage(msg, SendMode.Reliable);
        }

        private static void BroadcastDisconnect(IClient[] allClients, ushort disconnectedId)
        {
            using var writer = DarkRiftWriter.Create();
            writer.Write(disconnectedId);

            using var msg = Message.Create(Tags.Disconnect, writer);
            foreach (var c in allClients)
                c.SendMessage(msg, SendMode.Reliable);
        }

        private static void BroadcastMovementExcept(IClient[] allClients, ushort exceptId,
            float x, float y, float z, float rotY)
        {
            using var writer = DarkRiftWriter.Create();
            writer.Write(exceptId);
            writer.WriteVec3(x, y, z);
            writer.Write(rotY);

            using var msg = Message.Create(Tags.Movement, writer);
            foreach (var c in allClients)
                if (c.ID != exceptId)
                    c.SendMessage(msg, SendMode.Unreliable);
        }

        private static void BroadcastChat(IClient[] allClients, ushort senderId, string text)
        {
            using var writer = DarkRiftWriter.Create();
            writer.Write(senderId);
            writer.Write(text);

            using var msg = Message.Create(Tags.Chat, writer);
            foreach (var c in allClients)
                c.SendMessage(msg, SendMode.Reliable);
        }
    }
}
