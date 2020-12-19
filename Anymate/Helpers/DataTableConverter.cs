using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anymate.Helpers
{
    public class DataTableConverter : JsonConverter<DataTable>
    {
        public override DataTable Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, DataTable value,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (DataRow row in value.Rows)
            {
                writer.WriteStartObject();
                foreach (DataColumn column in row.Table.Columns)
                {
                    var columnValue = row[column];

                    // If necessary:
                    if (options.IgnoreNullValues)
                    {
                        // Do null checks on the values here and skip writing.                        
                    }

                    writer.WritePropertyName(column.ColumnName);
                    JsonSerializer.Serialize(writer, columnValue, options);
                }
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }

    public class DataSetConverter : JsonConverter<DataSet>
    {
        public override DataSet Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, DataSet value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (DataTable table in value.Tables)
            {
                writer.WritePropertyName(table.TableName);
                JsonSerializer.Serialize(writer, table, options);
            }
            writer.WriteEndObject();
        }
    }

    public static class DataTableSerializer
    {
        private static void DataSet_Serialization_WithSystemTextJson()
        {
            var options = new JsonSerializerOptions()
            {
                Converters = { new DataTableConverter(), new DataSetConverter() }
            };

            (DataTable table, DataSet dataSet) = GetDataSetAndTable();

            string jsonDataTable = JsonSerializer.Serialize(table, options);
            // [{"id":0,"item":"item 0"},{"id":1,"item":"item 1"}]
            Console.WriteLine(jsonDataTable);

            string jsonDataSet = JsonSerializer.Serialize(dataSet, options);
            // {"Table1":[{"id":0,"item":"item 0"},{"id":1,"item":"item 1"}]}
            Console.WriteLine(jsonDataSet);

            // Local function to create a sample DataTable and DataSet
            (DataTable, DataSet) GetDataSetAndTable()
            {
                dataSet = new DataSet("dataSet");

                table = new DataTable();
                DataColumn idColumn = new DataColumn("id", typeof(int))
                {
                    AutoIncrement = true
                };

                DataColumn itemColumn = new DataColumn("item");

                table.Columns.Add(idColumn);
                table.Columns.Add(itemColumn);

                dataSet.Tables.Add(table);

                for (int i = 0; i < 2; i++)
                {
                    DataRow newRow = table.NewRow();
                    newRow["item"] = "item " + i;
                    table.Rows.Add(newRow);
                }

                dataSet.AcceptChanges();

                return (table, dataSet);
            }
        }
    }
}
