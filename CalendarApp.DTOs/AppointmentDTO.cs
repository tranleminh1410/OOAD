using System;

namespace CalendarApp.DTOs
{
    public class AppointmentDTO
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsReminderSelected { get; set; }
        public DateTime? ReminderTime { get; set; }
        public string ReminderMessage { get; set; }

        // Enum: 0 là EMAIL, 1 là SMS
        public int ReminderType { get; set; }
        public bool IsGroup { get; set; }
        public bool IgnoreGroupConflict { get; set; }
    }
}