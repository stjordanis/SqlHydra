﻿open SqlHydra
open SqlHydra.Sqlite
open Schema

let app = 
    {
        Console.AppInfo.Name = "SqlHydra.Sqlite"
        Console.AppInfo.Command = "sqlhydra-sqlite"
        Console.AppInfo.DefaultReaderType = "System.Data.IDataReader"
        Console.AppInfo.Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
    }

[<EntryPoint>]
let main argv =

    let cfg = Console.getConfig(app, argv)

    let formattedCode = 
        SqliteSchemaProvider.getSchema cfg
        |> SchemaGenerator.generateModule cfg
        |> SchemaGenerator.toFormattedCode cfg app.Name

    System.IO.File.WriteAllText(cfg.OutputFile, formattedCode)
    0
