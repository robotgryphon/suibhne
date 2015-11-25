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
            if (Core.Database == null || GetLocationInfo(id) == null) {
                // Location does not exist, or database not ready
                return false;
            }

            try {
                if(Core.Database.State != ConnectionState.Open)
                    Core.Database.Open();

                SQLiteCommand update = Core.Database.CreateCommand();
                update.CommandText = "UPDATE Identifiers SET Name='" + name + "' WHERE Identifier='" + id + "';";
                int result = update.ExecuteNonQuery();

                if (result == 1) {
                    return true;
                }

            }

            catch (Exception ex) {
                Core.Log(ex.Message);
            }

            finally {
                Core.Database.Close();
            }

            return false;
        }

        public static Guid[] GetNetworks() {
            if (Core.Database == null)
                return null;

            try {
                Core.Database.Open();

                List<Guid> networks = new List<Guid>();
                SQLiteCommand command = Core.Database.CreateCommand();
                command.CommandText = "SELECT * FROM Identifiers WHERE LocationType=1;";

                DataTable resultsTable = new DataTable();
                SQLiteDataReader resultsReader = command.ExecuteReader();
                resultsTable.Load(resultsReader);
                
                foreach(DataRow dr in resultsTable.Rows) {
                    networks.Add(Guid.Parse(dr["Identifier"] as String));
                }

                return networks.ToArray();
            }

            catch (Exception) {
                return new Guid[0];
            }

            finally {
                Core.Database.Close();
            }
        }

        public static void AddNewLocation(Guid parent, Guid id, String name) {
            if (Core.Database == null)
                return;

            try {
                Core.Database.Open();

                SQLiteCommand command = Core.Database.CreateCommand();
                command.CommandText = "INSERT INTO Identifiers VALUES ('" + id.ToString() + "', '" + parent.ToString() + "', '" + name + "', 2);";
                command.ExecuteNonQuery();

                Directory.CreateDirectory(Core.ConfigDirectory + "/Networks/" + parent + "/Locations/" + id);
            }

            catch (Exception) { }

            finally {
                Core.Database.Close();
            }
        }

        /// <summary>
        /// Creates a new network.
        /// </summary>
        /// <param name="id">The new network's identifier.</param>
        /// <param name="name">The new network's name.</param>
        public static void AddNewNetwork(Guid id, String type, String name) {

            if (Core.Database == null)
                return;

            try {
                Core.Database.Open();

                SQLiteCommand command = Core.Database.CreateCommand();
                command.CommandText = "INSERT INTO Identifiers VALUES ('" + id.ToString() + "', '', '" + name + "', 1);";
                command.ExecuteNonQuery();

                Directory.CreateDirectory(Core.ConfigDirectory + "/Networks/" + id);
                Directory.CreateDirectory(Core.ConfigDirectory + "/Networks/" + id + "/Locations");

                
            }

            catch (Exception) {
                
            }

            finally {
                Core.Database.Close();
            }

            #region Creating basic network file
            try {
                String newConfigFile = Core.ConfigDirectory + "/Networks/" + id + "/network.ini";

                IniConfigSource config = new IniConfigSource();

                config.AddConfig("Network");
                config.Configs["Network"].Set("type", type);
                config.Save(newConfigFile);
            }

            catch(Exception e) {
                Core.Log(e.Message, LogType.ERROR);
            }
            #endregion
        }

        /// <summary>
        /// Deletes a location and all child locations.
        /// </summary>
        /// <param name="id"></param>
        public static void DeleteLocation(Guid id) {
            if (Core.Database == null)
                return;

            try {
                if (Directory.Exists(@Core.ConfigDirectory + @"Networks/" + id))
                    Directory.Delete(Core.ConfigDirectory + @"Networks/" + id, true);
                else {
                    Location location = GetLocationInfo(id);
                    String dir = Core.ConfigDirectory + @"Networks/" + location.Parent + @"/Locations/" + id;

                    if(Directory.Exists(dir))
                        Directory.Delete(dir, true);
                }
            }

            catch(Exception) {

            }

            try {
                if(Core.Database.State != ConnectionState.Open)
                    Core.Database.Open();

                SQLiteCommand command = Core.Database.CreateCommand();
                command.CommandText = "DELETE FROM Identifiers WHERE Identifier = '" + id + "' OR ParentId = '" + id + "';";
                command.ExecuteNonQuery();

                if (Core.Networks.ContainsKey(id))
                    Core.Networks.Remove(id);

            }

            catch (Exception e) {
                Core.Log(e.StackTrace);
                Core.Log(e.Message);
            }

            finally {
                Core.Database.Close();
            }
        }

        public static Dictionary<Guid, Location> GetChildLocations(Guid parent) {
            if (Core.Database == null) {
                // This shouldn't happen- the ValidateDatabase should be creating the table. But just in case..
                return null;
            }

            try {
                Dictionary<Guid, Location> children = new Dictionary<Guid, Location>();

                if(Core.Database.State != ConnectionState.Open)
                    Core.Database.Open();

                DataTable resultsTable = new DataTable();
                SQLiteCommand c = Core.Database.CreateCommand();
                c.CommandText = "SELECT * FROM Identifiers WHERE ParentId = '" + parent + "';";

                SQLiteDataReader resultsReader = c.ExecuteReader();
                resultsTable.Load(resultsReader);

                foreach (DataRow row in resultsTable.Rows) {
                    Location l = new Location("location");
                    Guid id = Guid.Parse(row["Identifier"].ToString());
                    l.Name = row["Name"].ToString();
                    l.Parent = parent;

                    int locationType = int.Parse(row["LocationType"].ToString());
                    l.Type = (Reference.LocationType)((byte)locationType);

                    children.Add(id, l);
                }

                return children;
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

                    int locationType = int.Parse(result["LocationType"].ToString());
                    returned.Type = (Reference.LocationType) ((byte) locationType);

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

        public static Location GetLocationInfo(Guid id) {

            Location returned = new Location("");

            if (Core.Database == null) {
                // This shouldn't happen- the ValidateDatabase should be creating the table. But just in case..
                return null;
            }

            try {
                if(Core.Database.State != ConnectionState.Open) Core.Database.Open();
                DataTable resultsTable = new DataTable();
                SQLiteCommand c = Core.Database.CreateCommand();
                c.CommandText = "SELECT * FROM Identifiers WHERE Identifier = '" + id + "';";

                SQLiteDataReader resultsReader = c.ExecuteReader();
                resultsTable.Load(resultsReader);

                if (resultsTable.Rows.Count > 0) {
                    DataRow result = resultsTable.Rows[0];
                    returned.Name = result["Name"].ToString();
                    if (result["ParentId"].ToString() != "")
                        returned.Parent = Guid.Parse(result["ParentId"].ToString());

                    int locationType = int.Parse(result["LocationType"].ToString());
                    returned.Type = (Reference.LocationType)((byte)locationType);
                } else
                    return null;
            }

            catch (Exception) {

            }

            finally {
                Core.Database.Close();
            }

            return returned;
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
                Location l = GetLocationInfo(id);
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
