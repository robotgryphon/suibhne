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
    public class LocationManager {

        public static bool RenameLocation(Guid id, string name) {

            // Update in database
            if (Core.Database == null || GetLocationInfo(id).Key == Guid.Empty) {
                // Location does not exist, or database not ready
                return false;
            }

            try {
                Core.Database.Open();

                SQLiteCommand update = Core.Database.CreateCommand();
                update.CommandText = "UPDATE Identifiers SET Name='" + name + "' WHERE Identifier='" + id + "';";
                int result = update.ExecuteNonQuery();

                if (result == 1) {
                    return true;
                }

            }

            catch (Exception) { }

            finally {
                Core.Database.Close();
            }

            return false;
        }

        public static void AddNewNetwork(Guid id, String name) {

            if (Core.Database == null)
                return;

            try {
                Core.Database.Open();

                SQLiteCommand command = Core.Database.CreateCommand();
                command.CommandText = "INSERT INTO Identifiers VALUES ('" + id.ToString() + "', '', '" + name + "', 1);";
                command.ExecuteNonQuery();

                NetworkBot b = new NetworkBot(Core.ConfigDirectory + "/Networks/" + id);
                Core.Networks.Add(id, b);
            }

            catch (Exception) { }

            finally {
                Core.Database.Close();
            }

        }

        /// <summary>
        /// Deletes a location and all child locations.
        /// </summary>
        /// <param name="id"></param>
        public static void DeleteLocation(Guid id) {
            if (Core.Database == null)
                return;

            try {
                Core.Database.Open();

                if (Directory.Exists(Core.ConfigDirectory + "/Networks/" + id))
                    Directory.Delete(Core.ConfigDirectory + "/Networks/" + id, true);
                else {
                    KeyValuePair<Guid, Location> parentID = GetLocationInfo(id);
                    if (Directory.Exists(Core.ConfigDirectory + "/Networks/" + parentID.Key + "/" + id))
                        Directory.Delete(Core.ConfigDirectory + "/Networks/" + parentID.Key + "/" + id, true);
                }

                SQLiteCommand command = Core.Database.CreateCommand();
                command.CommandText = "DELETE FROM Identifiers WHERE Identifier = '" + id + "' OR ParentId = '" + id + "';";
                command.ExecuteNonQuery();

                if (Core.Networks.ContainsKey(id))
                    Core.Networks.Remove(id);

            }

            catch (Exception) { }

            finally {
                Core.Database.Close();
            }
        }

        public static DataTable GetChildLocations(Guid parent) {
            if (Core.Database == null) {
                // This shouldn't happen- the ValidateDatabase should be creating the table. But just in case..
                return null;
            }

            try {
                Core.Database.Open();
                DataTable resultsTable = new DataTable();
                SQLiteCommand c = Core.Database.CreateCommand();
                c.CommandText = "SELECT * FROM Identifiers WHERE ParentId = '" + parent + "';";

                SQLiteDataReader resultsReader = c.ExecuteReader();
                resultsTable.Load(resultsReader);

                return resultsTable;
            }

            catch (Exception) {
                return null;
            }

            finally {
                Core.Database.Close();
            }
        }

        /// <summary>
        /// Gets a location based on a name and a parent's identifier.
        /// </summary>
        /// <param name="parent">The parent location's ID.</param>
        /// <param name="location">The location to look up.</param>
        /// <returns></returns>
        public static KeyValuePair<Guid, Location> GetLocationInfo(Guid parent, String location) {
            Location returned = new Location(""); 
            
            if (Core.Database == null) {
                // This shouldn't happen- the ValidateDatabase should be creating the table. But just in case..
                return new KeyValuePair<Guid, Location>(Guid.Empty, null);
            }

            try {
                Core.Database.Open();
                DataTable resultsTable = new DataTable();
                SQLiteCommand c = Core.Database.CreateCommand();
                c.CommandText = "SELECT * FROM Identifiers WHERE ParentId = '" + parent + "' AND lower(Name)='" + location.ToLower() + "';";

                SQLiteDataReader resultsReader = c.ExecuteReader();
                resultsTable.Load(resultsReader);

                if (resultsTable.Rows.Count > 0) {
                    DataRow result = resultsTable.Rows[0];
                    returned.Name = result["Name"].ToString();
                    returned.Parent = Guid.Parse(result["ParentId"].ToString());

                    byte locationType = (byte)result["LocationType"];
                    returned.Type = (Reference.LocationType)locationType;

                    return new KeyValuePair<Guid, Location>(Guid.Parse(result["Identifier"].ToString()), returned);
                } else
                    return new KeyValuePair<Guid,Location>(Guid.Empty, null);
            }

            catch (Exception) {
                return new KeyValuePair<Guid, Location>(Guid.Empty, null);
            }

            finally {
                Core.Database.Close();
            }
        }

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
                    if(result["ParentId"].ToString() != "")
                        returned.Parent = Guid.Parse(result["ParentId"].ToString());
                    
                    returned.Type = (Reference.LocationType) byte.Parse(result["LocationType"].ToString());
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
    }
}
