using System.Linq;

namespace Supercell.Laser.Logic.Home
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home.Gatcha;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Home.Quest;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Titan.DataStream;
    using System;
    using System.Collections.Immutable;
    using System.Numerics;
    using System.Reflection.Metadata;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using static System.Collections.Specialized.BitVector32;

    [JsonObject(MemberSerialization.OptIn)]
    public class ClientHome
    {
        public const int DAILYOFFERS_COUNT = 6;

        public static readonly int[] GoldPacksPrice = new int[]
        {
            20, 50, 140, 280
        };

        public static readonly int[] GoldPacksAmount = new int[]
        {
            150, 400, 1200, 2600
        };

        [JsonProperty] public long HomeId;
        [JsonProperty] public int ThumbnailId;
        [JsonProperty] public int NameColorId;
        [JsonProperty] public int CharacterId;

        [JsonProperty] public List<OfferBundle> OfferBundles;

        [JsonProperty] public int TrophiesReward;
        [JsonProperty] public int TokenReward;
        [JsonProperty] public int StarTokenReward;
        [JsonProperty] public int StarPointsGained;
        [JsonProperty] public int PowerPlayTrophiesReward;

        [JsonProperty] public BigInteger BrawlPassProgress;
        [JsonProperty] public BigInteger PremiumPassProgress;
        [JsonProperty] public int BrawlPassTokens;
        [JsonProperty] public bool HasPremiumPass;
        [JsonProperty] public List<int> UnlockedEmotes;

        [JsonProperty] public int Experience;
        [JsonProperty] public int TokenDoublers;

        [JsonProperty] public int TrophyRoadProgress;
        [JsonProperty] public Quests Quests;
        [JsonProperty] public NotificationFactory NotificationFactory;
        [JsonProperty] public List<int> UnlockedSkins;
        [JsonProperty] public int[] SelectedSkins;
        [JsonProperty] public int PowerPlayGamesPlayed;
        [JsonProperty] public int PowerPlayScore;
        [JsonProperty] public int PowerPlayHighestScore;
        [JsonProperty] public int BattleTokens;
        [JsonProperty] public DateTime BattleTokensRefreshStart;
        [JsonProperty] public DateTime PremiumEndTime;
        [JsonProperty] public DateTime ChatBanEndTime;
        [JsonProperty] public DateTime BanEndTime;
        [JsonProperty] public List<long> ReportsIds;
        [JsonProperty] public bool BlockFriendRequests;
        [JsonProperty] public string IpAddress;
        [JsonProperty] public string Device;


        [JsonIgnore] public EventData[] Events;

        public PlayerThumbnailData Thumbnail => DataTables.Get(DataType.PlayerThumbnail).GetDataByGlobalId<PlayerThumbnailData>(ThumbnailId);
        public NameColorData NameColor => DataTables.Get(DataType.NameColor).GetDataByGlobalId<NameColorData>(NameColorId);
        public CharacterData Character => DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(CharacterId);

        public HomeMode HomeMode;

        [JsonProperty] public DateTime LastVisitHomeTime;
        [JsonProperty] public DateTime LastRotateDate;

        [JsonIgnore] public bool ShouldUpdateDay;

        public ClientHome()
        {
            ThumbnailId = GlobalId.CreateGlobalId(28, 0);
            NameColorId = GlobalId.CreateGlobalId(43, 0);
            CharacterId = GlobalId.CreateGlobalId(16, 0);
            SelectedSkins = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };

            OfferBundles = new List<OfferBundle>();
            ReportsIds = new List<long>();
            UnlockedSkins = new List<int>();
            LastVisitHomeTime = DateTime.UnixEpoch;

            TrophyRoadProgress = 1;

            BrawlPassProgress = 1;
            PremiumPassProgress = 1;

            UnlockedEmotes = new List<int>();
            BattleTokens = 200;
            BattleTokensRefreshStart = new();
            if (NotificationFactory == null)
            {
                NotificationFactory = new NotificationFactory();
            }

        }

        public void HomeVisited()
        {
            
            RotateShopContent(DateTime.UtcNow, OfferBundles.Count == 0);
            LastVisitHomeTime = DateTime.UtcNow;
            //Quests = null;

            if (Quests == null && TrophyRoadProgress >= 11)
            {
                Quests = new Quests();
                Quests.AddRandomQuests(HomeMode.Avatar.Heroes, 6);
            }
        }

        /*public void Tick()
        {
            LastVisitHomeTime = DateTime.UtcNow;
            TokenReward = 0;
            TrophiesReward = 0;
            StarTokenReward = 0;
            StarPointsGained = 0;
            PowerPlayTrophiesReward = 0;
        }*/
        public void Tick()
        {
            LastVisitHomeTime = DateTime.UtcNow;
            while (ShouldAddTokens())
            {
                BattleTokensRefreshStart = BattleTokensRefreshStart.AddMinutes(30);
                BattleTokens = Math.Min(200, BattleTokens + 30);
                if (BattleTokens == 200)
                {
                    BattleTokensRefreshStart = new();
                    break;
                }
            }
            RotateShopContent(DateTime.UtcNow, OfferBundles.Count == 0);

        }

        public int GetbattleTokensRefreshSeconds()
        {
            if (BattleTokensRefreshStart == new DateTime())
            {
                return -1;
            }
            return (int)BattleTokensRefreshStart.AddMinutes(30).Subtract(DateTime.UtcNow).TotalSeconds;
        }
        public bool ShouldAddTokens()
        {
            if (BattleTokensRefreshStart == new DateTime())
            {
                return false;
            }
            return GetbattleTokensRefreshSeconds() < 1;
        }

        public void PurchaseOffer(int index)
        {
            if (index < 0 || index >= OfferBundles.Count) return;

            OfferBundle bundle = OfferBundles[index];
            if (bundle.Purchased) return;

            if (bundle.Currency == 0)
            {
                if (!HomeMode.Avatar.UseDiamonds(bundle.Cost)) return;
            }
            else if (bundle.Currency == 1)
            {
                if (!HomeMode.Avatar.UseGold(bundle.Cost)) return;
            }
            else if (bundle.Currency == 3)
            {
                if (!HomeMode.Avatar.UseStarPoints(bundle.Cost)) return;
            }

            bundle.Purchased = true;

            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            Random rand = new Random();

            foreach (Offer offer in bundle.Items)
            {
                if (offer.Type == ShopItem.BrawlBox || offer.Type == ShopItem.FreeBox)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(10);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.HeroPower)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(6);
                    reward.DataGlobalId = offer.ItemDataId;
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.BigBox)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(12);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.MegaBox)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(11);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.Skin)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(9);
                    reward.SkinGlobalId = GlobalId.CreateGlobalId(29, offer.SkinDataId);
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Gems)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(8);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Coin)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(7);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.CoinDoubler)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(2);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else
                {
                    // todo...
                }

                command.Execute(HomeMode);

                
            }
            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            HomeMode.GameListener.SendMessage(message);
        }

        private void RotateShopContent(DateTime time, bool isNewAcc)
        {
            /*if (OfferBundles.Select(bundle => bundle.IsDailyDeals).ToArray().Length > 6)
            {
                OfferBundles.RemoveAll(bundle => bundle.IsDailyDeals);
            }*/
            bool IsUpdated = false;
            int offLen = OfferBundles.Count;
            OfferBundles.RemoveAll(offer => offer.EndTime <= time);
            bool SHOULD_STAR_BIGBOX1 = true;
            bool SHOULD_STAR_BIGBOX2 = true;
            bool SHOULD_STAR_MEGABOX1 = true; 
            bool SHOULD_STAR_MEGABOX2 = true;
            bool SHOULD_STAR_MEGABOX3 = true;
            bool SHOULD_COMPENSACIA = true;
            bool SHOULD_COMPENSACIA1 = false;
            bool SHOULD_FREE3 = false;
            bool SHOULD_FREE4 = false;

            if (SHOULD_COMPENSACIA1)
            {
                OfferBundle bundle = new OfferBundle();
                bundle.Title = "1000 in telegram";
                bundle.IsDailyDeals = false;
                bundle.EndTime = DateTime.UtcNow.AddDays(5); // tomorrow at 8:00 utc (11:00 MSK)
                //bundle.BackgroundExportName = "offer_legendary";
                Offer megaBoxOffer = new Offer(ShopItem.MegaBox, 1);
                //bundle.Items.Add(megaBoxOffer);
                bundle.Cost = 0;
                bundle.Currency = 0;
                //OfferBundles.Add(bundle);
            }
            IsUpdated = OfferBundles.Count != offLen;
            if (isNewAcc || DateTime.UtcNow.Hour >= 8) // Daily deals refresh at 08:00 AM UTC
            {
                //LastRotateDate = new DateTime();
                if (LastRotateDate < DateTime.UtcNow.Date)
                {
                    IsUpdated = true;
                    OfferBundles.RemoveAll(offer => offer.IsDailyDeals);
                    LastRotateDate = DateTime.UtcNow.Date;
                    UpdateDailyOfferBundles();
                    UpdateDailySkins();
                    PowerPlayGamesPlayed = 0;
                    ReportsIds = new List<long>();
                    if (Quests != null)
                    {
                        Quests.QuestList.RemoveAll(bundle => bundle.IsDailyQuest);
                        Quests.AddRandomQuests(HomeMode.Avatar.Heroes, Quests.QuestList.Count >= 18 ? 8 : 10);
                    }
                }
            }
            if (OfferBundles == null)
            {
                UpdateDailyOfferBundles();
                IsUpdated = true;
            }
            else if (OfferBundles.Count == 0)
            {
                UpdateDailyOfferBundles();
                IsUpdated = true;
            }
            if (IsUpdated)
            {
                LogicDayChangedCommand newday = new()
                {
                    Home = this
                };
                newday.Home.Events = Events;
                AvailableServerCommandMessage eventupdated = new()
                {
                    Command = newday,
                };
                HomeMode.GameListener.SendMessage(eventupdated);
            }
        }

        private void UpdateDailySkins()
        {
            List<string> skins = new() { "Witch", "Rockstar", "Beach", "Pink", "Panda", "White", "Hair", "Gold", "Rudo", "Bandita", "Rey", "Knight", "Caveman", "Dragon", "Summer", "Summertime", "Pheonix", "Greaser", "GirlPrereg", "Box", "Santa", "Chef", "Boombox", "Wizard", "Reindeer", "GalElf", "Hat", "Footbull", "Popcorn", "Hanbok", "Cny", "Valentine", "WarsBox", "Nightwitch", "Cart", "Shiba", "GalBunny", "Ms", "GirlHotrod", "Maple", "RR", "Mecha", "MechaWhite", "MechaNight", "FootbullBlue", "Outlaw", "Hogrider", "BoosterDefault", "Shark", "HoleBlue", "BoxMoonFestival", "WizardRed", "Pirate", "GirlWitch", "KnightDark", "DragonDark", "DJ", "Wolf", "Brown", "Total", "Sally", "Leonard", "SantaRope", "Gift", "GT", "SniperDefaultAddonBee", "SniperLadyBug", "SniperLadyBugAddonBee", "Virus", "BoosterVirus", "HoleStreetNinja", "Gamer", "Valentines", "Koala", "BearKoala", "TurretDefault", "AgentP", "Football", "Arena", "Tanuki", "Horus", "ArenaPSG", "DarkBunny", "College", "TurretTanuki", "TotemDefault", "Bazaar", "RedDragon", "Constructor", "Hawaii", "Barbking", "Trader", "StationSummer", "Silver", "SniperMonster", "BombMonster", "SniperMonsterAddonBee", "Bank", "Retro", "Ranger", "Tracksuit", "Knight" };
            List<int> skis = new();
            List<int> starss = new();
            foreach (Hero h in HomeMode.Avatar.Heroes)
            {
                CharacterData c = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(h.CharacterId);
                string cn = c.Name;
                foreach (string name in skins)
                {
                    SkinData s = DataTables.Get(DataType.Skin).GetData<SkinData>(cn + name);
                    if (s != null)
                    {
                        if (UnlockedSkins.Contains(s.GetGlobalId())) continue;
                        if (s.Name == "RocketGirlRanger") continue;
                        if (s.Name == "PowerLevelerKnight") continue;
                        if (s.Name == "BlowerTrader") continue;
                        if (s.CostStarPoints > 1)
                        {
                            starss.Add(s.GetGlobalId());
                            continue;
                        }
                        if (!s.Name.EndsWith("Gold"))
                            skis.Add(s.GetGlobalId());
                        else
                        {
                            string sss = s.Name.Replace("Gold", "Silver");
                            SkinData sc = DataTables.Get(DataType.Skin).GetData<SkinData>(sss);
                            if (sc == null)
                            {
                                skis.Add(s.GetGlobalId());
                                continue;
                            }
                            if (UnlockedSkins.Contains(sc.GetGlobalId()))
                            {
                                skis.Add(sc.GetGlobalId());
                            }
                        }
                    }
                }
            }


            Random random = new Random();
            int[] selectedElements = new int[Math.Min(skis.Count, 8)];
            for (int i = 0; i < Math.Min(skis.Count, 8); i++)
            {
                int randomIndex;
                do
                {
                    randomIndex = random.Next(0, skis.Count);
                } while (selectedElements.Contains(skis[randomIndex]));

                selectedElements[i] = skis[randomIndex];
            }

            foreach (int bbbbbb in selectedElements)
            {
                SkinData skin = DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(bbbbbb);
                OfferBundle bundle = new OfferBundle();
                bundle.IsDailyDeals = false;
                bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)
                if (skin.CostGems > 0)
                {
                    bundle.Currency = 0;
                    bundle.Cost = (skin.CostGems - 1);
                }
                else if (skin.CostCoins > 0)
                {
                    bundle.Currency = 1;
                    bundle.Cost = (skin.CostCoins);
                }
                else
                {
                    continue;
                }

                Offer offer = new Offer(ShopItem.Skin, 1);
                offer.SkinDataId = GlobalId.GetInstanceId(bbbbbb);
                bundle.Items.Add(offer);
                OfferBundles.Add(bundle);
            }
            int[] selectedStElements = new int[Math.Min(starss.Count, 3)];
            for (int i = 0; i < Math.Min(starss.Count, 3); i++)
            {
                int randomIndex;
                do
                {
                    randomIndex = random.Next(0, starss.Count);
                } while (selectedStElements.Contains(starss[randomIndex]));

                selectedStElements[i] = starss[randomIndex];
            }
            foreach (int bbbbbb in selectedStElements)
            {
                SkinData skin = DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(bbbbbb);
                OfferBundle bundle = new OfferBundle();
                bundle.Currency = 3;
                bundle.IsDailyDeals = false;
                bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)
                bundle.Cost = skin.CostStarPoints;

                Offer offer = new Offer(ShopItem.Skin, 1);
                offer.SkinDataId = GlobalId.GetInstanceId(bbbbbb);
                bundle.Items.Add(offer);
                OfferBundles.Add(bundle);
            }
        }

        private void UpdateDailyOfferBundles()
        {
            OfferBundles = new List<OfferBundle>();
            OfferBundles.Add(GenerateDailyGift());

            List<Hero> unlockedHeroes = HomeMode.Avatar.Heroes;
            List<Hero> PossibleHeroes = new();
            foreach (Hero h in unlockedHeroes)
            {
                if (h.PowerLevel == 8) continue;
                if (h.PowerPoints >= 1410) continue;
                PossibleHeroes.Add(h);
            }
            Random random = new Random();
            bool shouldPowerPoints = true;
            bool hasMG = false;
            int offcount = 0;
            for (int i = 1; i < DAILYOFFERS_COUNT; i++)
            {
                if (PossibleHeroes.Count == 0) break;
                offcount++;
                if (!hasMG && (random.Next(0, 100) > 33))
                {
                    i++;
                    offcount++;
                    hasMG = true;
                    OfferBundle a = GenerateDailyOffer(false, null);
                    if (a != null)
                    {
                        OfferBundles.Add(a);
                    }
                }
                int inds = random.Next(0, PossibleHeroes.Count);

                Hero brawler = PossibleHeroes[inds];
                PossibleHeroes.Remove(PossibleHeroes[inds]);
                OfferBundle dailyOffer = GenerateDailyOffer(shouldPowerPoints, brawler);
                if (dailyOffer != null)
                {
                    if (!shouldPowerPoints) shouldPowerPoints = dailyOffer.Items[0].Type != ShopItem.HeroPower;
                    OfferBundles.Add(dailyOffer);
                }
            }
            if (offcount < 5 && !hasMG)
            {
                OfferBundle dailyOffer = GenerateDailyOffer(false, null);
                if (dailyOffer != null)
                {
                    OfferBundles.Add(dailyOffer);
                }
            }
        }

        private OfferBundle GenerateDailyGift()
        {
            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = true;
            bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)
            bundle.Cost = 0;

            Offer offer = new Offer(ShopItem.FreeBox, 1);
            bundle.Items.Add(offer);

            return bundle;
        }

        private OfferBundle GenerateDailyOffer(bool shouldPowerPoints, Hero brawler)
        {
            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = true;
            bundle.EndTime = DateTime.UtcNow.Date.AddDays(1).AddHours(8); // tomorrow at 8:00 utc (11:00 MSK)

            Random random = new Random();
            int type = shouldPowerPoints ? 0 : 1; // getting a type

            switch (type)
            {
                case 0: // Power points
                    
                    //for (int i = 0; i < Math.Min(PossibleHeroes.Count, 5))
                    

                    int count = random.Next(15, 100) + 1;
                    Offer offer = new Offer(ShopItem.HeroPower, count, brawler.CharacterId);

                    bundle.Items.Add(offer);
                    bundle.Cost = count * 2;
                    bundle.Currency = 1;

                    break;
                case 1: // mega box
                    Offer megaBoxOffer = new Offer(ShopItem.MegaBox, 1);
                    bundle.Items.Add(megaBoxOffer);
                    bundle.Cost = 40;
                    bundle.OldCost = 80;
                    bundle.Currency = 0;
                    break;
            }

            return bundle;
        }
        public void LogicDailyData(ByteStream encoder, DateTime utcNow)
        {

            encoder.WriteVInt(utcNow.Year * 1000 + utcNow.DayOfYear); // 0x78d4b8
            encoder.WriteVInt(utcNow.Hour * 3600 + utcNow.Minute * 60 + utcNow.Second); // 0x78d4cc
            encoder.WriteVInt(HomeMode.Avatar.Trophies); // 0x78d4e0
            encoder.WriteVInt(HomeMode.Avatar.HighestTrophies); // 0x78d4f4
            encoder.WriteVInt(HomeMode.Avatar.HighestTrophies); // highest trophy again?
            encoder.WriteVInt(TrophyRoadProgress);
            encoder.WriteVInt(Experience + 1909); // experience

            ByteStreamHelper.WriteDataReference(encoder, Thumbnail);
            ByteStreamHelper.WriteDataReference(encoder, NameColorId);

            encoder.WriteVInt(18); // Played game modes
            for (int i = 0; i < 18; i++)
            {
                encoder.WriteVInt(i);
            }

            encoder.WriteVInt(53); // Selected Skins Dictionary
            for (int i = 0; i < 53; i++)
            {
                encoder.WriteVInt(29);
                try
                {
                    encoder.WriteVInt(SelectedSkins[i]);
                }
                catch
                {
                    encoder.WriteVInt(0);
                    SelectedSkins = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,};
                }
            }

            encoder.WriteVInt(UnlockedSkins.Count); // Played game modes
            foreach (int s in UnlockedSkins)
            {
                ByteStreamHelper.WriteDataReference(encoder, s);
            }

            encoder.WriteVInt(0);

            encoder.WriteVInt(0);

            encoder.WriteVInt(0);

            encoder.WriteVInt(0);

            encoder.WriteVInt(0);

            encoder.WriteVInt(0);
            encoder.WriteVInt(HomeMode.Avatar.HighestTrophies); // 122
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            encoder.WriteBoolean(true);
            encoder.WriteVInt(TokenDoublers);
            encoder.WriteVInt(0); // token doubler
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);

            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);

            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);
            encoder.WriteBoolean(false);
            encoder.WriteVInt(2);
            encoder.WriteVInt(2);
            encoder.WriteVInt(2);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);

            encoder.WriteVInt(0); // Shop offers at 0x78e0c4

            encoder.WriteVInt(0);

            encoder.WriteVInt(BattleTokens); // 0x78e228
            encoder.WriteVInt(GetbattleTokensRefreshSeconds()); // 0x78e23c
            encoder.WriteVInt(0); // 0x78e250
            encoder.WriteVInt(0); // 0x78e3a4
            encoder.WriteVInt(0); // 0x78e3a4

            ByteStreamHelper.WriteDataReference(encoder, Character);

            encoder.WriteString("RU");
            encoder.WriteString("t.me/archbrawl");

            encoder.WriteVInt(0);

            TokenReward = 0;
            TrophiesReward = 0;
            StarTokenReward = 0;
            StarPointsGained = 0;
            PowerPlayTrophiesReward = 0;

            encoder.WriteVInt(0); // array

            encoder.WriteVInt(1); // BrawlPassSeasonData
            {
                encoder.WriteVInt(8);
                encoder.WriteVInt(BrawlPassTokens);
                //encoder.WriteVInt(PremiumPassProgress);
                encoder.WriteBoolean(HasPremiumPass);
                encoder.WriteVInt(0);
                encoder.WriteBoolean(false); // что ето

                if (encoder.WriteBoolean(true)) // Track 9
                {
                    encoder.WriteLongLong128(PremiumPassProgress);
                }
                if (encoder.WriteBoolean(true)) // Track 10
                {
                    encoder.WriteLongLong128(BrawlPassProgress);
                }
            }

            encoder.WriteVInt(1);
            {
                encoder.WriteVInt(2);
                encoder.WriteVInt(PowerPlayScore);
            }

            encoder.WriteBoolean(true);
            encoder.WriteVInt(0);

            encoder.WriteBoolean(true);
            encoder.WriteVInt(0);

            encoder.WriteBoolean(false);

            encoder.WriteInt(0);
        }

        public void LogicConfData(ByteStream encoder, DateTime utcNow)
        {
            encoder.WriteVInt(0);

            encoder.WriteVInt(9);
            for (int i = 1; i <= 9; i++)
                encoder.WriteVInt(i);

            encoder.WriteVInt(Events.Length);
            foreach (EventData data in Events)
            {
                data.IsSecondary = false;
                data.Encode(encoder);
            }

            encoder.WriteVInt(0);

            encoder.WriteVInt(0);

            ByteStreamHelper.WriteIntList(encoder, GoldPacksPrice);
            ByteStreamHelper.WriteIntList(encoder, GoldPacksAmount);

            encoder.WriteBoolean(true);
            encoder.WriteVInt(0); // ReleaseEntry
            encoder.WriteVInt(1); // IntValueEntries
            {
                encoder.WriteInt(1);
                encoder.WriteInt(41000034); // theme
            }
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);

            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
        }

        public void Encode(ByteStream encoder)
        {
            DateTime utcNow = DateTime.UtcNow;

            LogicDailyData(encoder, utcNow);
            LogicConfData(encoder, utcNow);

            encoder.WriteLong(HomeId);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            /*encoder.WriteVInt(1);
            {
                encoder.WriteVInt(83);
                encoder.WriteInt(0);
                encoder.WriteBoolean(false);
                encoder.WriteInt(0);
                encoder.WriteString(null);
                encoder.WriteInt(0);
                encoder.WriteStringReference("Добро пожаловать в Simple Brawl");
                encoder.WriteInt(0);
                encoder.WriteStringReference("Также рекомендуем зайти в наш тг канал и чат нажав кнопку ниже!(t.me/simpleservers)");
                encoder.WriteInt(0);
                encoder.WriteStringReference("Перейти");
                encoder.WriteStringReference("pop_up_1920x1235_welcome.png");
                encoder.WriteStringReference("6bb3b752a80107a14671c7bdebe0a1b662448d0c");
                encoder.WriteStringReference("brawlstars://extlink?page=https%3A%2F%2Ft.me%simpleservers%2F");
                encoder.WriteVInt(0);
            }*/

            encoder.WriteBoolean(false);
            encoder.WriteVInt(0);
        }
    }
}
