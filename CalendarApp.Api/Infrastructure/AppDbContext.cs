using Microsoft.EntityFrameworkCore;
using CalendarApp.Domain;

namespace CalendarApp.Api.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<GroupMeeting> GroupMeetings { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasKey(u => u.UserId);

            modelBuilder.Entity<Appointment>()
                .HasDiscriminator<string>("AppointmentType")
                .HasValue<Appointment>("Personal")
                .HasValue<GroupMeeting>("Group");   
            modelBuilder.Entity<GroupMeeting>()
                .HasMany(g => g.Participants)
                .WithMany();
        }
    }
}