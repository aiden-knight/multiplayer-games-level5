﻿using System.Data.Common;
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
        GAME_READY,
        PLAY,
	}

    public class MessagePacket : Packet
    {
        public string message;
        public MessagePacket()
        {
            m_Type = PacketType.MESSAGE;
        }

        public MessagePacket(string message)
        {
            m_Type = PacketType.MESSAGE;
            this.message = message;
        }
    }

    public class PositionPacket : Packet
    {
        public float x, y;
        public PositionPacket()
        {
            m_Type = PacketType.POSITION;
        }

        public PositionPacket(float x, float y)
        {
            m_Type = PacketType.POSITION;
            this.x = x;
            this.y = y;
        }
    }
    public class LoginPacket : Packet
    {
        public int ID;
        public LoginPacket()
        {
            m_Type = PacketType.LOGIN;
        }

        public LoginPacket(int ID)
        {
            m_Type = PacketType.LOGIN;
            this.ID = ID;
        }
    }

    public class GameReadyPacket : Packet
    {
        public int playerID;
        public GameReadyPacket()
        {
            m_Type = PacketType.GAME_READY;
        }
        public GameReadyPacket(int playerID)
        {
            m_Type = PacketType.GAME_READY;
            this.playerID = playerID;
        }
    }

    public class PlayPacket : Packet
    {
        public PlayPacket()
        {
            m_Type = PacketType.PLAY;
        }
    }

    public class EmptyPacket : Packet
    {
        public EmptyPacket()
        {
            m_Type = PacketType.EMPTY;
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
                        case (byte)PacketType.MESSAGE:
                            return JsonSerializer.Deserialize<MessagePacket>(root.GetRawText(), options);
                        case (byte)PacketType.POSITION:
                            return JsonSerializer.Deserialize<PositionPacket>(root.GetRawText(), options);
                        case (byte)PacketType.LOGIN:
                            return JsonSerializer.Deserialize<LoginPacket>(root.GetRawText(), options);
                        case (byte)PacketType.GAME_READY:
                            return JsonSerializer.Deserialize<GameReadyPacket>(root.GetRawText(), options);
                        case (byte)PacketType.PLAY:
                            return JsonSerializer.Deserialize<PlayPacket>(root.GetRawText(), options);
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
		public PacketType m_Type { get; protected set; }

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