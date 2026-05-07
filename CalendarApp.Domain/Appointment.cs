using System;
using System.Collections.Generic;

namespace CalendarApp.Domain
{
    public class Appointment
    {
        public int Id { get; protected set; }
        public string Name { get; protected set; }
        public string Location { get; protected set; }
        public DateTime StartTime { get; protected set; }
        public DateTime EndTime { get; protected set; }
        public string? UserId { get; set; }

        private List<Reminder> _reminders = new List<Reminder>();
        public IReadOnlyList<Reminder> Reminders => _reminders.AsReadOnly();
        protected Appointment() { }

        public Appointment(string name, string location, DateTime startTime, DateTime endTime)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên cuộc hẹn không được để trống.");

            if (endTime <= startTime)
                throw new ArgumentException("Thời gian kết thúc phải sau thời gian bắt đầu.");

            Name = name;
            Location = location;
            StartTime = startTime;
            EndTime = endTime;
        }

        public int GetDuration()
        {
            // Trả về số phút của cuộc hẹn
            return (int)(EndTime - StartTime).TotalMinutes;
        }

        public void AddReminder(Reminder reminder)
        {
            if (reminder != null)
            {
                _reminders.Add(reminder);
            }
        }
    }
}