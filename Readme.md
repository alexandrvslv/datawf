# DataWF Data Wrapper Framework

## Overview

Data/Document Work Flow is a set of C# libraries to build simple cross-platform IS:

- DataWF.Common - .NETStandard collections, reflections, io and networks helpers
- DataWF.Data - .NETStandard Cross RDBMS ORM
- DataWF.Gui - Xwt based desktop UI
- DataWF.Data.Gui - Database desktop UI
- DataWF.Module.Common - Common models
- DataWF.Module.Flow - Document Workflow models
- DataWF.Module.CommonGui - Users Administator UI
- DataWF.Mudule.FlowGui - Document Workflow UI

Note: most of DataWF UI libraries is in developing stage and not ready for production!

## Planing

Implement RPC/REST Server with Desktop/Web UI.
Port to Avalonia UI

## DataWF.Data

ORM based on ADO.NET drivers. Not use EF.

Features: Code-first, Relational data managment, Formatting DDL and DML, Caching large tables

Cover basic database objects: Schema, Table, View, Column, Constraints(Primary and Foreign Key), Index, Stored Procedure.

Support several RDBMS(simply extendable):

- MSSQL
- MySql
- Oracle
- Postgress
- Sqlite

Limitation/Overhead:

- One column Primary Key
- System columns: Create date, Stamp date, Access binary, Item Type identifier

Note: used beta version of ODP.NET for .netstandard compatibility

Model example:

    [Table("employer_table")]
    public class Employer : DBItem
    {
        public static DBTable<Employer> DBTable { get { return DBService.GetTable<Employer>(); } }

        public Employer() { Build(DBTable); }

        [Column("id", Keys = DBColumnKeys.Primary)]
        public int? Id {  get { return GetProperty<int?>(); }  set { SetProperty(value); } }

        [Column("inn", 20, Keys = DBColumnKeys.Code), Index("employerinn", true)]
        public string INN {  get { return GetProperty<string>(); }  set { SetProperty(value); } }

        [Column("name", 200, Keys = DBColumnKeys.Culture)]
        public override string Name {  get { return GetName(nameof(Name)); }  set { SetName(nameof(Name), value); } }

        [Column("positionid")]
        public int? PositionId { get { return GetProperty<int?>(); } set { SetProperty(value); } }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get { return GetPropertyReference<Position>(); }
            set { SetPropertyReference(value); }
        }

        [Column("typeid", Keys = DBColumnKeys.Type, Default = "1")]
        public EmployerType? Type {  get { return GetProperty<EmployerType?>(); }  set { SetProperty(value); } }
    }

Connection example:

    var connection = new DBConnection("test") { System = DBSystem.SQLite, DataBase = "test.sqlite" };
    var qresult = connection.ExecuteQResult( $"select * from {SomeTableName}");

## DataWF.Gui

Cross Platform Desktop UI, based on Xwt. Provide several widgets to build data navigation application

Main widgets:

- LayoutList - universal widget, can be used like list view, tree view or property view. Support sorting, grouping, filtering and can construct from reflection
- Toolsbar - buttons container
- GroupBox - widget layout container
- DockBox - dock panel manager
- ToolWindow - helper window

Note: Move UI from WinForm/Gtk# to Xwt is not compleate and little bugly.

## DataWF.Data.Gui

- Database Administrator
- Database Export utilite
- Query Builder(not compleate)
- Report engeene(planed)

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

## DataWF.Module.CommonGUI

- User&Group Administrator

## DataWF.Module.FlowGui

- Workflow Administrator
- Document Editor (Create, Edit, Send document throw the flow);
