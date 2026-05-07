namespace CalendarApp.Domain
{
    public class User
    {
        public string UserId { get; private set; }
        public string UserName { get; private set; }
        public string PasswordHash { get; private set; }
        protected User() { }
        public User(string userId, string userName, string passwordHash)
        {
            UserId = userId;
            UserName = userName;
            PasswordHash = passwordHash;
        }
    }
}