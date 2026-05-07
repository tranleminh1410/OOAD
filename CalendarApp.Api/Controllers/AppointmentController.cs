using CalendarApp.Api.Infrastructure;
using CalendarApp.Domain;
using CalendarApp.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CalendarApp.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AppointmentController(AppDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        }

        [HttpGet("all")]
        public IActionResult GetAllAppointments()
        {
            string currentUserId = GetCurrentUserId();

            var groupMeetings = _context.GroupMeetings
                .Include(g => g.Participants)
                .Include(g => g.Reminders)
                .Where(g => g.Participants.Any(p => p.UserId == currentUserId))
                .ToList();

            var personalApps = _context.Appointments
                .Where(a => !(a is GroupMeeting) && a.UserId == currentUserId)
                .Include(a => a.Reminders)
                .ToList();

            var allApps = personalApps.Cast<Appointment>().Union(groupMeetings).ToList();

            var result = allApps.Select(app => new
            {
                name = app.Name,
                location = app.Location,
                startTime = app.StartTime,
                endTime = app.EndTime,
                reminders = app.Reminders,
                isGroup = app is GroupMeeting,
                participants = app is GroupMeeting gm ? gm.Participants : null
            });

            return Ok(result);
        }

        [HttpGet("available-groups")]
        public IActionResult GetAvailableGroups()
        {
            try
            {
                string currentUserId = GetCurrentUserId();

                var availableGroups = _context.GroupMeetings
                    .Include(g => g.Participants)
                    .Where(g => !g.Participants.Any(p => p.UserId == currentUserId))
                    .Select(g => new
                    {
                        name = g.Name,
                        location = g.Location,
                        startTime = g.StartTime,
                        endTime = g.EndTime,
                        participantCount = g.Participants.Count
                    })
                    .ToList();

                return Ok(availableGroups);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("add")]
        public IActionResult AddAppointment([FromBody] AppointmentDTO dto)
        {
            try
            {
                string currentUserId = GetCurrentUserId();
                var currentUser = _context.Users.FirstOrDefault(u => u.UserId == currentUserId);

                int durationMinutes = (int)(dto.EndTime - dto.StartTime).TotalMinutes;
                string inputStart = dto.StartTime.ToString("yyyy-MM-dd HH:mm");

                if (!dto.IgnoreGroupConflict)
                {
                    var matchingGroup = _context.GroupMeetings.ToList().FirstOrDefault(g =>
                        g.Name.Trim().Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
                        g.GetDuration() == durationMinutes &&
                        g.StartTime.ToString("yyyy-MM-dd HH:mm") == inputStart);

                    if (matchingGroup != null)
                    {
                        return Ok(new
                        {
                            message = $"Phát hiện nhóm '{dto.Name}' đang họp. Bạn có muốn tham gia không?",
                            action = "ask_join_group"
                        });
                    }
                }

                Appointment newApp;
                if (dto.IsGroup)
                {
                    var groupMeeting = new GroupMeeting(dto.Name, dto.Location, dto.StartTime, dto.EndTime)
                    {
                        UserId = currentUserId
                    };
                    groupMeeting.AddParticipant(currentUser);
                    newApp = groupMeeting;
                }
                else
                {
                    newApp = new Appointment(dto.Name, dto.Location, dto.StartTime, dto.EndTime)
                    {
                        UserId = currentUserId
                    };
                }

                bool hasConflict = _context.Appointments.Any(app =>
                    !(app is GroupMeeting) &&
                    app.UserId == currentUserId &&
                    newApp.StartTime < app.EndTime && newApp.EndTime > app.StartTime);

                if (hasConflict)
                {
                    return Conflict(new
                    {
                        message = "Bạn đã có lịch vào thời gian này. Bạn muốn ghi đè lịch cũ chứ?",
                        action = "require_replace_choice"
                    });
                }

                if (dto.IsReminderSelected && dto.ReminderTime.HasValue)
                {
                    var type = (ReminderType)dto.ReminderType;
                    newApp.AddReminder(new Reminder(dto.ReminderTime.Value, dto.ReminderMessage, type));
                }

                _context.Appointments.Add(newApp);
                _context.SaveChanges();

                string successMsg = dto.IsGroup ? "Đã TẠO cuộc họp nhóm thành công!" : "Đã thêm lịch trình cá nhân thành công!";
                return Ok(new { message = successMsg, action = "success" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("join-group")]
        public IActionResult JoinGroup([FromBody] AppointmentDTO dto)
        {
            try
            {
                string currentUserId = GetCurrentUserId();

                if (string.IsNullOrEmpty(currentUserId))
                    return Unauthorized(new { message = "Lỗi xác thực người dùng!" });

                int durationMinutes = (int)(dto.EndTime - dto.StartTime).TotalMinutes;
                string inputStart = dto.StartTime.ToString("yyyy-MM-dd HH:mm");

                var matchingGroup = _context.GroupMeetings.Include(g => g.Participants).ToList().FirstOrDefault(g =>
                    g.Name.Trim().Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    g.GetDuration() == durationMinutes &&
                    g.StartTime.ToString("yyyy-MM-dd HH:mm") == inputStart);

                if (matchingGroup != null)
                {
                    var currentUser = _context.Users.First(u => u.UserId == currentUserId);

                    if (!matchingGroup.Participants.Any(p => p.UserId == currentUser.UserId))
                    {
                        matchingGroup.AddParticipant(currentUser);
                        _context.SaveChanges();
                    }

                    return Ok(new { message = "Đã tham gia nhóm thành công!" });
                }
                return NotFound(new { message = "Không tìm thấy nhóm." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("replace")]
        public IActionResult ReplaceAppointment([FromBody] AppointmentDTO dto)
        {
            try
            {
                string currentUserId = GetCurrentUserId();
                var newApp = new Appointment(dto.Name, dto.Location, dto.StartTime, dto.EndTime)
                {
                    UserId = currentUserId
                };

                var conflictApps = _context.Appointments
                    .Include(a => a.Reminders)
                    .Where(app => !(app is GroupMeeting) &&
                                  app.UserId == currentUserId &&
                                  newApp.StartTime < app.EndTime && newApp.EndTime > app.StartTime)
                    .ToList();

                if (conflictApps.Any())
                {
                    if (dto.IsReminderSelected && dto.ReminderTime.HasValue)
                    {
                        var type = (ReminderType)dto.ReminderType;
                        newApp.AddReminder(new Reminder(dto.ReminderTime.Value, dto.ReminderMessage, type));
                    }

                    _context.Appointments.RemoveRange(conflictApps);
                    _context.Appointments.Add(newApp);
                    _context.SaveChanges();

                    return Ok(new { message = $"Đã ghi đè {conflictApps.Count} lịch trình thành công!" });
                }
                return NotFound(new { message = "Lỗi logic: Không tìm thấy lịch trùng để ghi đè." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi ghi đè: " + ex.Message });
            }
        }

        [HttpPost("delete")]
        public IActionResult DeleteAppointment([FromBody] AppointmentDTO dto)
        {
            try
            {
                string currentUserId = GetCurrentUserId();
                string inputStart = dto.StartTime.ToString("yyyy-MM-dd HH:mm");
                string inputEnd = dto.EndTime.ToString("yyyy-MM-dd HH:mm");

                var appToModify = _context.Appointments
                    .Include(a => a.Reminders)
                    .Include(a => (a as GroupMeeting).Participants)
                    .ToList()
                    .FirstOrDefault(a =>
                        a.Name == dto.Name &&
                        a.StartTime.ToString("yyyy-MM-dd HH:mm") == inputStart &&
                        a.EndTime.ToString("yyyy-MM-dd HH:mm") == inputEnd &&
                        (a.UserId == currentUserId || (a is GroupMeeting gm && gm.Participants.Any(p => p.UserId == currentUserId)))
                    );

                if (appToModify != null)
                {
                    if (appToModify.UserId == currentUserId)
                    {
                        _context.Appointments.Remove(appToModify);
                        _context.SaveChanges();
                        return Ok(new { message = "Đã hủy lịch trình thành công!" });
                    }
                    else if (appToModify is GroupMeeting groupMeeting)
                    {
                        groupMeeting.RemoveParticipant(currentUserId);
                        _context.SaveChanges();
                        return Ok(new { message = "Đã rời khỏi cuộc họp nhóm thành công!" });
                    }
                }

                return NotFound(new { message = "Không tìm thấy lịch trình hoặc bạn không có quyền thao tác!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi xóa: " + ex.Message });
            }
        }
    }
}