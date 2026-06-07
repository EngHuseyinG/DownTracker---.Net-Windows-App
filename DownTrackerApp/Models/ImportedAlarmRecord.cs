namespace DownTracker.Models
{
    public class ImportedAlarmRecord
    {
        public string FullTagName { get; set; }     // Kaynak R Sütunu (İndex 17)
        public string DeviceName { get; set; }      // Kaynak B Sütunu (İndex 1)
        public string Comment { get; set; }         // Kaynak O Sütunu (İndex 14)
        public string ParentCategory { get; set; }   // Kaynak P Sütunu (İndex 15)
        public string Category { get; set; }         // Kaynak Q Sütunu (İndex 16)
        public string Code { get; set; } = "";       // Şimdilik Boş (F Sütunu)
    }
}
