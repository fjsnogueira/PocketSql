﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using PocketSql.Evaluation;

namespace PocketSql
{
    public class EngineDataReader : IDataReader
    {
        private readonly List<EngineResult> data;
        private int tableIndex;
        private int rowIndex = -1;

        public EngineDataReader(List<EngineResult> data) => this.data = data;

        public bool IsClosed { get; private set; }
        public void Close() => IsClosed = true;
        public void Dispose() => Close();
        public object this[int i] => GetValue(i);
        public object this[string name] => this[GetOrdinal(name)];

        public int Depth => 0;
        public int RecordsAffected => data[tableIndex].RecordsAffected;
        public int FieldCount =>
            tableIndex < 0 || tableIndex >= data.Count
                ? 0
                : data[tableIndex].ResultSet?.Columns?.Count ?? 0;

        public DataTable GetSchemaTable() => throw new FeatureNotSupportedException(
            $"{nameof(EngineDataReader)} doesn't support {nameof(GetSchemaTable)}");

        public string GetName(int i) => data[tableIndex].ResultSet.Columns[i].Name.LastOrDefault();
        public int GetOrdinal(string name) => data[tableIndex].ResultSet.GetColumnOrdinal(name);

        public Type GetFieldType(int i) => Eval.TranslateCsType(data[tableIndex].ResultSet.Columns[i].Type);
        public string GetDataTypeName(int i) => GetFieldType(i).Name;

        public bool IsDBNull(int i) => data[tableIndex].ResultSet.Rows[rowIndex].IsNull(i);

        public bool GetBoolean(int i) => (bool)GetValue(i);

        private static bool AsBoolean(object val) =>
            val switch
            {
                int i => (i != 0),
                string s => bool.Parse(s),
                _ => (bool)val
            };

        public byte GetByte(int i) => (byte)GetValue(i);
        public DateTime GetDateTime(int i) => (DateTime)GetValue(i);

        private static DateTime AsDate(object val) =>
            val is string s ? DateTime.Parse(s) :
            (DateTime)val;

        public decimal GetDecimal(int i) => (decimal)GetValue(i);
        public double GetDouble(int i) => (double)GetValue(i);
        public float GetFloat(int i) => (float)GetValue(i);
        public Guid GetGuid(int i) => (Guid)GetValue(i);
        public short GetInt16(int i) => (short)GetValue(i);
        public int GetInt32(int i) => (int)GetValue(i);
        public long GetInt64(int i) => (long)GetValue(i);
        public string GetString(int i) => (string)GetValue(i);
        public char GetChar(int i) => (char)GetValue(i);

        // TODO: consolidate all this data type conversion logic
        public object GetValue(int i) =>
            data[tableIndex].ResultSet.Columns[i].Type switch
            {
                DbType.Boolean => AsBoolean(data[tableIndex].ResultSet.Rows[rowIndex].Values[i]),
                DbType.Date => AsDate(data[tableIndex].ResultSet.Rows[rowIndex].Values[i]),
                DbType.DateTime => AsDate(data[tableIndex].ResultSet.Rows[rowIndex].Values[i]),
                DbType.DateTime2 => AsDate(data[tableIndex].ResultSet.Rows[rowIndex].Values[i]),
                _ => data[tableIndex].ResultSet.Rows[rowIndex].Values[i]
            };

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length) =>
            GetArray(i, fieldOffset, buffer, bufferOffset, length);

        public long GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length) =>
            GetArray(i, fieldOffset, buffer, bufferOffset, length);

        private long GetArray<T>(int i, long fieldOffset, T[] buffer, int bufferOffset, int length)
        {
            var array = (T[])GetValue(i);

            if (buffer == null)
            {
                return array.Length;
            }

            var bufferAmount = buffer.Length - bufferOffset;
            var charsAmount = array.Length - fieldOffset;
            var amount = Math.Max(0, Math.Min(length, Math.Min(bufferAmount, charsAmount)));
            Array.Copy(array, fieldOffset, buffer, bufferOffset, amount);
            return amount;
        }

        public int GetValues(object[] values)
        {
            var i = 0;

            for (; i < values.Length && i < FieldCount; ++i)
            {
                values[i] = GetValue(i);
            }

            return i;
        }

        public IDataReader GetData(int i) => throw new FeatureNotSupportedException(
            $"{nameof(EngineDataReader)} doesn't support {nameof(GetData)}");

        public bool NextResult()
        {
            tableIndex++;
            rowIndex = -1;
            return tableIndex < data.Count;
        }

        public bool Read()
        {
            rowIndex++;
            return
                tableIndex >= 0
                && tableIndex < (data?.Count ?? 0)
                && rowIndex < (data?[tableIndex]?.ResultSet?.Rows?.Count ?? 0);
        }
    }
}
