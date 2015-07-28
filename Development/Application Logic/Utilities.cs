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
using Ostenvighx.Suibhne.Networks.Base;

namespace Ostenvighx.Suibhne {
    public class Utilities {

        public static KeyValuePair<Guid, Location> GetLocationInfo(Guid id) {

            Location returned = new Location("");

            if (Core.Database == null) {
                // This shouldn't happen- the ValidateDatabase should be creating the table. But just in case..
                return new KeyValuePair<Guid, Location>(Guid.Empty, null);
            }

            try {
                Core.Database.Open();
                DataTable resultsTable = new DataTable();
                SQLiteCommand c = Core.Database.CreateCommand();
                c.CommandText = "SELECT * FROM Identifiers WHERE Identifier = '" + id + "';";

                SQLiteDataReader resultsReader = c.ExecuteReader();
                resultsTable.Load(resultsReader);

                if (resultsTable.Rows.Count > 0) {
                    DataRow result = resultsTable.Rows[0];
                    returned.Name = result["Name"].ToString();
                    returned.Parent = Guid.Parse(result["ParentId"].ToString());
                    returned.Password = result["Password"].ToString();
                } else
                    returned = null;
            }

            catch (Exception) {

            }

            finally {
                Core.Database.Close();
            }

            return new KeyValuePair<Guid,Location>(id, returned);
        }

        public static KeyValuePair<Guid, Location> GetLocationInfo(string network, string location = "") {
            if (Core.Database == null) {
                // This shouldn't happen- the ValidateDatabase should be creating the table. But just in case..
                return new KeyValuePair<Guid,Location>(Guid.Empty, null);
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

                Guid id = Guid.Parse( resultsTable.Rows[0]["Identifier"].ToString() );
                Location l = GetLocationInfo(id).Value;
                KeyValuePair<Guid, Location> returned = new KeyValuePair<Guid,Location>(id, l);

                return returned;
            }

            catch (Exception) {

            }

            finally {
                Core.Database.Close();
            }

            return new KeyValuePair<Guid, Location>(Guid.Empty, null);
        }

        public static void SaveToSystemFile(JObject j) {
            String converted = Convert.ToBase64String(Encoding.UTF8.GetBytes(j.ToString()));
            File.WriteAllText(Core.ConfigDirectory + "/system.sns", converted);
        }
    }
}
