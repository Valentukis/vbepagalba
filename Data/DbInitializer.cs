using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VbeSort.Models;

namespace VbeSort.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        var provider = context.Database.ProviderName ?? string.Empty;
        var isPostgres = provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);

        if (isPostgres)
        {
            InitializePostgreSqlSchema(context);
        }
        else
        {
            context.Database.EnsureCreated();
        }

        SeedDefaultUsers(context);
    }

    private static void InitializePostgreSqlSchema(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Users" (
                "Id"           SERIAL PRIMARY KEY,
                "Username"     TEXT NOT NULL UNIQUE,
                "PasswordHash" TEXT NOT NULL,
                "Role"         TEXT NOT NULL DEFAULT 'Student',
                "DisplayName"  TEXT NOT NULL DEFAULT '',
                "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "DisplayName" TEXT NOT NULL DEFAULT '';

            CREATE TABLE IF NOT EXISTS "ExerciseProgresses" (
                "Id"               SERIAL PRIMARY KEY,
                "UserId"           INTEGER NOT NULL,
                "ExerciseId"       TEXT NOT NULL,
                "TopicName"        TEXT NOT NULL DEFAULT '',
                "Status"           INTEGER NOT NULL DEFAULT 0,
                "IsBookmarked"     BOOLEAN NOT NULL DEFAULT FALSE,
                "PersonalNote"     TEXT NOT NULL DEFAULT '',
                "AttemptCount"     INTEGER NOT NULL DEFAULT 0,
                "CorrectCount"     INTEGER NOT NULL DEFAULT 0,
                "TotalTimeSeconds" INTEGER NOT NULL DEFAULT 0,
                "LastAttemptAtUtc" TIMESTAMPTZ NULL,
                "UpdatedAtUtc"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                CONSTRAINT "UQ_ExerciseProgresses_User_Ex" UNIQUE ("UserId", "ExerciseId")
            );

            CREATE TABLE IF NOT EXISTS "ExamAttempts" (
                "Id"                SERIAL PRIMARY KEY,
                "UserId"            INTEGER NOT NULL,
                "Title"             TEXT NOT NULL DEFAULT 'Bandomasis egzaminas',
                "Mode"              TEXT NOT NULL DEFAULT 'custom',
                "TotalQuestions"    INTEGER NOT NULL DEFAULT 0,
                "SolvedCorrectly"   INTEGER NOT NULL DEFAULT 0,
                "PartiallySolved"   INTEGER NOT NULL DEFAULT 0,
                "TotalPointsEarned" INTEGER NOT NULL DEFAULT 0,
                "MaxPossiblePoints" INTEGER NOT NULL DEFAULT 0,
                "DurationSeconds"   INTEGER NOT NULL DEFAULT 0,
                "TopicsCsv"         TEXT NOT NULL DEFAULT '',
                "DetailsJson"       TEXT NOT NULL DEFAULT '[]',
                "StartedAtUtc"      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                "CompletedAtUtc"    TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """);
    }

    private static void SeedDefaultUsers(AppDbContext context)
    {
        if (context.Users.Any())
        {
            return;
        }

        var hasher = new PasswordHasher<User>();

        var admin = new User
        {
            Username = "admin",
            DisplayName = "Administratorius",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, "admin123");
        context.Users.Add(admin);

        var defaultStudents = new (string Username, string Display)[]
        {
            ("lukre", "Lukrėcija"),
            ("ema", "Ema"),
            ("karina", "Karina"),
            ("tadas", "Tadas"),
            ("valentinas", "Valentinas")
        };

        foreach (var (username, display) in defaultStudents)
        {
            var student = new User
            {
                Username = username,
                DisplayName = display,
                Role = "Student",
                CreatedAt = DateTime.UtcNow
            };
            student.PasswordHash = hasher.HashPassword(student, "vbe2027");
            context.Users.Add(student);
        }

        context.SaveChanges();
    }
}
