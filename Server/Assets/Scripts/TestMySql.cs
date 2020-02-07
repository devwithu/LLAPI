using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MySql.Data.MySqlClient; 

public class TestMySql : MonoBehaviour
{
    public static MySqlConnection connection; 

    // Use this for initialization 
    void Start () { 
      SetupSQLConnection(); 
      TestDB(); 
      CloseSQLConnection(); 
    } 

    private void SetupSQLConnection() { 
      if (connection == null) { 
        //string connectionString = "SERVER=127.0.0.1;" + "DATABASE=unity;" + "UID=unity;" + "PASSWORD=unity;"; 
        string connectionString = "data source=127.0.0.1;database=unity; uid=unity;pwd=unity;Allow User Variables=True;"; 

        try { 
          connection = new MySqlConnection(connectionString); 
          connection.Open(); 
        } catch (MySqlException ex) { 
          Debug.LogError("MySQL Error: " + ex.ToString()); 
        } 
      } 
    } 

    private void CloseSQLConnection() { 
      if (connection != null) { 
        connection.Close(); 
      } 
    } 

    public void TestDB() { 
      string commandText = string.Format("INSERT INTO scores (playername, playerscore) VALUES ({0}, {1})", "'megaplayer'", 10); 
      if (connection != null) { 
        MySqlCommand command = connection.CreateCommand(); 
        command.CommandText = commandText; 
        try { 
          command.ExecuteNonQuery(); 
        } catch (System.Exception ex) { 
          Debug.LogError("MySQL error: " + ex.ToString()); 
        } 
      } 
    } 
}