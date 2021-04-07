This project is used to generate the items seed for the [Valhalla Loot List](https://github.com/Elite747/ValhallaLootList) web app database. A World of Warcraft world database is used to generate this. (Not provided) If you have this database running on MySql, you can set the connection string to it using the following command:

``` 
dotnet user-secrets set "ConnectionStrings:WowConnection" "<WoW world DB connection string>" -p src/server
```

Once set, you can generate the `seed.items.json` using the following command:

``` 
dotnet run -p src/ItemImporter
```
