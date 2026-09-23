using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VbeSort.Models;

namespace VbeSort.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // EnsureCreated skips table creation when other tables already exist in the schema
            // (e.g. Supabase's built-in tables). Create our table explicitly instead.
            context.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS "Users" (
                    "Id"           SERIAL PRIMARY KEY,
                    "Username"     TEXT NOT NULL UNIQUE,
                    "PasswordHash" TEXT NOT NULL,
                    "Role"         TEXT NOT NULL DEFAULT 'Student',
                    "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW()
                )
                """);

            if (context.Users.Any())
            {
                return; // DB already seeded
            }

            var hasher = new PasswordHasher<User>();

            var admin = new User
            {
                Username = "admin",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };
            admin.PasswordHash = hasher.HashPassword(admin, "admin123");
            context.Users.Add(admin);

            var defaultStudents = new[] { "lukre", "ema", "karina", "tadas", "valentinas" };
            foreach (var username in defaultStudents)
            {
                var student = new User
                {
                    Username = username,
                    Role = "Student",
                    CreatedAt = DateTime.UtcNow
                };
                student.PasswordHash = hasher.HashPassword(student, "vbe2027");
                context.Users.Add(student);
            }

            context.SaveChanges();
        }
    }
}
