﻿using System;
using System.IO;
using Atomex.Core;
using LiteDB;
using Serilog;

namespace Atomex.LiteDb
{
    public static class LiteDbMigrationManager
    {
        public static void Migrate(
            string pathToDb,
            string sessionPassword,
            Network network)
        {
            try
            {
                if (!File.Exists(pathToDb))
                {
                    CreateDataBase(
                        pathToDb: pathToDb,
                        sessionPassword: sessionPassword,
                        targetVersion: LiteDbMigrations.Version2);

                    return;
                }

                var currentVersion = GetDataBaseVersion(pathToDb, sessionPassword);

                if (currentVersion == LiteDbMigrations.Version0)
                    currentVersion = LiteDbMigrations.MigrateFrom_0_to_1(pathToDb, sessionPassword);

                if (currentVersion == LiteDbMigrations.Version1)
                    LiteDbMigrations.MigrateFrom_1_to_2(pathToDb, sessionPassword, network);
            }
            catch (Exception e)
            {
                Log.Error(e, "LiteDb migration error");
            }
        }

        private static ushort GetDataBaseVersion(
            string pathToDb,
            string sessionPassword)
        {
            using (var db = new LiteDatabase($"FileName={pathToDb};Password={sessionPassword}"))
            {
                return db.Engine.UserVersion;
            }
        }

        private static void CreateDataBase(
            string pathToDb,
            string sessionPassword,
            ushort targetVersion)
        {
            using (var db = new LiteDatabase($"FileName={pathToDb};Password={sessionPassword}"))
            {
                db.Engine.UserVersion = targetVersion;
            }
        }
    }
}