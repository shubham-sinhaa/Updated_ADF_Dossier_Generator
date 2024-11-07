# Dossier DSL

The **Dossier Document Generator** is a system designed to generate documents with dynamic content using a custom Domain-Specific Language (DSL). This DSL enables users to define placeholders within templates that can be replaced with data at runtime, including support for dynamic tables, filters, formatters, and hyperlinks.

Below is a comprehensive guide to the different types of placeholders supported by the Dossier DSL.

---

## 1. Placeholders Overview

Placeholders in Dossier DSL follow a standard format: 

`[AF.<PlaceholderType>:<TableName>.<ColumnName>|<Formatter>]`

- **`AF`**: Prefix for all placeholders.
- **`PlaceholderType`**: Defines the type of operation the placeholder represents.
- **`TableName`** : Refers to the table or data source from which the content will be drawn.
- **`ColumnName`**: Refers to the specific column or field within the table.
- **`Formatter`** (Optional): Defines how the value should be formatted. Only supported by the `AF.Value` placeholder.

Placeholders inside tables omit the `TableName` since the table's context is specified separately.

---

## 2. Supported Placeholder Types

### 2.1 Value Placeholder `[AF.Value]`

The `AF.Value` placeholder is used to replace a single value with data from a specified table and column.
The target table should have exactly one row of data else this will throw an error.

**Syntax:** `[AF.Value:<TableName>.<ColumnName>|<Formatter>]`

- **TableName**: The name of the data source table.
- **ColumnName**: The column from which to extract data.
- **Formatter** (Optional): Used to format the data.

**Example:** `[AF.Value:ClientData.FromDate]`

Replaces the placeholder with the value from the `FromDate` column in the `ClientData` table.

---

### 2.2 Multiline Value Placeholder `[AF.MultilineValue]`

The `AF.MultilineValue` placeholder is used to insert values that span multiple lines. Each line is rendered as a new
paragraph in the document. The target table should have exactly one row of data else this will throw an error.

**Syntax:** `[AF.MultilineValue:<TableName>.<ColumnName>]`

- **TableName**: The name of the data source table.
- **ColumnName**: The column from which to extract data.

**Example:** `[AF.MultilineValue:OverviewSummary.SummaryText]`

Replaces the placeholder with a multi-line data from the `SummaryText` column of the `OvviewSummary` table.

---

### 2.3 URL Placeholder `[AF.Url]`

The `AF.Url` placeholder is used to generate hyperlinks with a link value and a display text value. The target table should have exactly one row of data else this will throw an error.

**Syntax:** `[AF.Url:<TableName>.<LinkColumn>,<DisplayColumn>]`

- **TableName**: The name of the data source table.
- **LinkColumn**: The column from which to extract the link url.
- **DisplayColumn**: The column from which to extract the link text.

**Example:** `[AF.Url:ClientData.WebSite,ClientName]`

Creates a hyperlink where the URL is derived from the `WebSite` column and the display text comes from the `ClientName` column in the `ClientData` table.

---

### 2.4 Table Placeholder `[AF.Table]`

The `AF.Table` placeholder is used to insert a table from a data source and iterate over its rows. 
The `AF.Table` placeholder should be placed on the first row of the table. That row will be removed by the processor
once the placeholder has been processed. The data table context is available for all child placeholders inside the table which means that child placeholders
do not need to specify the table name.

**Syntax:** `[AF.Table:<TableName>]`

- **TableName**: The name of the data source table.

**Example:** `[AF.Table:PrintContent]`

Inserts a table based on the `PrintContent` table. See also, [Child Placeholders](#child-placeholders)

#### 2.4.1 Row filters

You can also use row filters to generate sections in a table or iterate through a subset of the rows.
Ideally, a row filter would be defined on the first placeholder in a row but it could also be specified anywhere within a row.
The number of results in the filtered dataset will determine the number of times the row is cloned. If multiple filters are defined in a row, only the 1st one is processed.

**Example:** `[AF.Row.Value:ColumnName;Filter=Column2('Some value')]`

---

### 2.5 Section Placeholder `[AF.Section.<Start|End>]`

The `AF.Section.Start` placeholder is used to mark the beginning of a section that will be cloned for each of the rows. 
The section start and end markers should be placed in their own paragraphs before and after the content that needs to be replicated. 
The section markers will be removed by the processor once the placeholders have been processed. 
The data table context is available for all child placeholders inside the section which means that child placeholders
do not need to specify the table name.

**Syntax:** `[AF.Section.Start:<TableName>]`, `[AF.Section.End]`

- **TableName**: The name of the data source table.

**Example:** `[AF.Section.Start:PrintContent]`

Repeats a section for each row in the `PrintContent` table. See also, [Child Placeholders](#child-placeholders)

#### 2.5.1 Row filters

You can also use row filters to iterate through a subset of the rows. The number of results in the filtered dataset will determine 
the number of times the section is cloned.

**Example:** `[AF.Section.Start:PrintContent;Filter=Column2('Some value')]`

---

#### 2.6 Scoped placeholders

 Scoped placeholders like `AF.Table` and `AF.Section` also support child placeholders.

* `AF.Row.Value:<ColumnName>` - insert a value from a specific column in the given context.
* `AF.Row.Url:<LinkColumn>,<DisplayColumn>` - insert a hyperlink using the link and display columns.

---

## 3. Formatters

Placeholders can be extended with formatters to change the appearance of the data being inserted into the document. Formatters are optional and can be chained using the pipe (`|`) character.

### 3.1 Supported Formatters

- **Date Formatter**: Formats date values in the given format.

**Example:** 
* `[AF.Value:ClientData.FromDate|date('dd-MM-yyyy')]`
* `[AF.Row.Value:ArticleDate|date('dd-MM-yyyy')]`
 
---
