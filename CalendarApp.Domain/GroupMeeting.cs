using System;
using System.Collections.Generic;

namespace CalendarApp.Domain
{
    public class GroupMeeting : Appointment
    {
        private List<User> _participants = new List<User>();
        public IReadOnlyList<User> Participants => _participants.AsReadOnly();
        protected GroupMeeting() { }

        public GroupMeeting(string name, string location, DateTime startTime, DateTime endTime)
            : base(name, location, startTime, endTime)
        {
        }

        public void AddParticipant(User user)
        {
            if (user != null && !_participants.Contains(user))
            {
                _participants.Add(user);
            }
        }

        public void RemoveParticipant(string userId)
        {
            var user = _participants.FirstOrDefault(u => u.UserId == userId);
            if (user != null)
            {
                _participants.Remove(user);
            }
        }
    }
}