using Supercell.Laser.Logic.Command.Home;

namespace Supercell.Laser.Logic.Message.Account.Auth
{
    public class AuthenticationMessage : GameMessage
    {
        public AuthenticationMessage() : base()
        {
            AccountId = 0;
        }

        public long AccountId;
        public string PassToken;
        public string DeviceId;
        public string Device;
        public bool isAndroid;
        public int RndKey;

        public int Major;
        public int Minor;
        public int Build;

        public override void Decode()
        {
            AccountId = Stream.ReadLong();
            PassToken = Stream.ReadString();
            Major = Stream.ReadInt();
            Minor = Stream.ReadInt();
            Build = Stream.ReadInt();
            Stream.ReadString();
            DeviceId = Stream.ReadString();
            Stream.ReadVInt();
            Stream.ReadVInt();
            Stream.ReadString();
            Stream.ReadString();
            isAndroid = Stream.ReadBoolean();
            Stream.ReadString();
            Stream.ReadString();
            Stream.ReadBoolean();
            Stream.ReadString();
            RndKey = Stream.ReadInt();
            Console.WriteLine($"[LoginMessage::deocde] AccountID: " + AccountId + " PassToken: " + PassToken);
        }

        public override int GetMessageType()
        {
            return 10101;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
