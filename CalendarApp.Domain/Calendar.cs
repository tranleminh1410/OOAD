using System;
using System.Collections.Generic;
using System.Linq;

namespace CalendarApp.Domain
{
    public class Calendar
    {
        public int CalendarId { get; private set; }
        public int OwnerId { get; private set; }

        private List<Appointment> _appointments = new List<Appointment>();
        public IReadOnlyList<Appointment> Appointments => _appointments.AsReadOnly();

        private List<GroupMeeting> _systemGroupMeetings = new List<GroupMeeting>();
        public IReadOnlyList<GroupMeeting> SystemGroupMeetings => _systemGroupMeetings.AsReadOnly();

        public Calendar(int calendarId, int ownerId)
        {
            CalendarId = calendarId;
            OwnerId = ownerId;
        }

        public void AddSystemGroupMeeting(GroupMeeting gm)
        {
            _systemGroupMeetings.Add(gm);
        }

        public void AddAppointment(Appointment app)
        {
            if (app != null) _appointments.Add(app);
        }

        public bool CheckConflict(Appointment newApp)
        {
            return _appointments.Any(app => newApp.StartTime < app.EndTime && newApp.EndTime > app.StartTime);
        }

        // Đã sửa: Tìm TẤT CẢ các lịch cũ nằm trong khoảng thời gian của lịch mới
        public List<Appointment> GetConflictingAppointments(Appointment newApp)
        {
            return _appointments.Where(app => newApp.StartTime < app.EndTime && newApp.EndTime > app.StartTime).ToList();
        }

        // Đã sửa: Xóa tất cả các lịch cũ bị trùng và thêm lịch mới vào
        public void ReplaceAppointments(List<Appointment> oldApps, Appointment newApp)
        {
            foreach (var app in oldApps)
            {
                _appointments.Remove(app);
            }
            _appointments.Add(newApp);
        }

        public void RemoveAppointment(string name, DateTime start, DateTime end)
        {
            // Ép về cùng định dạng để tránh lỗi mili-giây khi xóa
            string inputStart = start.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            string inputEnd = end.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

            var appToRemove = _appointments.FirstOrDefault(a =>
                a.Name == name
                && a.StartTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm") == inputStart
                && a.EndTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm") == inputEnd);

            if (appToRemove != null) _appointments.Remove(appToRemove);
        }

        public GroupMeeting FindMatchingGroupMeeting(string name, int duration, DateTime startTime)
        {
            string inputTimeStr = startTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

            foreach (var gm in _systemGroupMeetings)
            {
                string gmTimeStr = gm.StartTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                if (gm.Name.Trim().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)
                    && gm.GetDuration() == duration
                    && gmTimeStr == inputTimeStr)
                {
                    return gm;
                }
            }
            return null;
        }
    }
}