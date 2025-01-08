namespace Supercell.Laser.Logic.Data
{
    public class EmoteData : LogicData
    {
        public EmoteData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }

        public bool Disabled { get; set; }

        public string FileName { get; set; }

        public string ExportName { get; set; }

        public string Character { get; set; }

        public string Skin { get; set; }
        
        public bool IsPicto { get; set; }

        public string BattleCategory { get; set; }

        public int Type { get; set; }

        public int Rarity { get; set; }

        public string EmoteType { get; set; }

        public string SoundEffect { get; set; }

        public int SfxIndex { get; set; }

        public bool LockedForChronos { get; set; }

        public string EmoteBundles { get; set; }

        public bool IsDefaultBattleEmote { get; set; }
    }
}
