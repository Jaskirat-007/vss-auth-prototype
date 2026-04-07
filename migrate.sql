CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407195641_InitialCreate') THEN
    CREATE TABLE IF NOT EXISTS users (
        "Id" uuid NOT NULL,
        "TenantId" uuid,
        "UserName" character varying(256) NOT NULL,
        "NormalizedUserName" character varying(256),
        "Name" character varying(256),
        "Surname" character varying(256),
        "Email" character varying(256) NOT NULL,
        "NormalizedEmail" character varying(256),
        "EmailConfirmed" boolean NOT NULL,
        "PasswordHash" character varying(256),
        "SecurityStamp" character varying(128),
        "IsActive" boolean NOT NULL,
        "PhoneNumber" character varying(16),
        "PhoneNumberConfirmed" boolean NOT NULL,
        "TwoFactorEnabled" boolean NOT NULL,
        "LockoutEnd" timestamp with time zone,
        "LockoutEnabled" boolean NOT NULL,
        "AccessFailedCount" integer NOT NULL,
        "ShouldChangePasswordOnNextLogin" boolean NOT NULL,
        "EntityVersion" uuid,
        "ExtraProperties" text,
        "LastPasswordChangeTime" timestamp with time zone,
        CONSTRAINT "PK_users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407195641_InitialCreate') THEN
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_streams_Slug" ON streams ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407195641_InitialCreate') THEN
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_Email" ON users ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407195641_InitialCreate') THEN
    CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_UserName" ON users ("UserName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260407195641_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260407195641_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;