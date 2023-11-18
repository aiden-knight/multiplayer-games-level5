using System.Data.Common;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Multiplayer_Games_Programming_Packet_Library
{
	public enum PacketType
	{
		EMPTY,
		MESSAGE,
		POSITION,
		LOGIN,
        UDP_LOGIN,
        GAME_READY,
        PLAY,
        JOIN_LOBBY,
        BALL,
        ENCRYPTED,
	}

    public class EncryptedPacket : Packet
    {
        public byte[] encryptedPacket;
        public EncryptedPacket()
        {
            Type = PacketType.ENCRYPTED;
            encryptedPacket = Array.Empty<byte>();
        }

        public EncryptedPacket(byte[] encryptedPacket)
        {
            Type = PacketType.ENCRYPTED;
            this.encryptedPacket = encryptedPacket;
        }
    }

    public class MessagePacket : Packet
    {
        public string message;
        public MessagePacket()
        {
            Type = PacketType.MESSAGE;
        }

        public MessagePacket(string message)
        {
            Type = PacketType.MESSAGE;
            this.message = message;
        }
    }

    public class PositionPacket : Packet
    {
        public float x, y;
        public PositionPacket()
        {
            Type = PacketType.POSITION;
        }

        public PositionPacket(float x, float y)
        {
            Type = PacketType.POSITION;
            this.x = x;
            this.y = y;
        }
    }
    public class LoginPacket : Packet
    {
        public int ID;
        public RSAParameters publicKey;
        public LoginPacket()
        {
            Type = PacketType.LOGIN;
        }

        public LoginPacket(int ID, RSAParameters publicKey)
        {
            Type = PacketType.LOGIN;
            this.ID = ID;
            this.publicKey = publicKey;
        }
    }

    public class UdpLoginPacket : Packet
    {
        public int ID;
        public UdpLoginPacket()
        {
            Type = PacketType.UDP_LOGIN;
        }
        public UdpLoginPacket(int ID)
        {
            Type = PacketType.UDP_LOGIN;
            this.ID = ID;
        }
    }

    public class BallPacket : Packet
    {
        public float x, y;
        public float vX, vY; // velocity

        public BallPacket()
        {
            Type = PacketType.BALL;
        }

        public BallPacket(float x, float y, float vX, float vY)
        {
            Type = PacketType.BALL;
            this.x = x; this.y = y;
            this.vX = vX; this.vY = vY;
        }
    }

    public class GameReadyPacket : Packet
    {
        public int playerID;
        public GameReadyPacket()
        {
            Type = PacketType.GAME_READY;
        }
        public GameReadyPacket(int playerID)
        {
            Type = PacketType.GAME_READY;
            this.playerID = playerID;
        }
    }

    public class PlayPacket : Packet
    {
        public PlayPacket()
        {
            Type = PacketType.PLAY;
        }
    }
    public class JoinLobbyPacket : Packet
    {
        public JoinLobbyPacket()
        {
            Type = PacketType.JOIN_LOBBY;
        }
    }
    public class EmptyPacket : Packet
    {
        public EmptyPacket()
        {
            Type = PacketType.EMPTY;
        }
    }

    public class PacketConverter : JsonConverter<Packet>
    {
        public override Packet? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("Type", out var type))
                {
                    byte typeByte = type.GetByte();
                    switch (typeByte)
                    {
                        case (byte)PacketType.EMPTY:
                            return JsonSerializer.Deserialize<EmptyPacket>(root.GetRawText(), options);
                        case (byte)PacketType.BALL:
                            return JsonSerializer.Deserialize<BallPacket>(root.GetRawText(), options);
                        case (byte)PacketType.MESSAGE:
                            return JsonSerializer.Deserialize<MessagePacket>(root.GetRawText(), options);
                        case (byte)PacketType.POSITION:
                            return JsonSerializer.Deserialize<PositionPacket>(root.GetRawText(), options);
                        case (byte)PacketType.LOGIN:
                            return JsonSerializer.Deserialize<LoginPacket>(root.GetRawText(), options);
                        case (byte)PacketType.UDP_LOGIN:
                            return JsonSerializer.Deserialize<UdpLoginPacket>(root.GetRawText(), options);
                        case (byte)PacketType.GAME_READY:
                            return JsonSerializer.Deserialize<GameReadyPacket>(root.GetRawText(), options);
                        case (byte)PacketType.PLAY:
                            return JsonSerializer.Deserialize<PlayPacket>(root.GetRawText(), options);
                        case (byte)PacketType.JOIN_LOBBY:
                            return JsonSerializer.Deserialize<JoinLobbyPacket>(root.GetRawText(), options);
                        case (byte)PacketType.ENCRYPTED:
                            return JsonSerializer.Deserialize<EncryptedPacket>(root.GetRawText(), options);
                    }
                }
            }

            throw new JsonException("Unknown type");
        }

        public override void Write(Utf8JsonWriter writer, Packet value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

    public abstract class Packet
	{
		[JsonPropertyName("Type")]
		public PacketType Type { get; protected set; }

		/// <summary>
		/// Converts instance into JSON
		/// </summary>
		/// <returns>Object instance as JSON</returns>
		public string ToJson()
		{
			JsonSerializerOptions options = new JsonSerializerOptions()
			{
				Converters = { new PacketConverter() },
				IncludeFields = true,
			};

			return JsonSerializer.Serialize(this, options);
		}

		/// <summary> 
		/// Deserializes JSON data
		/// </summary>
		/// <param name="json"></param>
		/// <returns>Object instance created from JSON</returns>
		public static Packet? Deserialize(string json)
		{
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Converters = { new PacketConverter() },
                IncludeFields = true,
            };

            return JsonSerializer.Deserialize<Packet>(json, options);
        }
	}
}