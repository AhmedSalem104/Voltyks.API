-- Check if the AddAdminRoleProtection migration was applied
SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId LIKE '%AddAdminRoleProtection%';

-- Check if the trigger exists
SELECT name FROM sys.triggers WHERE name = 'TR_BlockDirectAdminRoleInsert';

-- Check if voltyksapp user exists
SELECT name FROM sys.database_principals WHERE name = 'voltyksapp';

-- Check if HumanAccessRole exists
SELECT name FROM sys.database_principals WHERE name = 'HumanAccessRole';
