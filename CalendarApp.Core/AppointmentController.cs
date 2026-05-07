using System;
using CalendarApp.Domain;
using CalendarApp.DTOs;

namespace CalendarApp.Core
{
    public class AppointmentController
    {
        private readonly Calendar _calendar;
        private readonly User _currentUser;

        public AppointmentController()
        {
            _calendar = new Calendar(calendarId: 1, ownerId: 101);
            _currentUser = new User("U01", "Tran Viet Phong");

            var demoGroup = new GroupMeeting("Hop Team Phat Trien", "Phong 101", DateTime.Now.AddDays(1), DateTime.Now.AddDays(1).AddHours(2));
            _calendar.AddAppointment(demoGroup);
        }

        public Appointment CreateAppointmentInstance(AppointmentDTO dto)
        {
            return new Appointment(dto.Name, dto.Location, dto.StartTime, dto.EndTime);
        }

        public bool CheckConflict(Appointment app)
        {
            return _calendar.CheckConflict(app);
        }

        public bool ReplaceAppointment(Appointment oldApp, Appointment newApp)
        {
            return _calendar.ReplaceAppointment(oldApp, newApp);
        }

        public GroupMeeting FindMatchingGroupMeeting(string name, int duration)
        {
            return _calendar.FindMatchingGroupMeeting(name, duration);
        }

        public void JoinGroupMeeting(GroupMeeting gm)
        {
            gm.AddParticipant(_currentUser);
        }

        public void AddPersonalAppointment(Appointment app, AppointmentDTO dto)
        {
            if (dto.IsReminderSelected && dto.ReminderTime.HasValue)
            {
                var type = (ReminderType)dto.ReminderType;
                var reminder = new Reminder(dto.ReminderTime.Value, dto.ReminderMessage, type);

                app.AddReminder(reminder);
            }

            _calendar.AddAppointment(app);
        }
    }
}