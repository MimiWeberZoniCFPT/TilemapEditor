﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/**
 * Project      : Tilemap Editor
 * Description  : A C# program where you can modify and create tilesets and tilemaps with an access to a database
 * File         : DbManager.cs
 * Author       : Weber Jamie
 * Date         : 13 October 2023
**/
namespace TilemapEditor
{
    /// <summary>
    /// The class that manages the discussions with the database
    /// </summary>
    public class DbManager
    {
        /// <summary>
        /// The connection to the database
        /// </summary>
        private MySqlConnection connection;

        /// <summary>
        /// The instance of the class
        /// </summary>
        private static DbManager? instance;

        /// <summary>
        /// The constructor of the class
        /// Create the connection to the database and open it
        /// </summary>
        private DbManager()
        {
            string server = "localhost";
            string database = "TilemapEditor";
            string uid = "dbTilemapEditor";
            string password = "tilePassword";
            string connectionString = "SERVER=" + server + ";DATABASE=" + database + ";UID=" + uid + ";PASSWORD=" + password + ";";
            connection = new MySqlConnection(connectionString);
            connection.Open();
        }

        /// <summary>
        /// The singleton pattern to get only one instance of this class
        /// </summary>
        public static DbManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DbManager();
                }
                return instance;
            }
        }

        /// <summary>
        /// If a SELECT request is needed, call this function
        /// Get the requested data from the database by using the sql query given in parameter and put it in a DataTable
        /// </summary>
        /// <param name="sql">The sql query to execute</param>
        /// <param name="options">The options if needed</param>
        /// <returns>A datatable of the data returned from the database</returns>
        private DataTable GetTable(string sql, Dictionary<string, (object, MySqlDbType)>? options = null)
        {
            MySqlCommand query = new(sql, connection);
            if (options != null)
            {
                foreach (KeyValuePair<string, (object, MySqlDbType)> option in options)
                {
                    query.Parameters.Add(option.Key, option.Value.Item2).Value = option.Value.Item1;
                }
            }
            MySqlDataAdapter adapter = new MySqlDataAdapter(query);
            DataTable table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        /// <summary>
        /// If an Update or Insert request is needed, call this function
        /// Change the data from the database
        /// </summary>
        /// <param name="sql">The sql query to execute</param>
        /// <param name="options">The options if needed</param>
        private void ChangeDatabase(string sql, Dictionary<string, (object, MySqlDbType)>? options = null)
        {
            MySqlCommand query = new(sql, connection);
            if (options != null)
            {
                foreach(KeyValuePair<string, (object, MySqlDbType)> option in options)
                {
                    query.Parameters.Add(option.Key, option.Value.Item2).Value = option.Value.Item1;
                }
            }
            query.ExecuteNonQuery();
        }

        /// <summary>
        /// Get the list of tilesets from the database
        /// </summary>
        public DataTable Tilesets { get { return GetTable("SELECT Tilesets.idTileset AS 'Id', Tilesets.name AS 'Nom', COUNT(Tiles.idTileset) as 'Tiles' FROM Tilesets, Tiles WHERE Tilesets.idTileset = Tiles.idTileset GROUP BY Tilesets.idTileset"); } }

        /// <summary>
        /// Get the list of tilemaps from the database
        /// </summary>
        public DataTable Tilemaps { get { return GetTable("SELECT Tilemaps.idTilemap AS 'Id', Tilemaps.name AS 'Nom', Tilesets.name AS 'Tileset' FROM Tilemaps, Tilesets WHERE Tilemaps.idTileset = Tilesets.idTileset;"); } }


        /// <summary>
        /// Get a tileset depending on the given id
        /// </summary>
        /// <param name="id">The id of the tileset</param>
        /// <returns>A Tileset instance</returns>
        public Tileset GetTileset(int id)
        {
            DataRow tileset = GetTable($"SELECT * FROM Tilesets WHERE idTileset = {id};").Rows[0];
            DataTable tiles = GetTable($"SELECT image, number FROM Tiles WHERE idTileset = {id} ORDER BY number;");
            return new Tileset((int)tileset["idTileset"], (string)tileset["name"], tiles);
        }

        /// <summary>
        /// Get a tilemap depending on the given id
        /// </summary>
        /// <param name="id">The id of the tilemap</param>
        /// <returns>A tilemap instance</returns>
        public Tilemap GetTilemap(int id)
        {
            DataRow tilemap = GetTable($"SELECT * FROM Tilemaps WHERE idTilemap = {id};").Rows[0];
            Tileset tileset = GetTileset((int)tilemap["idTileset"]);
            DataTable tiles = GetTable($"SELECT posX, posY, number FROM TilesPosition WHERE idTilemap = {id} ORDER BY posX, posY;");
            return new Tilemap((int)tilemap["idTilemap"], (string)tilemap["name"], tileset, tiles);
        }

        /// <summary>
        /// Create a new tileset for the database
        /// </summary>
        /// <param name="name">The name of the tileset</param>
        /// <param name="tile">The base tile of the tileset</param>
        public void AddTileset(string name, Bitmap tile)
        {
            MemoryStream ms = new MemoryStream();
            tile.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ChangeDatabase("INSERT INTO Tilesets (name) VALUES (@name);", new Dictionary<string, (object, MySqlDbType)>() { ["@name"] = (name, MySqlDbType.VarChar)});
            int id = (int)(GetTable("SELECT Tilesets.idTileset AS 'id' FROM Tilesets WHERE Tilesets.name = @name;", new Dictionary<string, (object, MySqlDbType)>() { ["@name"] = (name, MySqlDbType.VarChar) }).Rows[0]["id"]);
            Dictionary<string, (object, MySqlDbType)> opt = new Dictionary<string, (object, MySqlDbType)>() {
                ["@img"] = (ms.ToArray(), MySqlDbType.LongBlob),
                ["@id"] = (Convert.ToString(id), MySqlDbType.Int32)
            };
            ChangeDatabase("INSERT INTO Tiles (number, image, idTileset) VALUES (0, @img, @id);", opt);
        }
    }
}
