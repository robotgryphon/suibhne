using Nini.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json.Linq;

using System.Data;
using System.Data.SQLite;

namespace Ostenvighx.Suibhne {
    public class Utilities {

        public static DataRow GetLocationEntry(Guid id) {
            if (Core.Database == null) {
                // This shouldn't happen- the ValidateDatabase should be creating the table. But just in case..
                return null;
            }

            try {
                Core.Database.Open();
                DataTable resultsTable = new DataTable();
                SQLiteCommand c = Core.Database.CreateCommand();
                c.CommandText = "SELECT * FROM Identifiers WHERE Identifier = '" + id + "';";

                SQLiteDataReader resultsReader = c.ExecuteReader();
                resultsTable.Load(resultsReader);

                return resultsTable.Rows[0];
            }

            catch (Exception e) {

            }

            finally {
                Core.Database.Close();
            }

            return null;
        }

        public static DataRow GetLocationEntry(string network, string location = "") {
            if (Core.Database == null) {
                // This shouldn't happen- the ValidateDatabase should be creating the table. But just in case..
                return null;
            }

            try {
                Core.Database.Open();
                DataTable resultsTable = new DataTable();
                SQLiteCommand c = Core.Database.CreateCommand();

                if (location != "") {
                    c.CommandText = "SELECT * FROM Identifiers " +
	                    "WHERE Identifiers.ParentId IN (SELECT Identifier FROM Identifiers WHERE lower(Name) = '" + network.ToLower() + "')" +
                        " AND lower(Name) = '" + location.ToLower() + "';";
                } else {
                    c.CommandText = "SELECT * FROM Identifiers WHERE lower(Name) = '" + network.ToLower() + "';";
                }


                SQLiteDataReader resultsReader = c.ExecuteReader();
                resultsTable.Load(resultsReader);

                return resultsTable.Rows[0];
            }

            catch (Exception e) {

            }

            finally {
                Core.Database.Close();
            }

            return null;
        }

        public static void SaveToSystemFile(JObject j) {
            String converted = Convert.ToBase64String(Encoding.UTF8.GetBytes(j.ToString()));
            File.WriteAllText(Core.ConfigDirectory + "/system.sns", converted);
        }
    }
}
