namespace WindowsGSM.GameServer
{
    class CS2 : Engine.Source2
    {
        public const string FullName = "Counter-Strike: 2 Dedicated Server";
        public override string Defaultmap { get { return "de_mirage"; } }
        public override string Additional { get { return "-dedicated -usercon +game_type 0 +game_mode 1 +mapgroup mg_active"; } }
        public override string Game { get { return "cs2"; } }
        public override string AppId { get { return "730"; } }

        public CS2(Functions.ServerConfig serverData) : base(serverData)
        {
            base.serverData = serverData;
        }
    }
}
