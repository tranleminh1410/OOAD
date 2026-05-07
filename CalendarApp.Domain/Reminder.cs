using System;

namespace CalendarApp.Domain
{
    public class Reminder
    {
        public int Id { get; private set; }
        public int AppointmentId { get; private set; }
        public DateTime TriggerTime { get; private set; }
        public string Message { get; private set; }
        public ReminderType Type { get; private set; }
        protected Reminder() { }

        public Reminder(DateTime triggerTime, string message, ReminderType type)
        {
            TriggerTime = triggerTime;
            Message = message;
            Type = type;
        }

        public string GetMessage()
        {
            return Message;
        }
    }
}