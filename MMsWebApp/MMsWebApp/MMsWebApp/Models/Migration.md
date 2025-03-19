# Migration Instructions

## Prerequisites
- Visual Studio with .NET Core SDK installed
- Entity Framework Core tools installed

## Steps to Create and Apply a Migration

1. Open Visual Studio and load your project.

2. Open the Package Manager Console:
   - Go to `Tools` > `NuGet Package Manager` > `Package Manager Console`.

3. Add a new migration:
   - In the Package Manager Console, run the following command:
     ```
     Add-Migration AddCollectedAtColumn
     ```

4. Apply the migration to update the database schema:
   - In the Package Manager Console, run the following command:
     ```
     Update-Database
     ```

5. Verify that the migration was applied successfully by checking the database schema.

## Troubleshooting
- Ensure that the `DbContext` is correctly configured in your project.
- Check for any typos or errors in the migration commands.
- If you encounter any issues, refer to the Entity Framework Core documentation for additional guidance.