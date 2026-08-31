Now comes the real part. I want the app to work with any database—SQLite, PostgreSQL, SQL Server, and MongoDB—and to choose the database type and connection string at startup.

In the .NET world, I see two main options:

1. The plain SQL-style approach: I define an interface with methods for the data I need in the app. This is a bit complicated because MongoDB does not expose a DBConnection implementation the way relational databases(SqlServer) do; instead, it uses a MongoClient. In practice, that means I share the app-facing logic while keeping the database-specific implementation behind the interface. ( Dapper will not help )

2. The EF Core approach with different providers (see https://learn.microsoft.com/en-us/ef/core/providers/?tabs=dotnet-core-cli). This would let the application reuse a database-independent implementation by working through DbContext and DbSets. It feels cleaner and more natural for the app, although the POCO model may need a slightly different design for MongoDB, as described here: https://kevsoft.net/2025/04/03/decoupling-mongo-dbs-object-id-in-csharp-with-entity-framework-core.html

For me, option 2 feels clearer and more maintainable, even though option 1 makes me think more carefully about the app design before I commit to a solution. But honestly, I like to keep my options open for a bit longer before deciding.
