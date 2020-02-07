using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MySql.Data.MySqlClient; 

public class MySqlDB : MonoBehaviour
{
    MySqlConnection connection; 

    public void Init() {
        if (connection == null) { 
            //string connectionString = "SERVER=127.0.0.1;" + "DATABASE=unity;" + "UID=unity;" + "PASSWORD=unity;"; 
            string connectionString = "data source=127.0.0.1;database=unity; uid=unity;pwd=unity;Allow User Variables=True;"; 

            try { 
                connection = new MySqlConnection(connectionString); 
                connection.Open(); 
                Debug.Log("database has been initiliazed");
            } catch (MySqlException ex) { 
                Debug.LogError("MySQL Error: " + ex.ToString()); 
            } 
        } 
    }

    public void Shutdown() {
       if (connection != null) { 
          connection.Close(); 
       } 
    }


}
