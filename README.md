# SqlHydra
SqlHydra is a collection of dotnet tools that generate F# records for a given database provider.

Currently supported databases:
- [SQL Server](#sqlhydrasqlserver-)
- [SQLite](#sqlhydrasqlite-)

Features:
- Generate a record for each table
- Generate [Data Readers](#data-readers) for each table


## SqlHydra.SqlServer [![NuGet version (SqlHydra.SqlServer)](https://img.shields.io/nuget/v/SqlHydra.SqlServer.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.SqlServer/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.SqlServer`

### Configure

Create a batch file or shell script (`gen.bat` or `gen.sh`) in your project directory with the following contents:

```bat
dotnet sqlhydra-mssql -c "{connection string}" -o "{output file}.fs" -ns "{namespace}"
```

_Example:_

```bat
dotnet sqlhydra-mssql -c "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI" -o "AdventureWorks.fs" -ns "SampleApp.AdventureWorks"
```

### Generate Records
1) Run your `gen.bat` (or `gen.sh`) file to generate the output .fs file.
2) Manually add the .fs file to your project.

### Regenerate Records
1) Run your `gen.bat` (or `gen.sh`) file to refresh the output .fs file.

## SqlHydra.Sqlite [![NuGet version (SqlHydra.Sqlite)](https://img.shields.io/nuget/v/SqlHydra.SqlServer.svg?style=flat-square)](https://www.nuget.org/packages/SqlHydra.Sqlite/)

### Local Install (recommended)
Run the following commands from your project directory:
1) `dotnet new tool-manifest`
2) `dotnet tool install SqlHydra.Sqlite`

### Configure

Create a batch file or shell script (`gen.bat` or `gen.sh`) in your project directory with the following contents:

```bat
dotnet sqlhydra-sqlite -c "{connection string}" -o "{output file}.fs" -ns "{namespace}"
```

_Example:_

```bat
dotnet sqlhydra-sqlite -c "Data Source=C:\MyProject\AdventureWorksLT.db" -o "AdventureWorks.fs" -ns "SampleApp.AdventureWorks"
```

### Generate Records
1) Run your `gen.bat` (or `gen.sh`) file to generate the output .fs file.
2) Manually add the .fs file to your project.

### Regenerate Records
1) Run your `gen.bat` (or `gen.sh`) file to refresh the output .fs file.


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
Using the `--readers` option will generate a special `HydraReader` class that will provide strongly typed readers for each table in a given database schema. 
- The `HydraReader` will contain a property for each table in the schema.
- The generated record for a given table can be loaded in its entirety via the `Read` method.
- Each table property in the `HydraReader` will contain a property for each column in the table to allow reading individual columns.

![HydraReader](https://user-images.githubusercontent.com/1030435/127605927-70cefaf3-f03f-42bd-a1e9-c5cfe509da8b.gif)


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
            hydra.Address.ReadIfNotNull(hydra.Address.AddressID)
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
For example, if you want to use `System.Data.SqlClient` instead of the default `Microsoft.Data.SqlClient`:

`--readers System.Data.SqlClient.SqlDataReader`.


## CLI Reference

### Arguments

| Name&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; | Alias | Default | Description |
| -------- | ----- | ------- | ------- |
| --connection | -c | *Required* | The database connection string |
| --output | -o | *Required* | A path to the generated .fs output file (relative paths are valid) |
| --namespace | -ns | *Required* | The namespace of the generated .fs output file |
| --cli-mutable |  | false | If this argument exists, a `[<CLIMutable>]` attribute will be added to each record. |
| --readers [IDataReader Type Override] |  |  | Generates data readers for each table. You can optionally override the default ADO.NET IDataReader type. Ex: `--readers "System.Data.SqlClient.SqlDataReader"`

_Example:_

```bat
dotnet sqlhydra-mssql -c "Data Source=localhost\SQLEXPRESS;Initial Catalog=AdventureWorksLT2019;Integrated Security=SSPI" -o "AdventureWorks.fs" -ns "SampleApp.AdventureWorks" --cli-mutable
```

## Recommended 3rd Party Data Library?

The answer is: it depends on how you like to design your data access code!

* If you like to meticulously craft your SQL by hand, then [Donald](#donald) with the SqlHydra generated `--readers` pairs very well together.
* Alternatively, you can use any [ADO.NET](#adonet) library that returns an `IDataReader` with the SqlHydra generated readers.
* If you want to use only the generated types, then [Dapper.FSharp](#dapperfsharp) is a great fit since Dapper uses reflection out of the box to transform `IDataReader` query results into your generated entity records.

### Donald
[Examples of using SqlHydra generated records and data readers with Donald](https://github.com/JordanMarr/SqlHydra/blob/main/src/SampleApp/DonaldExample.fs).

### ADO.NET
[Examples of using SqlHydra generated records and data readers with ADO.NET](https://github.com/JordanMarr/SqlHydra/blob/main/src/SampleApp/ReaderExample.fs).

### Dapper.FSharp
[Examples of using SqlHydra generated records with Dapper.FSharp](https://github.com/JordanMarr/SqlHydra/blob/main/src/SampleApp/DapperFSharpExample.fs).

After creating SqlHydra, I was trying to find the perfect ORM to complement SqlHyda's generated records.
Ideally, I wanted to find a library with 
- First-class support for F# records, option types, etc.
- LINQ queries (to take advantage of strongly typed SqlHydra generated records)

[FSharp.Dapper](https://github.com/Dzoukr/Dapper.FSharp) met the first critera with flying colors. 
As the name suggests, Dapper.FSharp was written specifically for F# with simplicity and ease-of-use as the driving design priorities.
FSharp.Dapper features custom F# Computation Expressions for selecting, inserting, updating and deleting, and support for F# Option types and records (no need for `[<CLIMutable>]` attributes!).

If only it had Linq queries, it would be the _perfect_ complement to SqlHydra...

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
    } |> conn.SelectAsync<SalesLT.Address>
    
let getCustomersWithAddresses(conn: IDbConnection) =
    select {
        for c in customerTable do
        leftJoin ca in customerAddressTable on (c.CustomerID = ca.CustomerID)
        leftJoin a  in addressTable on (ca.AddressID = a.AddressID)
        where (isIn c.CustomerID [30018;29545;29954;29897;29503;29559])
        orderBy c.CustomerID
    } |> conn.SelectAsyncOption<Customer, CustomerAddress, Address>

```

