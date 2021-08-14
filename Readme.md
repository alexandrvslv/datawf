# DataWF Data/Document WorkFlow

## Overview

Data/Document Work Flow is a set of C# .NET Standard libraries to build simple cross-platform IS:

- DataWF.Common - Collections, Reflections, io and networks helpers
- DataWF.Data - Cross RDBMS ORM

Source generators:

- DataWF.Common.Generator - Invokers for object properties: fast, generic, runtime access
- DataWF.Data.Generator - helpers and optimisations for user models

Sample & Basic models to build document worflow app

- DataWF.Module.Common - Common models
- DataWF.Module.Flow - Document Workflow models

Note: most of DataWF UI libraries of this repository is in developing/deprecating stage and not ready for production!

## Planing

Code quality, tests, simplify, usability
Support multiservices: muilti web clients geneerator, web reference properties, 
Port UI to Avalonia or Maui

## DataWF.Data

ORM based on ADO.NET clients implementations. Not use EF directly, just as analog targeting to model-driven apps.

Features: Code-first, Relational data managment, Formatting DDL and DML, Caching large tables, Query parser/executor

Cover folowing database objects: Schema, Table, View, Column, Constraints(Primary and Foreign Key), Index, Stored Procedure.

Support several RDBMS:

- Postgress
- Sqlite
- MSSQL
- MySql
- Oracle
- Any ADO.NET Driver 

Limitation/Overhead:

- Single column Primary Key
- System columns for each table: Create date, Stamp date, Access binary, Item Type identifier
- In Memory caching and indexing: slowdown downloading process


Model example:

    [Table("employer_table")]
    public sealed partial class Employer : DBItem
    {
        [Column("id", Keys = DBColumnKeys.Primary)]
        public int Id 
        {  
            get => GetValue<int>();  
            set => SetValue(value);
        }

        [Column("inn", 20, Keys = DBColumnKeys.Code), Index("employerinn", true)]
        public string INN 
        {  
            get => GetValue<string>(); 
            set => SetValue(value);
        }

        [Column("name", 200, Keys = DBColumnKeys.Culture)]
        public override string Name 
        {  
            get => GetName(); 
            set => SetName(value); 
        }

        [Column("positionid")]
        public int? PositionId 
        { 
            get => GetValue<int?>(); 
            set => SetValue(value);
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get => GetReference<Position>();
            set => SetReference(value);
        }

        [Column("typeid", Keys = DBColumnKeys.Type, Default = "1")]
        public EmployerType? Type 
        {  
            get => GetValue<EmployerType?>();
            set => SetValue(value);
        }
    }

Source generator for model:
    
    // Table with columns cache
    public partial class EmployerTable : DBTable<Employer>, IEmployerTable
    {
        public DBColumn<int> IdKey => _IdKey ??= ParseProperty(nameof(Employer.Id));
        public DBColumn<string> INNKey => _IINKey ??= ParseProperty(nameof(Employer.INN));
        ...
    }

    // Table interface
    public partial interface IEmployerTable : IDBTable
    {
        DBColumn<int> IdKey { get; }
        DBColumn<string> INNKey { get; }
        ...
    }

    // Log object (with log table)
    public partial class EmployerLog: DBItemLog
    {
        public EmployerLog(Employer item): base(item)
        {}

        [LogColumn("id", "id_log")]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue<int>(value, Table.IdKey);
        }
    }

Models Schema:

    [Schema("employers")]
    [SchemaEntry(typeof(Employer))]
    [SchemaEntry(typeof(Position))]
    public partial class EmployerSchema: DBSchema
    {}

Source generator for Models Schema:

    // Embed tables reference && runtime generation
    public partial class EmployerSchema: IEmployerSchema
    {
        public EmployerTable Employer => _Employer ??= GetTable<Employer>();
        public PositionTable Position => _Position ??= GetTable<Position>();
        
        public void Generate()
        {
            base.Generate(new Type[]{typeof(Employer), typeof(Position)});
        }
    }

    // Schema interface
    public partial interface IEmployerSchema: IDBSchema
    {
        EmployerTable Employer {get;}
        PositionTable Position {get;}
    }

    // Log Schema
    public partial class EmployerSchemaLog: IEmployerSchemaLog
    {
        public EmployerLogTable EmployerLog => _EmployerLog ??= GetTable<EmployerLog>();
        public PositionLogTable PositionLog => _PositionLog ??= GetTable<PositionLog>();
        
        public void Generate()
        {
            base.Generate(new Type[]{typeof(EmployerLog), typeof(PositionLog)});
        }
    }

Connection example:

    var connection = new DBConnection("test") { System = DBSystem.SQLite, DataBase = "test.sqlite" };
    var qresult = connection.ExecuteQResult($"select * from {SomeTableName}");

## DataWF.Module.Common

Common Models:

- Group & Permission
- User & User Group
- User Log
- Reference Book
- Scheduler

## DataWF.Module.Flow

WorkFlow Models:

- Work & Stage
- Template & Attribute
- Document & Relating Data
