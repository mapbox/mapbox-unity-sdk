using SQLite4Unity3d;
using UnityEngine;
using System;
#if !UNITY_EDITOR
using System.Collections;
using System.IO;
#endif
using System.Collections.Generic;


namespace Mapbox.Platform.SQLite
{
	public class SQLiteDataService : IDisposable
	{

		private SQLiteConnection _connection;
		private bool _disposed = false;


		public SQLiteDataService(string DatabaseName)
		{

#if UNITY_EDITOR
			var dbPath = string.Format(@"Assets/StreamingAssets/{0}", DatabaseName);
#else
        // check if file exists in Application.persistentDataPath
        var filepath = string.Format("{0}/{1}", Application.persistentDataPath, DatabaseName);

        if (!File.Exists(filepath))
        {
            Debug.Log("Database not in Persistent path");
            // if it doesn't ->
            // open StreamingAssets directory and load the db ->

#if UNITY_ANDROID
            var loadDb = new WWW("jar:file://" + Application.dataPath + "!/assets/" + DatabaseName);  // this is the path to your StreamingAssets in android
            while (!loadDb.isDone) { }  // CAREFUL here, for safety reasons you shouldn't let this while loop unattended, place a timer and error check
            // then save to Application.persistentDataPath
            File.WriteAllBytes(filepath, loadDb.bytes);
#elif UNITY_IOS
                 var loadDb = Application.dataPath + "/Raw/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy(loadDb, filepath);
#elif UNITY_WP8
                var loadDb = Application.dataPath + "/StreamingAssets/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy(loadDb, filepath);

#elif UNITY_WINRT
		var loadDb = Application.dataPath + "/StreamingAssets/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
		// then save to Application.persistentDataPath
		File.Copy(loadDb, filepath);
#else
	var loadDb = Application.dataPath + "/StreamingAssets/" + DatabaseName;  // this is the path to your StreamingAssets in iOS
	// then save to Application.persistentDataPath
	File.Copy(loadDb, filepath);

#endif

            Debug.Log("Database written");
        }

        var dbPath = filepath;
#endif
			_connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
			Debug.Log("Final PATH: " + dbPath);

		}



		~SQLiteDataService()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposeManagedResources)
		{
			if (!_disposed)
			{
				if (disposeManagedResources)
				{
					if (null != _connection)
					{
						_connection.Close();
						_connection.Dispose();
						_connection = null;
					}
				}
				_disposed = true;
			}
		}


		public TableQuery<T> Table<T>() where T : new()
		{
			return _connection.Table<T>();
		}


		public int InsertAll(System.Collections.IEnumerable objects)
		{
			return _connection.InsertAll(objects);
		}


		//public void CreateDB()
		//{
		//	_connection..DropTable<Person>();
		//	_connection.CreateTable<Person>();

		//	_connection.InsertAll(new[]{
		//	new Person{
		//		Id = 1,
		//		Name = "Tom",
		//		Surname = "Perez",
		//		Age = 56
		//	},
		//	new Person{
		//		Id = 2,
		//		Name = "Fred",
		//		Surname = "Arthurson",
		//		Age = 16
		//	},
		//	new Person{
		//		Id = 3,
		//		Name = "John",
		//		Surname = "Doe",
		//		Age = 25
		//	},
		//	new Person{
		//		Id = 4,
		//		Name = "Roberto",
		//		Surname = "Huertas",
		//		Age = 37
		//	}
		//});
		//}

		//public IEnumerable<Person> GetPersons()
		//{
		//	return _connection.Table<Person>();
		//}

		//public IEnumerable<Person> GetPersonsNamedRoberto()
		//{
		//	return _connection.Table<Person>().Where(x => x.Name == "Roberto");
		//}

		//public Person GetJohnny()
		//{
		//	return _connection.Table<Person>().Where(x => x.Name == "Johnny").FirstOrDefault();
		//}

		//public Person CreatePerson()
		//{
		//	var p = new Person
		//	{
		//		Name = "Johnny",
		//		Surname = "Mnemonic",
		//		Age = 21
		//	};
		//	_connection.Insert(p);
		//	return p;
		//}
	}
}