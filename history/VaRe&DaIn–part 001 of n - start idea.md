I wanted a proof-of-concept app that is truly database-independent: SQL Server, PostgreSQL, MongoDB, or just a local SQLite file. Partly for fun, partly for the engineering challenge, and mostly because real enterprise installs rarely have the same infrastructure. That is the **DaIn (Database Independence)** goal behind this project.

That idea became **Vacation Relay (VaRe)**: when someone is on PTO/OOO, the app records who is covering them and for what time interval. Other apps can query that API and route responsibility correctly, regardless of which database the company uses.

The artifacts will be a nuget and an application . And ,by the way, : VR&DI are already taken 

Project: https://github.com/ignatandrei/VacationRelay
