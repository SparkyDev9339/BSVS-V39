namespace Supercell.Laser.Logic.Home.Quest
{
    using Newtonsoft.Json;
    using Supercell.Laser.Logic.Battle.Objects;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Titan.DataStream;

    public class Quests
    {
        /*
         0 - gemgrab
         2 - heist
         3 - bounty
         5 - brawlball
         6 - solosd
         7 - biggame
         8 - roborumble
         9 - dsh
         10 - boss
         11 - siage
         */
        private static readonly int[] ALLOWED_MODES = { 0, 2, 5, 6, 9, 11 };
        private static readonly int[] GOAL_TABLE = { 40000, 50000, 60000, 80000, 100000 };
        private static readonly int[] WIN_TABLE = { 6, 10 };

        [JsonProperty("quest_list")]
        public List<Quest> QuestList;

        public Quests()
        {
            QuestList = new List<Quest>();
        }

        public void AddRandomQuests(List<Hero> unlockedHeroes, int count)
        {
            List<int> characters = unlockedHeroes.Select(x => x.CharacterId).ToList();

            Random rand = new Random();

            Add100Quest(rand, characters, false);
            Add100Quest(rand, characters, false);
            Add250Quest(rand, characters, false);
            Add250Quest(rand, characters, false);
            Add500Quest(rand, characters, false);
            Add500Quest(rand, characters, true);
        }

        private void Add500Quest(Random rand, List<int> characters, bool IsPremium)
        {
            Quest quest = new Quest();

            quest.MissionType = 1;

            bool forCharacter = rand.Next(120) > 40;
            if (forCharacter)
            {
                quest.CharacterId = characters[rand.Next(0, characters.Count)];
                quest.GameModeVariation = -1;
            }
            else
            {
                quest.GameModeVariation = ALLOWED_MODES[rand.Next(0, ALLOWED_MODES.Length)];
            }

            switch (quest.MissionType)
            {
                case 1:
                    quest.QuestGoal = 10;
                    quest.Reward = 500;
                    break;
            }
            quest.BrawlPassExclusive = IsPremium;

            QuestList.Add(quest);
        }

        private void Add250Quest(Random rand, List<int> characters, bool IsPremium)
        {
            Quest quest = new Quest();

            quest.MissionType = 1;

            bool forCharacter = rand.Next(120) > 40;
            if (forCharacter)
            {
                quest.CharacterId = characters[rand.Next(0, characters.Count)];
                quest.GameModeVariation = -1;
            }
            else
            {
                quest.GameModeVariation = ALLOWED_MODES[rand.Next(0, ALLOWED_MODES.Length)];
            }

            switch (quest.MissionType)
            {
                case 1:
                    quest.QuestGoal = 6;
                    quest.Reward = 250;
                    break;
            }
            quest.BrawlPassExclusive = IsPremium;

            QuestList.Add(quest);
        }

        private void Add100Quest(Random rand, List<int> characters, bool IsPremium)
        {
            Quest quest = new Quest();

            quest.MissionType = 1;

            bool forCharacter = rand.Next(120) > 40;
            if (forCharacter)
            {
                quest.CharacterId = characters[rand.Next(0, characters.Count)];
                quest.GameModeVariation = -1;
            }
            else
            {
                quest.GameModeVariation = ALLOWED_MODES[rand.Next(0, ALLOWED_MODES.Length)];
            }

            switch (quest.MissionType)
            {
                case 1:
                    quest.QuestGoal = 3;
                    quest.Reward = 100;
                    break;
            }
            quest.BrawlPassExclusive = IsPremium;
            quest.IsDailyQuest = true;

            QuestList.Add(quest);
        }

        public List<Quest> UpdateQuestsProgress(int gameModeVariation, int characterId, int kills, int damage, int heals, ClientHome home)
        {
            List<Quest> completed = new List<Quest>();
            List<Quest> progressive = new List<Quest>();

            if (QuestList == null)
            {
                return progressive;
            }
            foreach (Quest quest in QuestList.ToArray())
            {
                if ((quest.GameModeVariation == gameModeVariation || quest.GameModeVariation == -1) 
                    && (quest.CharacterId == characterId || quest.CharacterId == 0))
                {
                    if (quest.BrawlPassExclusive && !home.HasPremiumPass) continue;
                    if (quest.MissionType == 1)
                    {
                        var progress = quest.Clone();
                        progress.Progress = 1;

                        quest.CurrentGoal += 1;
                        if (quest.CurrentGoal >= quest.QuestGoal)
                        {
                            completed.Add(quest);
                        }

                        progressive.Add(progress);
                    }
                    else if (quest.MissionType == 2)
                    {
                        var progress = quest.Clone();
                        progress.Progress = kills;

                        quest.CurrentGoal += kills;
                        if (quest.CurrentGoal >= quest.QuestGoal)
                        {
                            completed.Add(quest);
                        }

                        progressive.Add(progress);
                    }
                    else if (quest.MissionType == 3)
                    {
                        var progress = quest.Clone();
                        progress.Progress = damage;

                        quest.CurrentGoal += damage;
                        if (quest.CurrentGoal >= quest.QuestGoal)
                        {
                            completed.Add(quest);
                        }

                        progressive.Add(progress);
                    }
                    else if (quest.MissionType == 4)
                    {
                        var progress = quest.Clone();
                        progress.Progress = heals;
                        if (progress.CurrentGoal + progress.Progress > progress.QuestGoal)
                        {
                            progress.Progress = progress.QuestGoal - progress.CurrentGoal;
                        }

                        quest.CurrentGoal += heals;
                        if (quest.CurrentGoal >= quest.QuestGoal)
                        {
                            completed.Add(quest);
                        }

                        progressive.Add(progress);
                    }
                }
            }

            foreach (Quest quest in completed)
            {
                QuestList.Remove(quest);
                home.TokenReward += quest.Reward;
                home.BrawlPassTokens += quest.Reward;
            }

            return progressive;
        }

        public void Encode(ChecksumEncoder encoder)
        {
            encoder.WriteVInt(QuestList.Count);
            foreach (Quest quest in QuestList.ToArray())
            {
                quest.Encode(encoder);
            }
        }
    }
}
