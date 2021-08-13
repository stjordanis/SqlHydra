# SqlHydra
SqlHydra is a suite of NuGet packages for working with databases in F#.

- [SqlHydra.SqlServer](#sqlhydrasqlserver-) is a dotnet tool that generates F# records for a SQL Server database.
- [SqlHydra.Sqlite](#sqlhydrasqlite-) is a dotnet tool that generates F# records for a SQLite database.
- [SqlHydra.Query](#sqlhydraquery-) is an F# query generator computation expression powered by [SqlKata](https://sqlkata.com/) that supports the following databases:
    - SQL Server, SQLite, PostgreSql, MySql, Oracle, Firebird


### 🚧  🚧

The API is still forming and is subject to change, especially now while the version # is 0.x.
It will be upgraded to v1.0 once the dust has settled.

## SqlHydra.SqlServer [![NuGet version (SqlHydra.SqlServer)](https://img.shields.io/nuget/v/SqlHydra.SqlServer.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.SqlServer/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.SqlServer`

### Configure / Run

Run the tool from the command line (or add to a .bat/.sh file):

```bat
dotnet sqlhydra-mssql
```

* The configuration wizard will ask you some questions, create a new .toml configuration file for you, and then run your new config.
* If a configuration file already exists, it will just run that config.

## SqlHydra.Sqlite [![NuGet version (SqlHydra.Sqlite)](https://img.shields.io/nuget/v/SqlHydra.SqlServer.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.Sqlite/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.Sqlite`

### Configure / Run

Run the tool from the command line (or add to a .bat/.sh file):

```bat
dotnet sqlhydra-sqlite
```

* The configuration wizard will ask you some questions, create a new .toml configuration file for you, and then run your new config.
* If a configuration file already exists, it will just run that config.

![hydra-console](https://user-images.githubusercontent.com/1030435/127790303-a69ca6ea-f0a7-4216-aa5d-c292b0dc3229.gif)


## Example Output for AdventureWorks
```F#
// This code was generated by SqlHydra.SqlServer.
namespace SampleApp.AdventureWorks

module dbo =
    type ErrorLog =
        { ErrorLogID: int
          ErrorTime: System.DateTime
          UserName: string
          ErrorNumber: int
          ErrorMessage: string
          ErrorSeverity: Option<int>
          ErrorState: Option<int>
          ErrorProcedure: Option<string>
          ErrorLine: Option<int> }

    type BuildVersion =
        { SystemInformationID: byte
          ``Database Version``: string
          VersionDate: System.DateTime
          ModifiedDate: System.DateTime }

module SalesLT =
    type Address =
        { City: string
          StateProvince: string
          CountryRegion: string
          PostalCode: string
          rowguid: System.Guid
          ModifiedDate: System.DateTime
          AddressID: int
          AddressLine1: string
          AddressLine2: Option<string> }

    type Customer =
        { LastName: string
          PasswordHash: string
          PasswordSalt: string
          rowguid: System.Guid
          ModifiedDate: System.DateTime
          CustomerID: int
          NameStyle: bool
          FirstName: string
          MiddleName: Option<string>
          Title: Option<string>
          Suffix: Option<string>
          CompanyName: Option<string>
          SalesPerson: Option<string>
          EmailAddress: Option<string>
          Phone: Option<string> }
    
    // etc...
```


## Data Readers
Using the "generate data readers" option will generate a special `HydraReader` class that will provide strongly typed readers for each table in a given database schema. 
- The `HydraReader` will contain a property for each table in the schema.
- The generated record for a given table can be loaded in its entirety via the `Read` method.
- Each table property in the `HydraReader` will contain a property for each column in the table to allow reading individual columns.

![HydraReader2](https://user-images.githubusercontent.com/1030435/127606304-a73571e9-a2fa-431b-a703-365b0895b0d8.gif)


### Reading Generated Table Records

The following example loads the generated AdventureWorks Customer and Address records using the `Read` and `ReadIfNotNull` methods.
The `getCustomersLeftJoinAddresses` function returns a  `Task<(SalesLT.Customer * SalesLT.Address option) list>`.

``` fsharp
let getCustomersLeftJoinAddresses(conn: SqlConnection) = task {
    let sql = 
        """
        SELECT TOP 20 * FROM SalesLT.Customer c
        LEFT JOIN SalesLT.CustomerAddress ca ON c.CustomerID = ca.CustomerID
        LEFT JOIN SalesLT.Address a on ca.AddressID = a.AddressID
        ORDER BY c.CustomerID
        """
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    
    let hydra = SalesLT.HydraReader(reader)

    return [
        while reader.Read() do
            hydra.Customer.Read(), 
            hydra.Address.ReadIfNotNull()
    ]
}
```

### Reading Individual Columns

The next example loads individual columns using the property readers. This is useful for loading your own custom domain entities or for loading a subset of fields.
The `getProductImages` function returns a `Task<(string * string * byte[] option) list>`.

```fsharp
/// A custom domain entity
type ProductInfo = 
    {
        Product: string
        ProductNumber: string
        ThumbnailFileName: string option
        Thumbnail: byte[] option
    }

let getProductImages(conn: SqlConnection) = task {
    let sql = "SELECT TOP 10 [Name], [ProductNumber] FROM SalesLT.Product p WHERE ThumbNailPhoto IS NOT NULL"
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    
    let hydra = SalesLT.HydraReader(reader)

    return [ 
        while reader.Read() do
            { 
                ProductInfo.Product = hydra.Product.Name.Read()
                ProductInfo.ProductNumber = hydra.Product.ProductNumber.Read()
                ProductInfo.ThumbnailFileName = hydra.Product.ThumbnailPhotoFileName.Read()
                ProductInfo.Thumbnail = hydra.Product.ThumbNailPhoto.Read()
            }
    ]
}

```

### Automatic Resolution of Column Naming Conflicts

When joining tables that have the same column name, the generated `HydraReader` will automatically resolve the conflicts with the assumption that you read tables in the same order that they are joined. 

```fsharp
let getProductsAndCategories(conn: SqlConnection) = task {
    let sql = 
        """
        SELECT p.Name, c.Name
        FROM SalesLT.Product p
        LEFT JOIN SalesLT.ProductCategory c ON p.ProductCategoryID = c.ProductCategoryID
        """
    use cmd = new SqlCommand(sql, conn)
    use! reader = cmd.ExecuteReaderAsync()
    
    let hydra = SalesLT.HydraReader(reader)

    return [
        while reader.Read() do
            hydra.Product.Name.Read(), 
            hydra.ProductCategory.Name.Read()
    ]
}
```

### Overriding the Data Reader Type
If you want to use a different ADO.NET provider, you can override the generated IDataReader by specifying an optional fully qualified IDataReader type.
(The wizard will prompt you for this if you choose to not accept the default.)


## TOML Configuration Reference

### Options

| Name&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Required | Description |
| -------- | ----- | ------- |
| [general]  | Required | This section contains general settings. |
| connection | Required | The database connection string |
| output | Required | A path to the generated .fs output file (relative paths are valid) |
| namespace | Required | The namespace of the generated .fs output file |
| cli_mutable | Required | If this argument exists, a `[<CLIMutable>]` attribute will be added to each record. |
| [readers]  | Optional | This optional section contains settings that apply to the data readers feature. |
| reader_type | Required | Generates data readers for each table. You can optionally override the default ADO.NET IDataReader type. Ex: "System.Data.SqlClient.SqlDataReader"

## Recommended Data Library?

* SqlHydra.Query is made to complement SqlHydra.* generated types and data readers.
* Or you can use any [ADO.NET](#adonet) library that returns an `IDataReader` with the SqlHydra generated readers.* 
* If you like to meticulously craft your SQL by hand, then [Donald](#donald) with the SqlHydra generated `HydraReader` pairs very well together.
* If you want to use only the generated types, then [Dapper.FSharp](#dapperfsharp) is a great fit since Dapper uses reflection out of the box to transform `IDataReader` query results into your generated entity records.

### SqlHydra.Query
[Examples of using SqlHydra generated records and data readers with SqlHydra.Query](#sqlhydraquery-).

### ADO.NET
[Examples of using SqlHydra generated records and data readers with ADO.NET](https://github.com/JordanMarr/SqlHydra/blob/main/src/SampleApp/ReaderExample.fs).

### Donald
[Examples of using SqlHydra generated records and data readers with Donald](https://github.com/JordanMarr/SqlHydra/blob/main/src/SampleApp/DonaldExample.fs).

### Dapper.FSharp
[Examples of using SqlHydra generated records with Dapper.FSharp](https://github.com/JordanMarr/SqlHydra/blob/main/src/SampleApp/DapperFSharpExample.fs).

After creating SqlHydra, I was trying to find the perfect ORM to complement SqlHyda's generated records.
Ideally, I wanted to find a library with 
- First-class support for F# records, option types, etc.
- LINQ queries (to take advantage of strongly typed SqlHydra generated records)

[FSharp.Dapper](https://github.com/Dzoukr/Dapper.FSharp) met the first critera with flying colors. 
As the name suggests, Dapper.FSharp was written specifically for F# with simplicity and ease-of-use as the driving design priorities.
FSharp.Dapper features custom F# Computation Expressions for selecting, inserting, updating and deleting, and support for F# Option types and records.

If only it had Linq queries, it would be the perfect complement to SqlHydra...

So I submitted a [PR](https://github.com/Dzoukr/Dapper.FSharp/pull/26) to Dapper.FSharp that adds Linq query expressions (now in v2.0+)!

Between the two, you can have strongly typed access to your database:

```fsharp
module SampleApp.DapperFSharpExample
open System.Data
open Microsoft.Data.SqlClient
open Dapper.FSharp.LinqBuilders
open Dapper.FSharp.MSSQL
open SampleApp.AdventureWorks // Generated Types

Dapper.FSharp.OptionTypes.register()
    
// Tables
let customerTable =         table<Customer>         |> inSchema (nameof SalesLT)
let customerAddressTable =  table<CustomerAddress>  |> inSchema (nameof SalesLT)
let addressTable =          table<SalesLT.Address>  |> inSchema (nameof SalesLT)

let getAddressesForCity(conn: IDbConnection) (city: string) = 
    select {
        for a in addressTable do
        where (a.City = city)
    } 
    |> conn.SelectAsync<SalesLT.Address>
    
let getCustomersWithAddresses(conn: IDbConnection) =
    select {
        for c in customerTable do
        leftJoin ca in customerAddressTable on (c.CustomerID = ca.CustomerID)
        leftJoin a  in addressTable on (ca.AddressID = a.AddressID)
        where (isIn c.CustomerID [30018;29545;29954;29897;29503;29559])
        orderBy c.CustomerID
    } 
    |> conn.SelectAsyncOption<Customer, CustomerAddress, Address>

```

## SqlHydra.Query [![NuGet version (SqlHydra.Query)](https://img.shields.io/nuget/v/SqlHydra.Query.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.Query/)
SqlHydra.Query wraps the powerful [SqlKata](https://sqlkata.com/) query generator with F# computation expression builders for strongly typed query generation.
It can create queries for the database dialects: SQL Server, SQLite, PostgreSql, MySql, Oracle, Firebird.
SqlHydra.Query can be used with any library that accepts a data reader; however, is designed pair well with SqlHydra generated records and readers! 

### Setup
```F#
open SqlHydra.Query

// Tables
let customerTable =         table<SalesLT.Customer>         |> inSchema (nameof SalesLT)
let customerAddressTable =  table<SalesLT.CustomerAddress>  |> inSchema (nameof SalesLT)
let addressTable =          table<SalesLT.Address>          |> inSchema (nameof SalesLT)
let productTable =          table<SalesLT.Product>          |> inSchema (nameof SalesLT)
let categoryTable =         table<SalesLT.ProductCategory>  |> inSchema (nameof SalesLT)
let errorLogTable =         table<dbo.ErrorLog>
```

```F#
/// Opens a connection and creates a QueryContext that will generate SQL Server dialect queries
let openContext() = 
    let compiler = SqlKata.Compilers.SqlServerCompiler()
    let conn = openConnection()
    new QueryContext(conn, compiler)
```

### Select Builder

The following select queries will use the `HydraReader.Read` method generated by `SqlHydra.*` when the [Readers](#data-readers) option is selected.
`HydraReader.Read` infers the type generated by the query and uses the generated reader to hydrate the queried entities.

Selecting city and state columns only:
```F#
use ctx = openContext()

let cities =
    select {
        for a in addressTable do
        where (a.City =% "S%")
        select (a.City, a.StateProvince)
    }
    |> ctx.Read HydraReader.Read
    |> List.map (fun (city, state) -> $"City, State: %s{city}, %s{state}")

```

**An important note about select:**
SqlHydra.Query `select` operations currently only supports tables and fields for the sake of modifying the generated SQL query and the returned query type `'T`.
Transformations (i.e. `.ToString()` or calling any functions is _not supported_ and will throw an exception.

Select `Address` entities where City starts with `S%`:
```F#
let addresses =
    select {
        for a in addressTable do
        where (a.City =% "S%")
    }
    |> ctx.Read HydraReader.Read
```

Select top 10 `Product` entities with inner joined category name:
```F#
let! productsWithCategory = 
    select {
        for p in productTable do
        join c in categoryTable on (p.ProductCategoryID.Value = c.ProductCategoryID)
        select (p, c.Name)
        take 10
    }
    |> ctx.ReadAsync HydraReader.Read
```

Select `Customer` with left joined `Address` where `CustomerID` is in a list of values:
```F#
let! customerAddresses =
    select {
        for c in customerTable do
        leftJoin ca in customerAddressTable on (c.CustomerID = ca.Value.CustomerID)
        leftJoin a  in addressTable on (ca.Value.AddressID = a.Value.AddressID)
        where (c.CustomerID |=| [1;2;30018;29545]) // two without address, two with address
        orderBy c.CustomerID
        select (c, a)
    }
    |> ctx.ReadAsync HydraReader.Read
```

### Insert Builder

```F#
let errorLog = 
    {
        dbo.ErrorLog.ErrorLogID = 0 // Exclude
        dbo.ErrorLog.ErrorTime = System.DateTime.Now
        dbo.ErrorLog.ErrorLine = None
        dbo.ErrorLog.ErrorMessage = "TEST"
        dbo.ErrorLog.ErrorNumber = 400
        dbo.ErrorLog.ErrorProcedure = (Some "Procedure 400")
        dbo.ErrorLog.ErrorSeverity = None
        dbo.ErrorLog.ErrorState = None
        dbo.ErrorLog.UserName = "jmarr"
    }

let result : int = 
    insert {
        for e in errorLogTable do
        entity errorLog
        excludeColumn e.ErrorLogID
    }
    |> ctx.InsertGetId

printfn "Identity: %i" result
```

### Update Builder

Update individual fields:
```F#
let result = 
    update {
        for e in errorLogTable do
        set e.ErrorNumber 123
        set e.ErrorMessage "ERROR #123"
        set e.ErrorLine (Some 999)
        set e.ErrorProcedure None
        where (e.ErrorLogID = 1)
    }
    |> ctx.Update
```

Update an entity with fields excluded/included:
```F#
let result = 
    update {
        for e in errorLogTable do
        entity errorLog
        excludeColumn e.ErrorLogID
        where (e.ErrorLogID = errorLog.ErrorLogID)
    }
    |> ctx.Update

```

### Delete Builder

```F#
let result = 
    delete {
        for e in errorLogTable do
        where (e.ErrorLogID = 5)
    }
    |> ctx.Delete

printfn "result: %i" result
```
